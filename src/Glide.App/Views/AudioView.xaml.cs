using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Glide.App.Helpers;
using Glide.App.ViewModels;
using Glide.Core.Models;
using Glide.Core.Security;
using Glide.Core.Services;

namespace Glide.App.Views;

public partial class AudioView : UserControl
{
    private readonly INavigationPolicy _policy;
    private readonly IServiceCatalog _catalog;
    private ServiceDescriptor _serviceDescriptor;
    private bool _isInitialized = false;

    public AudioView()
    {
        InitializeComponent();
        _policy = new StrictNavigationPolicy();
        _catalog = new DefaultServiceCatalog();
        _serviceDescriptor = _catalog.GetServiceById("ytmusic")!;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;

        try
        {
            var env = await WebViewHelper.GetOrCreateEnvironmentAsync();
            await AudioWebView.EnsureCoreWebView2Async(env);

            _isInitialized = true;
            AudioLoadingOverlay.Visibility = Visibility.Collapsed;

            AudioWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // Interceptador de navegação - Allowlist rígida de acordo com o provedor ativo
            AudioWebView.CoreWebView2.NavigationStarting += (s, args) =>
            {
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    if (!_policy.IsAllowed(_serviceDescriptor, uri))
                    {
                        args.Cancel = true;
                    }
                }
            };

            AudioWebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                AudioLoadingOverlay.Visibility = Visibility.Collapsed;
            };

            AudioWebView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    if (_policy.IsAllowed(_serviceDescriptor, uri))
                    {
                        AudioWebView.CoreWebView2.Navigate(args.Uri);
                    }
                }
            };

            // Escuta mensagens do JavaScript injetado para metadados da faixa (YouTube Music e Spotify)
            AudioWebView.CoreWebView2.WebMessageReceived += (s, args) =>
            {
                try
                {
                    string json = args.TryGetWebMessageAsString();
                    if (string.IsNullOrWhiteSpace(json)) return;

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "trackInfo")
                    {
                        string title = root.GetProperty("title").GetString() ?? "";
                        string artist = root.GetProperty("artist").GetString() ?? "";

                        if (!string.IsNullOrWhiteSpace(title) && DataContext is AudioViewModel vm)
                        {
                            vm.Main.CurrentTrack = vm.Main.CurrentTrack with
                            {
                                Title = title,
                                Artist = string.IsNullOrWhiteSpace(artist) ? _serviceDescriptor.Name : artist
                            };
                        }
                    }
                }
                catch
                {
                    // Ignora falhas de parse de mensagens web
                }
            };

            // Injeta o observador de metadados assim que o DOM carregar
            AudioWebView.CoreWebView2.DOMContentLoaded += async (s, args) =>
            {
                string script = @"
                    setInterval(() => {
                        try {
                            // YouTube Music
                            let titleEl = document.querySelector('.title.ytmusic-player-bar, yt-formatted-string.title');
                            let artistEl = document.querySelector('.byline.ytmusic-player-bar, yt-formatted-string.byline');
                            
                            // Spotify Web Player
                            if (!titleEl || !titleEl.textContent) {
                                titleEl = document.querySelector('[data-testid=""context-item-info-title""]') ||
                                          document.querySelector('[data-testid=""now-playing-widget""] [data-testid=""context-item-info-title""]');
                                artistEl = document.querySelector('[data-testid=""context-item-info-subtitles""]') ||
                                           document.querySelector('[data-testid=""now-playing-widget""] [data-testid=""context-item-info-subtitles""]');
                            }

                            if (titleEl && titleEl.textContent) {
                                window.chrome.webview.postMessage(JSON.stringify({
                                    type: 'trackInfo',
                                    title: titleEl.textContent.trim(),
                                    artist: artistEl ? artistEl.textContent.trim() : ''
                                }));
                            }
                        } catch(e) {}
                    }, 2000);
                ";
                await AudioWebView.CoreWebView2.ExecuteScriptAsync(script);
            };

            AudioWebView.CoreWebView2.Navigate(_serviceDescriptor.BaseUrl);
        }
        catch (Exception ex)
        {
            AudioLoadingOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Não foi possível inicializar o reprodutor de áudio: {ex.Message}", "Glide - Áudio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void YtMusicTabBtn_Click(object sender, RoutedEventArgs e)
    {
        SwitchService("ytmusic");
    }

    private void SpotifyTabBtn_Click(object sender, RoutedEventArgs e)
    {
        SwitchService("spotify");
    }

    private void SwitchService(string serviceId)
    {
        var svc = _catalog.GetServiceById(serviceId);
        if (svc == null || svc.Id == _serviceDescriptor.Id) return;

        _serviceDescriptor = svc;
        ProviderNameTxt.Text = svc.Name;

        var primaryStyle = (Style)FindResource("PrimaryButtonStyle");
        var secondaryStyle = (Style)FindResource("SecondaryButtonStyle");

        if (serviceId == "ytmusic")
        {
            YtMusicTabBtn.Style = primaryStyle;
            SpotifyTabBtn.Style = secondaryStyle;
        }
        else
        {
            SpotifyTabBtn.Style = primaryStyle;
            YtMusicTabBtn.Style = secondaryStyle;
        }

        if (_isInitialized && AudioWebView.CoreWebView2 != null)
        {
            AudioLoadingOverlay.Visibility = Visibility.Visible;
            AudioWebView.CoreWebView2.Navigate(svc.BaseUrl);
        }
    }

    private void EcoModeToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        if (WebContainer.Visibility == Visibility.Visible)
        {
            WebContainer.Visibility = Visibility.Collapsed;
            EcoContainer.Visibility = Visibility.Visible;
            EcoModeToggleBtn.Content = "🖥️ Ver Tela Web";
        }
        else
        {
            EcoContainer.Visibility = Visibility.Collapsed;
            WebContainer.Visibility = Visibility.Visible;
            EcoModeToggleBtn.Content = "🌿 Alternar Modo Eco";
        }
    }

    private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
    {
        AudioWebView.CoreWebView2?.ExecuteScriptAsync(@"
            const ytPlay = document.querySelector('#play-pause-button, .play-pause-button');
            if (ytPlay) { ytPlay.click(); }
            else {
                const spPlay = document.querySelector('[data-testid=""control-button-playpause""]');
                if (spPlay) spPlay.click();
            }
        ");
        if (DataContext is AudioViewModel vm)
        {
            vm.Main.TogglePlayPauseCommand.Execute(null);
        }
    }

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        AudioWebView.CoreWebView2?.ExecuteScriptAsync(@"
            const ytNext = document.querySelector('.next-button, #next-button');
            if (ytNext) { ytNext.click(); }
            else {
                const spNext = document.querySelector('[data-testid=""control-button-skip-forward""]');
                if (spNext) spNext.click();
            }
        ");
        if (DataContext is AudioViewModel vm)
        {
            vm.Main.NextTrackCommand.Execute(null);
        }
    }

    private void PreviousBtn_Click(object sender, RoutedEventArgs e)
    {
        AudioWebView.CoreWebView2?.ExecuteScriptAsync(@"
            const ytPrev = document.querySelector('.previous-button, #previous-button');
            if (ytPrev) { ytPrev.click(); }
            else {
                const spPrev = document.querySelector('[data-testid=""control-button-skip-back""]');
                if (spPrev) spPrev.click();
            }
        ");
        if (DataContext is AudioViewModel vm)
        {
            vm.Main.PreviousTrackCommand.Execute(null);
        }
    }

    private void MiniPlayerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            var method = typeof(MainWindow).GetMethod("ToggleMiniPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(main, null);
        }
    }
}
