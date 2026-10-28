using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Glide.App.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace Glide.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TaskbarIcon _taskbarIcon;
    private MiniPlayerWindow? _miniPlayerWindow;
    private bool _isExplicitExit = false;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _taskbarIcon = new TaskbarIcon();
        SetupTaskbarIcon();
    }

    private void SetupTaskbarIcon()
    {
        try
        {
            string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "glide-icon.ico");
            if (System.IO.File.Exists(icoPath))
            {
                _taskbarIcon.Icon = new System.Drawing.Icon(icoPath);
            }
            else
            {
                _taskbarIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Glide.App;component/Assets/glide-logo.png", UriKind.Absolute));
            }
        }
        catch
        {
            // Fallback silencioso
        }

        _taskbarIcon.ToolTipText = "Glide — Gaming Companion Hub";
        _taskbarIcon.TrayMouseDoubleClick += (s, e) => RestoreMainWindow();

        var contextMenu = new ContextMenu();

        var openItem = new MenuItem { Header = "Abrir Glide" };
        openItem.Click += (s, e) => RestoreMainWindow();

        var miniPlayerItem = new MenuItem { Header = "Miniplayer" };
        miniPlayerItem.Click += (s, e) => ToggleMiniPlayer();

        var gamingModeItem = new MenuItem { Header = "Modo Gaming (Alternar)" };
        gamingModeItem.Click += (s, e) => _viewModel.ToggleGamingModeCommand.Execute(null);

        var exitItem = new MenuItem { Header = "Sair do Glide" };
        exitItem.Click += (s, e) => ExplicitExit();

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(miniPlayerItem);
        contextMenu.Items.Add(gamingModeItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
    }

    private void RestoreMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ToggleMiniPlayer()
    {
        if (_miniPlayerWindow == null || !_miniPlayerWindow.IsLoaded)
        {
            _miniPlayerWindow = new MiniPlayerWindow(this, _viewModel);
        }

        if (_miniPlayerWindow.IsVisible)
        {
            _miniPlayerWindow.Hide();
            _viewModel.IsMiniPlayerActive = false;
        }
        else
        {
            _miniPlayerWindow.Show();
            _viewModel.IsMiniPlayerActive = true;
        }
    }

    private void MiniPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMiniPlayer();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit && _viewModel.SettingsVm.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            _taskbarIcon.ShowBalloonTip("Glide", "O Glide continua ativo na bandeja do sistema.", BalloonIcon.Info);
        }
        else
        {
            _taskbarIcon.Dispose();
            _miniPlayerWindow?.Close();
            base.OnClosing(e);
        }
    }

    private void ExplicitExit()
    {
        _isExplicitExit = true;
        _taskbarIcon.Dispose();
        _miniPlayerWindow?.Close();
        Application.Current.Shutdown();
    }
}
