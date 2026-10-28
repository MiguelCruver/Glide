using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Glide.Core.Models;
using MQTTnet;
using MQTTnet.Protocol;

namespace Glide.App.Services;

public class GlideOnlineP2PService : IDisposable
{
    private static GlideOnlineP2PService? _instance;
    public static GlideOnlineP2PService Instance => _instance ??= new GlideOnlineP2PService();

    private readonly IMqttClient _mqttClient;
    private string _currentRoomCode = string.Empty;
    private ConnectParticipant? _localParticipant;
    private readonly Timer _presenceTimer;
    private bool _disposed = false;

    public event Action<ConnectChatMessage>? MessageReceived;
    public event Action<ConnectParticipant>? ParticipantJoined;
    public event Action<string>? ParticipantLeft;
    public event Action<string, bool>? ParticipantSpeakingChanged;
    public event Action<bool>? ConnectionStatusChanged;

    public bool IsConnected => _mqttClient.IsConnected;
    public string CurrentRoomCode => _currentRoomCode;

    public GlideOnlineP2PService()
    {
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectedAsync += args =>
        {
            ConnectionStatusChanged?.Invoke(true);
            return Task.CompletedTask;
        };
        _mqttClient.DisconnectedAsync += args =>
        {
            ConnectionStatusChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        // Ping periódico de presença para manter usuários sincronizados na sala
        _presenceTimer = new Timer(async _ =>
        {
            if (_mqttClient.IsConnected && !string.IsNullOrWhiteSpace(_currentRoomCode) && _localParticipant != null)
            {
                await BroadcastPresenceAsync("ping");
            }
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<bool> ConnectToRoomAsync(string roomCode, ConnectParticipant localUser)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return false;

        _currentRoomCode = roomCode.Trim().ToUpperInvariant();
        _localParticipant = localUser;

        try
        {
            if (!_mqttClient.IsConnected)
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("broker.hivemq.com", 1883)
                    .WithClientId($"glide_{localUser.Id}_{Guid.NewGuid():N}")
                    .WithCleanSession(true)
                    .WithTimeout(TimeSpan.FromSeconds(5))
                    .Build();

                await _mqttClient.ConnectAsync(options);
            }

            if (_mqttClient.IsConnected)
            {
                string topicFilter = $"glide/rooms/{_currentRoomCode}/#";
                var subscribeOptions = new MqttClientFactory().CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(topicFilter).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .Build();

                await _mqttClient.SubscribeAsync(subscribeOptions);

                // Anuncia entrada na sala
                await BroadcastPresenceAsync("join");

                // Inicia timer de presença a cada 8 segundos
                _presenceTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8));
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Glide P2P] Falha de conexão: {ex.Message}");
        }

        return false;
    }

    public async Task LeaveRoomAsync()
    {
        _presenceTimer.Change(Timeout.Infinite, Timeout.Infinite);

        if (_mqttClient.IsConnected && !string.IsNullOrWhiteSpace(_currentRoomCode) && _localParticipant != null)
        {
            try
            {
                await BroadcastPresenceAsync("leave");
                string topicFilter = $"glide/rooms/{_currentRoomCode}/#";
                var unsubscribeOptions = new MqttClientFactory().CreateUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topicFilter)
                    .Build();
                await _mqttClient.UnsubscribeAsync(unsubscribeOptions);
            }
            catch {}
        }

