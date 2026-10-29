using System.Windows;
using System.Windows.Input;
using Glide.App.Services;
using Glide.Core.Enums;
using Glide.Core.Models;

namespace Glide.App.ViewModels;

public class HomeViewModel : ViewModelBase, IDisposable
{
    private readonly MainViewModel _mainVm;
    private readonly PerformanceMetricsService _metricsService;
    private readonly AppSettingsService _settingsService;
    private string _ramDisplayText = "Carregando...";
    private string _cpuDisplayText = "Calculando CPU...";
    private string _uptimeDisplayText = "0s";
    private bool _isDisposed = false;

    public HomeViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        _metricsService = PerformanceMetricsService.Instance;
        _settingsService = AppSettingsService.Instance;

        UpdateFromSnapshot(_metricsService.CurrentSnapshot);

        _metricsService.SnapshotUpdated += snap =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                UpdateFromSnapshot(snap);
            });
        };

        ConnectServiceCommand = new RelayCommand(param =>
        {
            if (param is not string serviceKey) return;
            HandleServiceConnect(serviceKey);
        });

        DisconnectServiceCommand = new RelayCommand(param =>
        {
            if (param is not string serviceKey) return;
            HandleServiceDisconnect(serviceKey);
        });
    }

    private void UpdateFromSnapshot(PerformanceSnapshot snap)
    {
        RamDisplayText = $"RAM: {snap.RamSummary}";
        CpuDisplayText = $"Carga: {snap.CpuSummary}";
        UptimeDisplayText = snap.UptimeSummary;
    }

    private void HandleServiceConnect(string serviceKey)
    {
        var settings = _settingsService.Settings;
        bool isNewConnection = !settings.ConnectedAccounts.Contains(serviceKey);
        if (isNewConnection)
        {
            settings.ConnectedAccounts.Add(serviceKey);
            _settingsService.SaveSettings(settings);
            NotifyAccountProperties();

            string serviceName = serviceKey switch
            {
                "google" => "Google (YouTube & Gmail)",
                "spotify" => "Spotify",
                "steam" => "Steam Community",
                "instagram" => "Instagram",
                "twitter" => "X (Twitter)",
                _ => serviceKey
            };

            NotificationService.Instance.AddNotification(new UnifiedNotification(
                Id: Guid.NewGuid().ToString(),
                ServiceId: serviceKey,
                Sender: serviceName,
                Title: "Conta Conectada",
                Snippet: $"Sua conta {serviceName} foi conectada com sucesso ao Glide.",
                Timestamp: DateTime.Now,
                Category: "Jogos"
            ));
        }

        switch (serviceKey)
        {
            case "google":
                _mainVm.NavigateCommand.Execute(GlideModule.Audio);
                break;
            case "spotify":
                _mainVm.NavigateCommand.Execute(GlideModule.Audio);
                break;
            case "steam":
                _mainVm.NavigateCommand.Execute(GlideModule.Notifications);
                break;
            case "instagram":
                _mainVm.NavigateCommand.Execute(GlideModule.Video);
                break;
            case "twitter":
                _mainVm.NavigateCommand.Execute(GlideModule.Video);
                break;
        }
    }

    private void HandleServiceDisconnect(string serviceKey)
    {
        var settings = _settingsService.Settings;
        if (settings.ConnectedAccounts.Contains(serviceKey))
        {
            settings.ConnectedAccounts.Remove(serviceKey);
            _settingsService.SaveSettings(settings);
            NotifyAccountProperties();

            string serviceName = serviceKey switch
            {
                "google" => "Google",
                "spotify" => "Spotify",
                "steam" => "Steam",
                "instagram" => "Instagram",
                "twitter" => "X (Twitter)",
                _ => serviceKey
            };

            NotificationService.Instance.AddNotification(new UnifiedNotification(
                Id: Guid.NewGuid().ToString(),
                ServiceId: "system",
                Sender: "Glide Hub",
                Title: "Conta Desconectada",
                Snippet: $"A sincronização com {serviceName} foi desativada.",
                Timestamp: DateTime.Now,
                Category: "Sistema"
            ));
        }
    }

    private void NotifyAccountProperties()
    {
        OnPropertyChanged(nameof(IsGoogleConnected));
        OnPropertyChanged(nameof(IsSpotifyConnected));
        OnPropertyChanged(nameof(IsSteamConnected));
        OnPropertyChanged(nameof(IsInstagramConnected));
        OnPropertyChanged(nameof(IsTwitterConnected));

        OnPropertyChanged(nameof(GoogleConnectStatusText));
        OnPropertyChanged(nameof(SpotifyConnectStatusText));
        OnPropertyChanged(nameof(SteamConnectStatusText));
        OnPropertyChanged(nameof(InstagramConnectStatusText));
        OnPropertyChanged(nameof(TwitterConnectStatusText));
    }

    public MainViewModel Main => _mainVm;

    public string RamDisplayText
    {
        get => _ramDisplayText;
        set => SetProperty(ref _ramDisplayText, value);
    }

    public string CpuDisplayText
    {
        get => _cpuDisplayText;
        set => SetProperty(ref _cpuDisplayText, value);
    }

    public string UptimeDisplayText
    {
        get => _uptimeDisplayText;
        set => SetProperty(ref _uptimeDisplayText, value);
    }

    // --- Status das Contas Conectadas ---
    public bool IsGoogleConnected => _settingsService.Settings.ConnectedAccounts.Contains("google");
    public bool IsSpotifyConnected => _settingsService.Settings.ConnectedAccounts.Contains("spotify");
    public bool IsSteamConnected => _settingsService.Settings.ConnectedAccounts.Contains("steam");
    public bool IsInstagramConnected => _settingsService.Settings.ConnectedAccounts.Contains("instagram");
    public bool IsTwitterConnected => _settingsService.Settings.ConnectedAccounts.Contains("twitter");

    public string GoogleConnectStatusText => IsGoogleConnected ? "✓ Conectado" : "Conectar Google";
    public string SpotifyConnectStatusText => IsSpotifyConnected ? "✓ Conectado" : "Conectar Spotify";
    public string SteamConnectStatusText => IsSteamConnected ? "✓ Conectado" : "Conectar Steam";
    public string InstagramConnectStatusText => IsInstagramConnected ? "✓ Conectado" : "Conectar Instagram";
    public string TwitterConnectStatusText => IsTwitterConnected ? "✓ Conectado" : "Conectar X (Twitter)";

    public ICommand ConnectServiceCommand { get; }
    public ICommand DisconnectServiceCommand { get; }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
