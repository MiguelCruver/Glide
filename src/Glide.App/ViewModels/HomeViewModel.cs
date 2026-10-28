using System.Windows;
using System.Windows.Input;
using Glide.App.Services;
using Glide.Core.Enums;

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
        if (!settings.ConnectedAccounts.Contains(serviceKey))
        {
            settings.ConnectedAccounts.Add(serviceKey);
            _settingsService.SaveSettings(settings);
            NotifyAccountProperties();
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

    public string GoogleConnectStatusText => IsGoogleConnected ? "✓ Conectado (Abrir)" : "Conectar Google";
    public string SpotifyConnectStatusText => IsSpotifyConnected ? "✓ Conectado (Abrir)" : "Conectar Spotify";
    public string SteamConnectStatusText => IsSteamConnected ? "✓ Conectado (Abrir)" : "Conectar Steam";
    public string InstagramConnectStatusText => IsInstagramConnected ? "✓ Conectado (Abrir)" : "Conectar Instagram";
    public string TwitterConnectStatusText => IsTwitterConnected ? "✓ Conectado (Abrir)" : "Conectar X (Twitter)";

    public ICommand ConnectServiceCommand { get; }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
