using Glide.Core.Enums;

namespace Glide.Core.Models;

public record ServiceDescriptor(
    string Id,
    string Name,
    string Description,
    ServiceCategory Category,
    string Icon,
    string BaseUrl,
    IReadOnlyList<string> AllowedDomains,
    bool IsImplemented = true,
    bool RequiresAuth = true
);

public record AudioTrack(
    string Title,
    string Artist,
    string Album,
    string? CoverArtUrl = null,
    TimeSpan Duration = default,
    TimeSpan Position = default,
    AudioPlaybackState State = AudioPlaybackState.Stopped
);

public record UnifiedNotification(
    string Id,
    string ServiceId,
    string Sender,
    string Title,
    string Snippet,
    DateTime Timestamp,
    bool IsRead = false,
    string? AvatarUrl = null,
    string Category = "Geral"
);

public record ConnectParticipant(
    string Id,
    string DisplayName,
    bool IsSpeaking = false,
    bool IsMuted = false,
    bool IsDeafened = false,
    int Volume = 100,
    bool IsScreenSharing = false,
    string? AvatarPath = null
);

public record ConnectChatMessage(
    string Id,
    string SenderName,
    string Content,
    DateTime Timestamp,
    string? ImagePath = null,
    bool IsSelf = false
);

public record UserProfile(
    string Nickname = "Raposo",
    string StatusMessage = "Online • No Glide Gaming Hub",
    string? AvatarPath = null,
    string? BannerPath = null
);

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public bool CloseToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool GamingMode { get; set; } = false;
    public bool HardwareAcceleration { get; set; } = true;
    public bool PushToTalkEnabled { get; set; } = false;
    public string PushToTalkKey { get; set; } = "V";
    public int AudioVolume { get; set; } = 80;
    public string PreferredAudioService { get; set; } = "ytmusic";
    public string PreferredVideoService { get; set; } = "youtube";

    // Novas Configurações
    public bool StrictSecurityMode { get; set; } = true;
    public bool AutoGamingModeDetection { get; set; } = true;
    public bool AutoEcoModeInBackground { get; set; } = true;
    public float MicSensitivityThreshold { get; set; } = 0.015f;
    public bool RealtimeMetricsEnabled { get; set; } = true;
    public List<string> ConnectedAccounts { get; set; } = new() { "google", "spotify" };
}
