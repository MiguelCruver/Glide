using System.Windows;
using System.Windows.Controls;
using Glide.App.Helpers;
using Glide.Core.Models;
using Glide.Core.Security;
using Glide.Core.Services;

namespace Glide.App.Views;

public partial class VideoView : UserControl
{
    private readonly INavigationPolicy _policy;
    private readonly IServiceCatalog _catalog;
    private ServiceDescriptor _serviceDescriptor;
    private bool _isInitialized = false;

    public VideoView()
    {
        InitializeComponent();
        _policy = new StrictNavigationPolicy();
        _catalog = new DefaultServiceCatalog();
        _serviceDescriptor = _catalog.GetServiceById("youtube")!;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;

        try
        {
            var env = await WebViewHelper.GetOrCreateEnvironmentAsync();
            await VideoWebView.EnsureCoreWebView2Async(env);

            _isInitialized = true;
            LoadingOverlay.Visibility = Visibility.Collapsed;

            // Configurações de segurança e desempenho do motor Chromium
            VideoWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            VideoWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            // Interceptador de navegação - Allowlist dinâmica de acordo com o serviço ativo
            VideoWebView.CoreWebView2.NavigationStarting += (s, args) =>
            {
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    if (!_policy.IsAllowed(_serviceDescriptor, uri))
                    {
                        args.Cancel = true;
                    }
                }
            };

            VideoWebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            };

            // Interceptador de nova janela / popups
            VideoWebView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    if (_policy.IsAllowed(_serviceDescriptor, uri))
                    {
                        VideoWebView.CoreWebView2.Navigate(args.Uri);
                    }
                }
            };

            VideoWebView.CoreWebView2.Navigate(_serviceDescriptor.BaseUrl);
        }
        catch (Exception ex)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Não foi possível inicializar o reprodutor de vídeo: {ex.Message}", "Glide - Vídeo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void YouTubeTabBtn_Click(object sender, RoutedEventArgs e)
    {
        SwitchService("youtube");
    }

    private void InstagramTabBtn_Click(object sender, RoutedEventArgs e)
    {
        SwitchService("instagram");
    }

    private void TwitterTabBtn_Click(object sender, RoutedEventArgs e)
    {
        SwitchService("twitter");
    }

    private void SwitchService(string serviceId)
    {
        var svc = _catalog.GetServiceById(serviceId);
        if (svc == null || svc.Id == _serviceDescriptor.Id) return;

        _serviceDescriptor = svc;
        VideoServiceNameTxt.Text = svc.Name;

        var primaryStyle = (Style)FindResource("PrimaryButtonStyle");
        var secondaryStyle = (Style)FindResource("SecondaryButtonStyle");

        YouTubeTabBtn.Style = serviceId == "youtube" ? primaryStyle : secondaryStyle;
        InstagramTabBtn.Style = serviceId == "instagram" ? primaryStyle : secondaryStyle;
        TwitterTabBtn.Style = serviceId == "twitter" ? primaryStyle : secondaryStyle;

        if (_isInitialized && VideoWebView.CoreWebView2 != null)
        {
            LoadingText.Text = $"Carregando {svc.Name}...";
            LoadingOverlay.Visibility = Visibility.Visible;
            VideoWebView.CoreWebView2.Navigate(svc.BaseUrl);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoWebView.CoreWebView2 != null && VideoWebView.CanGoBack)
        {
            VideoWebView.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoWebView.CoreWebView2 != null && VideoWebView.CanGoForward)
        {
            VideoWebView.GoForward();
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        VideoWebView.CoreWebView2?.Reload();
    }

    private void PopoutButton_Click(object sender, RoutedEventArgs e)
    {
        string currentUrl = VideoWebView.Source?.ToString() ?? _serviceDescriptor.BaseUrl;
        var floatingWindow = new VideoFloatingWindow(currentUrl);
        floatingWindow.Show();
    }

    private void SuspendButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoWebView.Visibility == Visibility.Visible)
        {
            VideoWebView.Visibility = Visibility.Collapsed;
            SuspendedOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            ResumeButton_Click(sender, e);
        }
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        SuspendedOverlay.Visibility = Visibility.Collapsed;
        VideoWebView.Visibility = Visibility.Visible;
    }
}
