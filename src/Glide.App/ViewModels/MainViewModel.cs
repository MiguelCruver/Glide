using System.Windows.Input;
using Glide.Core.Enums;
using Glide.Core.Models;
using Glide.Core.Services;

namespace Glide.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceCatalog _catalog;
    private GlideModule _currentModule = GlideModule.Home;
    private bool _isGamingMode;
    private AudioTrack _currentTrack;
    private bool _isMiniPlayerActive;

    public MainViewModel(IServiceCatalog? catalog = null)
    {
        _catalog = catalog ?? new DefaultServiceCatalog();
        _currentTrack = new AudioTrack(
            Title: "Noites Calmas (Lofi Gaming Mix)",
            Artist: "Glide Studio Sessions",
            Album: "Focus & Play Vol. 1",
            Duration: TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(45)),
            Position: TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(20)),
            State: AudioPlaybackState.Playing
        );

        // Sub-ViewModels
        HomeVm = new HomeViewModel(this);
        AudioVm = new AudioViewModel(this, _catalog);
        VideoVm = new VideoViewModel(this, _catalog);
        NotificationsVm = new NotificationsViewModel(this, _catalog);
        ConnectVm = new ConnectViewModel(this);
        SettingsVm = new SettingsViewModel(this);

        // Comandos de Navegação
        NavigateCommand = new RelayCommand(p =>
        {
            if (p is GlideModule module)
                CurrentModule = module;
            else if (p is string moduleStr && Enum.TryParse<GlideModule>(moduleStr, true, out var parsed))
                CurrentModule = parsed;
        });

        ToggleGamingModeCommand = new RelayCommand(() =>
        {
            IsGamingMode = !IsGamingMode;
        });

        TogglePlayPauseCommand = new RelayCommand(() =>
        {
            var newState = CurrentTrack.State == AudioPlaybackState.Playing 
                ? AudioPlaybackState.Paused 
                : AudioPlaybackState.Playing;
            CurrentTrack = CurrentTrack with { State = newState };
        });

        NextTrackCommand = new RelayCommand(() =>
        {
            CurrentTrack = CurrentTrack with
            {
                Title = "Neon Pulse (Synthwave)",
                Artist = "Retro Wave Labs",
                Position = TimeSpan.Zero
            };
        });

        PreviousTrackCommand = new RelayCommand(() =>
        {
            CurrentTrack = CurrentTrack with
            {
                Title = "Chill Beats for Comp",
                Artist = "CS2 Community Vibes",
                Position = TimeSpan.Zero
            };
        });
    }

    public IServiceCatalog Catalog => _catalog;

    public GlideModule CurrentModule
    {
        get => _currentModule;
        set
        {
            if (SetProperty(ref _currentModule, value))
            {
                OnPropertyChanged(nameof(CurrentView));
                OnPropertyChanged(nameof(IsHomeSelected));
                OnPropertyChanged(nameof(IsAudioSelected));
                OnPropertyChanged(nameof(IsVideoSelected));
                OnPropertyChanged(nameof(IsNotificationsSelected));
                OnPropertyChanged(nameof(IsConnectSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
            }
        }
    }

    public bool IsHomeSelected => CurrentModule == GlideModule.Home;
    public bool IsAudioSelected => CurrentModule == GlideModule.Audio;
    public bool IsVideoSelected => CurrentModule == GlideModule.Video;
    public bool IsNotificationsSelected => CurrentModule == GlideModule.Notifications;
    public bool IsConnectSelected => CurrentModule == GlideModule.Connect;
    public bool IsSettingsSelected => CurrentModule == GlideModule.Settings;

    public object CurrentView => CurrentModule switch
    {
        GlideModule.Home => HomeVm,
        GlideModule.Audio => AudioVm,
        GlideModule.Video => VideoVm,
        GlideModule.Notifications => NotificationsVm,
        GlideModule.Connect => ConnectVm,
        GlideModule.Settings => SettingsVm,
        _ => HomeVm
    };

    public bool IsGamingMode
    {
        get => _isGamingMode;
        set
        {
            if (SetProperty(ref _isGamingMode, value))
            {
                OnPropertyChanged(nameof(GamingModeStatusText));
            }
        }
    }

    public string GamingModeStatusText => IsGamingMode ? "MODO GAMING: ATIVADO" : "MODO GAMING: DESATIVADO";

    public AudioTrack CurrentTrack
    {
        get => _currentTrack;
        set
        {
            if (SetProperty(ref _currentTrack, value))
            {
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(PlayPauseIconGeometry));
            }
        }
    }

    public bool IsPlaying => CurrentTrack.State == AudioPlaybackState.Playing;

    public System.Windows.Media.Geometry PlayPauseIconGeometry => IsPlaying
        ? System.Windows.Media.Geometry.Parse("M 3 2 H 6 V 14 H 3 Z M 10 2 H 13 V 14 H 10 Z")
        : System.Windows.Media.Geometry.Parse("M 3 2 L 13 8 L 3 14 Z");

    public bool IsMiniPlayerActive
    {
        get => _isMiniPlayerActive;
        set => SetProperty(ref _isMiniPlayerActive, value);
    }

    public HomeViewModel HomeVm { get; }
    public AudioViewModel AudioVm { get; }
    public VideoViewModel VideoVm { get; }
    public NotificationsViewModel NotificationsVm { get; }
    public ConnectViewModel ConnectVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public ICommand NavigateCommand { get; }
    public ICommand ToggleGamingModeCommand { get; }
    public ICommand TogglePlayPauseCommand { get; }
    public ICommand NextTrackCommand { get; }
    public ICommand PreviousTrackCommand { get; }
}
