using System.IO;
using Glide.App.Services;
using Glide.Core.Models;
using Glide.Core.Services;
using Xunit;

namespace Glide.UnitTests;

public class ConnectServicesTests
{
    [Fact]
    public void ConnectDbService_ShouldStoreAndRetrieveMessages()
    {
        string tempDbPath = Path.Combine(Path.GetTempPath(), $"glide_test_{Guid.NewGuid():N}.db");
        try
        {
            var db = new ConnectDbService(tempDbPath);
            string room = "TEST-ROOM-1";

            var msg1 = new ConnectChatMessage(
                Id: Guid.NewGuid().ToString(),
                SenderName: "TestUser",
                Content: "Olá do teste unitário",
                Timestamp: DateTime.UtcNow,
                ImagePath: null,
                IsSelf: true
            );

            db.SaveMessage(room, msg1);

            var messages = db.GetMessages(room);
            Assert.Single(messages);
            Assert.Equal("TestUser", messages[0].SenderName);
            Assert.Equal("Olá do teste unitário", messages[0].Content);
            Assert.True(messages[0].IsSelf);

            // Test Clear
            db.ClearMessages(room);
            var messagesAfterClear = db.GetMessages(room);
            Assert.Empty(messagesAfterClear);
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                try { File.Delete(tempDbPath); } catch {}
            }
        }
    }

    [Fact]
    public void MicrophoneService_MuteAndDispose_ShouldBehaveDeterministically()
    {
        using var mic = new MicrophoneService();
        Assert.False(mic.IsMuted);
        Assert.False(mic.IsSpeaking);

        mic.IsMuted = true;
        Assert.True(mic.IsMuted);

        // Start and Stop should not throw even in headless / CI environments without audio inputs
        mic.Start();
        mic.Stop();
    }

    [Fact]
    public void ServiceCatalog_ShouldContainTwitterAndSpotifyWithValidDomains()
    {
        var catalog = new DefaultServiceCatalog();
        
        var twitter = catalog.GetServiceById("twitter");
        Assert.NotNull(twitter);
        Assert.Contains(twitter.AllowedDomains, d => d.Contains("x.com"));
        Assert.Contains(twitter.AllowedDomains, d => d.Contains("twitter.com"));

        var spotify = catalog.GetServiceById("spotify");
        Assert.NotNull(spotify);
        Assert.Contains(spotify.AllowedDomains, d => d.Contains("open.spotify.com"));
        Assert.Contains(spotify.AllowedDomains, d => d.Contains("spotify.com"));

        var instagram = catalog.GetServiceById("instagram");
        Assert.NotNull(instagram);
        Assert.Contains(instagram.AllowedDomains, d => d.Contains("instagram.com"));
    }

    [Fact]
    public void AppSettingsService_ShouldSaveAndSyncStrictSecurityMode()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"glide_settings_{Guid.NewGuid():N}.json");
        try
        {
            var service = new AppSettingsService(tempPath);
            Assert.True(service.Settings.StrictSecurityMode);

            var modified = new AppSettings
            {
                StrictSecurityMode = false,
                PushToTalkKey = "F",
                GamingMode = true
            };

            service.SaveSettings(modified);

            var reloaded = new AppSettingsService(tempPath);
            Assert.False(reloaded.Settings.StrictSecurityMode);
            Assert.Equal("F", reloaded.Settings.PushToTalkKey);
            Assert.True(reloaded.Settings.GamingMode);
        }
        finally
        {
            if (File.Exists(tempPath)) { try { File.Delete(tempPath); } catch {} }
        }
    }

    [Fact]
    public void UserProfileService_ShouldPersistCustomNickAndStatus()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"glide_profile_{Guid.NewGuid():N}");
        try
        {
            var profileService = new UserProfileService(tempDir);
            Assert.False(string.IsNullOrWhiteSpace(profileService.CurrentProfile.Nickname));

            var newProfile = new UserProfile(
                Nickname: "RaposoProCS2",
                StatusMessage: "Noite de comp",
                AvatarPath: null,
                BannerPath: null
            );

            profileService.SaveProfile(newProfile);

            var reloaded = new UserProfileService(tempDir);
            Assert.Equal("RaposoProCS2", reloaded.CurrentProfile.Nickname);
            Assert.Equal("Noite de comp", reloaded.CurrentProfile.StatusMessage);
        }
        finally
        {
            if (Directory.Exists(tempDir)) { try { Directory.Delete(tempDir, true); } catch {} }
        }
    }

    [Fact]
    public void PerformanceMetrics_ShouldReportRealValues()
    {
        using var perf = new PerformanceMetricsService();
        var snap = perf.CaptureSnapshot();

        Assert.True(snap.ProcessRamMb > 0, "ProcessRamMb deve ser maior que 0");
        Assert.True(snap.ThreadsCount > 0, "ThreadsCount deve ser maior que 0");
        Assert.False(string.IsNullOrWhiteSpace(snap.RamSummary));
        Assert.False(string.IsNullOrWhiteSpace(snap.CpuSummary));
    }

    [Fact]
    public void NotificationService_ShouldAddAndFilterNotifications()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"glide_notif_{Guid.NewGuid():N}.json");
        try
        {
            var notifService = new NotificationService(tempPath);
            var initial = notifService.GetAll();
            Assert.NotEmpty(initial);

            var custom = new UnifiedNotification(
                Id: "test-n1",
                ServiceId: "steam",
                Sender: "Test Steam",
                Title: "Partida Aceita",
                Snippet: "Lobby fechado",
                Timestamp: DateTime.UtcNow,
                IsRead: false,
                Category: "Jogos"
            );

            notifService.AddNotification(custom);
            Assert.Contains(notifService.GetAll(), n => n.Id == "test-n1");

            notifService.MarkAsRead("test-n1");
            var item = notifService.GetAll().First(n => n.Id == "test-n1");
            Assert.True(item.IsRead);

            notifService.DeleteNotification("test-n1");
            Assert.DoesNotContain(notifService.GetAll(), n => n.Id == "test-n1");
        }
        finally
        {
            if (File.Exists(tempPath)) { try { File.Delete(tempPath); } catch {} }
        }
    }

    [Fact]
    public void NotificationService_ShouldNotContainMockUsers()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"glide_notif_clean_{Guid.NewGuid():N}.json");
        try
        {
            var notifService = new NotificationService(tempPath);
            var items = notifService.GetAll();
            Assert.DoesNotContain(items, n => n.Sender.Contains("Matheus"));
            Assert.DoesNotContain(items, n => n.Sender.Contains("Grupo"));
        }
        finally
        {
            if (File.Exists(tempPath)) { try { File.Delete(tempPath); } catch {} }
        }
    }

    [Fact]
    public void OnlinePayloads_ShouldSerializeAndDeserializeProperly()
    {
        var chat = new OnlineChatPayload(
            Id: "msg-123",
            SenderId: "user-abc",
            SenderName: "Player1",
            Content: "Bora jogar!",
            Timestamp: DateTime.UtcNow,
            ImageBase64: null,
            ImageFileName: null
        );

        string json = System.Text.Json.JsonSerializer.Serialize(chat);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<OnlineChatPayload>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("msg-123", deserialized.Id);
        Assert.Equal("Player1", deserialized.SenderName);
        Assert.Equal("Bora jogar!", deserialized.Content);

        var presence = new OnlinePresencePayload(
            Type: "join",
            UserId: "user-abc",
            DisplayName: "Player1",
            AvatarPath: null,
            IsMuted: false,
            IsDeafened: false
        );

        string pJson = System.Text.Json.JsonSerializer.Serialize(presence);
        var pDeserialized = System.Text.Json.JsonSerializer.Deserialize<OnlinePresencePayload>(pJson);
        Assert.NotNull(pDeserialized);
        Assert.Equal("join", pDeserialized.Type);
        Assert.Equal("user-abc", pDeserialized.UserId);
    }
}

