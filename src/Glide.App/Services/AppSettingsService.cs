using System.IO;
using System.Text.Json;
using Glide.Core.Models;
using Glide.Core.Security;

namespace Glide.App.Services;

public class AppSettingsService
{
    private static AppSettingsService? _instance;
    public static AppSettingsService Instance => _instance ??= new AppSettingsService();

    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public AppSettingsService(string? customPath = null)
    {
        if (customPath != null)
        {
            _settingsFilePath = customPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string glideDir = Path.Combine(appData, "Glide");
            Directory.CreateDirectory(glideDir);
            _settingsFilePath = Path.Combine(glideDir, "settings.json");
        }

        _settings = LoadSettings();
        ApplyToRuntime();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch {}

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        try
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch {}

        ApplyToRuntime();
    }

    private void ApplyToRuntime()
    {
        StrictNavigationPolicy.GlobalStrictSecurityMode = _settings.StrictSecurityMode;
    }

    public void ResetToDefaults()
    {
        SaveSettings(new AppSettings());
    }

    public void ClearWebViewCache()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string profilePath = Path.Combine(appData, "Glide", "WebViewProfile");
            if (Directory.Exists(profilePath))
            {
                // Limpa pastas de cache temporário do WebView2
                string cachePath = Path.Combine(profilePath, "Default", "Cache");
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }
            }
        }
        catch {}
    }
}