        _currentRoomCode = string.Empty;
    }

    public async Task SendChatMessageAsync(ConnectChatMessage message, string? sourceImagePath = null)
    {
        if (!_mqttClient.IsConnected || string.IsNullOrWhiteSpace(_currentRoomCode) || _localParticipant == null)
            return;

        string? imageBase64 = null;
        string? imageFileName = null;

        if (!string.IsNullOrEmpty(sourceImagePath) && File.Exists(sourceImagePath))
        {
            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(sourceImagePath);
                // Limite de segurança de 1.5MB para imagens no canal P2P
                if (bytes.Length <= 1_500_000)
                {
                    imageBase64 = Convert.ToBase64String(bytes);
                    imageFileName = Path.GetFileName(sourceImagePath);
                }
            }
            catch {}
        }

        var payload = new OnlineChatPayload(
            Id: message.Id,
            SenderId: _localParticipant.Id,
            SenderName: message.SenderName,
            Content: message.Content,
            Timestamp: message.Timestamp,
            ImageBase64: imageBase64,
            ImageFileName: imageFileName
        );

        string json = JsonSerializer.Serialize(payload);
        string topic = $"glide/rooms/{_currentRoomCode}/chat";

        var mqttMsg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        try
        {
            await _mqttClient.PublishAsync(mqttMsg);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Glide P2P] Erro ao enviar mensagem de chat: {ex.Message}");
        }
    }

    public async Task SendVoiceActivityAsync(bool isSpeaking)
    {
        if (!_mqttClient.IsConnected || string.IsNullOrWhiteSpace(_currentRoomCode) || _localParticipant == null)
            return;

        var payload = new OnlineVoicePayload(
            UserId: _localParticipant.Id,
            IsSpeaking: isSpeaking
        );

        string json = JsonSerializer.Serialize(payload);
        string topic = $"glide/rooms/{_currentRoomCode}/voice";

        var mqttMsg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        try
        {
            await _mqttClient.PublishAsync(mqttMsg);
        }
        catch {}
    }

    public async Task BroadcastPresenceAsync(string action = "join")
    {
        if (!_mqttClient.IsConnected || string.IsNullOrWhiteSpace(_currentRoomCode) || _localParticipant == null)
            return;

        var payload = new OnlinePresencePayload(
            Type: action,
            UserId: _localParticipant.Id,
            DisplayName: _localParticipant.DisplayName,
            AvatarPath: _localParticipant.AvatarPath,
            IsMuted: _localParticipant.IsMuted,
            IsDeafened: _localParticipant.IsDeafened
        );

        string json = JsonSerializer.Serialize(payload);
        string topic = $"glide/rooms/{_currentRoomCode}/presence";

        var mqttMsg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        try
        {
            await _mqttClient.PublishAsync(mqttMsg);
        }
        catch {}
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            string topic = args.ApplicationMessage.Topic;
            byte[] bytes = args.ApplicationMessage.Payload.ToArray();
            string json = Encoding.UTF8.GetString(bytes);

            if (topic.EndsWith("/chat", StringComparison.OrdinalIgnoreCase))
            {
                var chat = JsonSerializer.Deserialize<OnlineChatPayload>(json);
                if (chat != null && _localParticipant != null && chat.SenderId != _localParticipant.Id)
                {
                    string? savedImagePath = null;
                    if (!string.IsNullOrEmpty(chat.ImageBase64))
                    {
                        try
                        {
                            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string roomAttachDir = Path.Combine(appData, "Glide", "Attachments", _currentRoomCode);
                            Directory.CreateDirectory(roomAttachDir);

                            string safeFileName = string.IsNullOrWhiteSpace(chat.ImageFileName)
                                ? $"{chat.Id}.png"
                                : $"{Guid.NewGuid():N}_{Path.GetFileName(chat.ImageFileName)}";

                            savedImagePath = Path.Combine(roomAttachDir, safeFileName);
                            byte[] imgBytes = Convert.FromBase64String(chat.ImageBase64);
                            File.WriteAllBytes(savedImagePath, imgBytes);
                        }
                        catch {}
                    }

                    var domainMessage = new ConnectChatMessage(
                        Id: chat.Id,
                        SenderName: chat.SenderName,
                        Content: chat.Content,
                        Timestamp: chat.Timestamp,
                        ImagePath: savedImagePath,
                        IsSelf: false
                    );

                    MessageReceived?.Invoke(domainMessage);
                }
            }
            else if (topic.EndsWith("/presence", StringComparison.OrdinalIgnoreCase))
            {
                var presence = JsonSerializer.Deserialize<OnlinePresencePayload>(json);
                if (presence != null && _localParticipant != null && presence.UserId != _localParticipant.Id)
                {
                    if (presence.Type == "leave")
                    {
                        ParticipantLeft?.Invoke(presence.UserId);
                    }
                    else
                    {
                        var participant = new ConnectParticipant(
                            Id: presence.UserId,
                            DisplayName: presence.DisplayName,
                            IsSpeaking: false,
                            IsMuted: presence.IsMuted,
                            IsDeafened: presence.IsDeafened,
                            AvatarPath: presence.AvatarPath
                        );
                        ParticipantJoined?.Invoke(participant);

                        // Se for um join novo e formos nós na sala, respondemos com nosso ping para o recém-chegado nos ver
                        if (presence.Type == "join")
                        {
                            _ = BroadcastPresenceAsync("ping");
                        }
                    }
                }
            }
            else if (topic.EndsWith("/voice", StringComparison.OrdinalIgnoreCase))
            {
                var voice = JsonSerializer.Deserialize<OnlineVoicePayload>(json);
                if (voice != null && _localParticipant != null && voice.UserId != _localParticipant.Id)
                {
                    ParticipantSpeakingChanged?.Invoke(voice.UserId, voice.IsSpeaking);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Glide P2P] Erro ao processar mensagem recebida: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _presenceTimer.Dispose();
        _ = LeaveRoomAsync();
        try { _mqttClient.Dispose(); } catch {}
    }
}

public record OnlinePresencePayload(
    string Type,
    string UserId,
    string DisplayName,
    string? AvatarPath,
    bool IsMuted,
    bool IsDeafened
);

public record OnlineChatPayload(
    string Id,
    string SenderId,
    string SenderName,
    string Content,
    DateTime Timestamp,
    string? ImageBase64,
    string? ImageFileName
);

public record OnlineVoicePayload(
    string UserId,
    bool IsSpeaking
);
