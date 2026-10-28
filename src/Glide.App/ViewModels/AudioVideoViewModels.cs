using System.Collections.ObjectModel;
using System.Windows.Input;
using Glide.Core.Enums;
using Glide.Core.Models;
using Glide.Core.Services;

namespace Glide.App.ViewModels;

public class AudioViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    private ServiceDescriptor? _selectedService;
    private bool _ecoAudioMode = true;
    private int _volume = 80;

    public AudioViewModel(MainViewModel mainVm, IServiceCatalog catalog)
    {
        _mainVm = mainVm;
        Services = new ObservableCollection<ServiceDescriptor>(catalog.GetServicesByCategory(ServiceCategory.Audio));
        _selectedService = Services.FirstOrDefault();

        SelectServiceCommand = new RelayCommand(p =>
        {
            if (p is ServiceDescriptor s) SelectedService = s;
        });

        ToggleEcoModeCommand = new RelayCommand(() => EcoAudioMode = !EcoAudioMode);
    }

    public ObservableCollection<ServiceDescriptor> Services { get; }

    public ServiceDescriptor? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    public bool EcoAudioMode
    {
        get => _ecoAudioMode;
        set => SetProperty(ref _ecoAudioMode, value);
    }

    public int Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public MainViewModel Main => _mainVm;
    public ICommand SelectServiceCommand { get; }
    public ICommand ToggleEcoModeCommand { get; }
}

public class VideoViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    private ServiceDescriptor? _selectedService;
    private string _selectedMonitor = "Monitor 2 (Secundário - 1080p)";
    private bool _isSuspended;

    public VideoViewModel(MainViewModel mainVm, IServiceCatalog catalog)
    {
        _mainVm = mainVm;
        Services = new ObservableCollection<ServiceDescriptor>(catalog.GetServicesByCategory(ServiceCategory.Video));
        _selectedService = Services.FirstOrDefault();
        Monitors = new ObservableCollection<string>
        {
            "Monitor 1 (Principal - 165Hz)",
            "Monitor 2 (Secundário - 1080p)"
        };

        ToggleSuspendCommand = new RelayCommand(() => IsSuspended = !IsSuspended);
    }

    public ObservableCollection<ServiceDescriptor> Services { get; }
    public ObservableCollection<string> Monitors { get; }

    public ServiceDescriptor? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    public string SelectedMonitor
    {
        get => _selectedMonitor;
        set => SetProperty(ref _selectedMonitor, value);
    }

    public bool IsSuspended
    {
        get => _isSuspended;
        set
        {
            if (SetProperty(ref _isSuspended, value))
            {
                OnPropertyChanged(nameof(SuspendButtonText));
            }
        }
    }

    public string SuspendButtonText => IsSuspended ? "▶ Retomar Módulo" : "⏸ Suspender Módulo";

    public MainViewModel Main => _mainVm;
    public ICommand ToggleSuspendCommand { get; }
}

