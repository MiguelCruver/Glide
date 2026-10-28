using System.IO;
using Microsoft.Web.WebView2.Core;

namespace Glide.App.Helpers;

public static class WebViewHelper
{
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly object _lock = new();

    public static async Task<CoreWebView2Environment> GetOrCreateEnvironmentAsync()
    {
        if (_sharedEnvironment != null) return _sharedEnvironment;

        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Glide",
            "WebViewProfile"
        );

        Directory.CreateDirectory(userDataFolder);

        var options = new CoreWebView2EnvironmentOptions(
            additionalBrowserArguments: "--disable-features=Translate,OptimizationHints --autoplay-policy=no-user-gesture-required"
        );

        _sharedEnvironment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder,
            options: options
        );

        return _sharedEnvironment;
    }
}

