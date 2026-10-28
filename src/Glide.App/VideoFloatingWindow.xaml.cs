using System.Windows;
using Glide.App.Helpers;

namespace Glide.App;

public partial class VideoFloatingWindow : Window
{
    private readonly string _initialUrl;

    public VideoFloatingWindow(string initialUrl)
    {
        InitializeComponent();
        _initialUrl = string.IsNullOrWhiteSpace(initialUrl) ? "https://www.youtube.com" : initialUrl;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await WebViewHelper.GetOrCreateEnvironmentAsync();
            await FloatingWebView.EnsureCoreWebView2Async(env);
            FloatingWebView.CoreWebView2.Navigate(_initialUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar vídeo flutuante: {ex.Message}", "Glide", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

