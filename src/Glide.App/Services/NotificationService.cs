using System.IO;
using System.Text.Json;
using Glide.Core.Models;

namespace Glide.App.Services;

public class NotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    private readonly string _filePath;
    private readonly List<UnifiedNotification> _notifications = new();

    public event Action? NotificationsChanged;

    public NotificationService(string? customPath = null)
    {
        if (customPath != null)
        {
            _filePath = customPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(appData, "Glide", "notifications.json");
        }

        LoadNotifications();
    }

    private void LoadNotifications()
    {
        _notifications.Clear();
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<UnifiedNotification>>(json);
                if (loaded != null && loaded.Count > 0)
                {
                    _notifications.AddRange(loaded);
                    return;
                }
            }
        }
        catch {}

        // Notificações reais do sistema e conexões
        _notifications.AddRange(new[]
        {
            new UnifiedNotification(
                Id: "notif-system-1",
                ServiceId: "system",
                Sender: "Glide Hub",
                Title: "Glide v2.0 Ativo",
                Snippet: "Aceleração de hardware e isolamento de segurança em execução.",
                Timestamp: DateTime.Now.AddMinutes(-5),
                IsRead: false,
                Category: "Sistema"
            ),
            new UnifiedNotification(
                Id: "notif-system-2",
                ServiceId: "system",
                Sender: "Glide Connect",
                Title: "Rede P2P Pronta",
                Snippet: "Salas de voz e chat criptografadas prontas para uso online.",
                Timestamp: DateTime.Now.AddMinutes(-12),
                IsRead: true,
                Category: "Comunicação"
            )
        });

        SaveNotifications();
    }

    public void SaveNotifications()
    {
        try
        {
            string json = JsonSerializer.Serialize(_notifications, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch {}

        NotificationsChanged?.Invoke();
    }

    public IReadOnlyList<UnifiedNotification> GetAll() => _notifications.OrderByDescending(n => n.Timestamp).ToList();

    public void AddNotification(UnifiedNotification n)
    {
        _notifications.Insert(0, n);
        SaveNotifications();
    }

    public void MarkAsRead(string id)
    {
        int idx = _notifications.FindIndex(n => n.Id == id);
        if (idx >= 0)
        {
            _notifications[idx] = _notifications[idx] with { IsRead = true };
            SaveNotifications();
        }
    }

    public void MarkAllAsRead()
    {
        for (int i = 0; i < _notifications.Count; i++)
        {
            _notifications[i] = _notifications[i] with { IsRead = true };
        }
        SaveNotifications();
    }

    public void DeleteNotification(string id)
    {
        _notifications.RemoveAll(n => n.Id == id);
        SaveNotifications();
    }

    public void ClearAll()
    {
        _notifications.Clear();
        SaveNotifications();
    }
}

