using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Glide.App.Services;

public record UpdateCheckResult(
    bool HasUpdate,
    string CurrentVersion,
    string LatestVersion,
    string? DownloadUrl,
    string? ReleaseNotes,
    string StatusMessage
);

public class AppUpdateService
{
    private static AppUpdateService? _instance;
    public static AppUpdateService Instance => _instance ??= new AppUpdateService();

    public const string CurrentVersion = "v2.0.0";
    private const string GitHubApiReleasesUrl = "https://api.github.com/repos/MiguelCruver/Glide/releases/latest";

    private readonly HttpClient _httpClient;

    public AppUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GlideApp-DesktopUpdater");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(GitHubApiReleasesUrl);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new UpdateCheckResult(
                        HasUpdate: false,
                        CurrentVersion: CurrentVersion,
                        LatestVersion: CurrentVersion,
                        DownloadUrl: null,
                        ReleaseNotes: null,
                        StatusMessage: "Repositório ainda não possui releases públicas publicadas. Você está na versão de desenvolvimento oficial v2.0.0."
                    );
                }

                return new UpdateCheckResult(
                    HasUpdate: false,
                    CurrentVersion: CurrentVersion,
                    LatestVersion: CurrentVersion,
                    DownloadUrl: null,
                    ReleaseNotes: null,
                    StatusMessage: $"Servidor do GitHub respondeu com status: {(int)response.StatusCode}."
                );
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

            string? setupUrl = null;
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    string name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        if (asset.TryGetProperty("browser_download_url", out var urlProp))
                        {
                            setupUrl = urlProp.GetString();
                            if (name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                            {
                                break; // Prefere instalador oficial
                            }
                        }
                    }
                }
            }

            // Normaliza tags removendo 'v' para comparação
            string cleanTag = tagName.TrimStart('v', 'V');
            string cleanCurrent = CurrentVersion.TrimStart('v', 'V');

            bool isNewer = false;
            if (Version.TryParse(cleanTag, out var serverVer) && Version.TryParse(cleanCurrent, out var localVer))
            {
                isNewer = serverVer > localVer;
            }
            else if (!string.IsNullOrWhiteSpace(tagName) && !string.Equals(tagName, CurrentVersion, StringComparison.OrdinalIgnoreCase))
            {
                isNewer = true;
            }

            if (isNewer && !string.IsNullOrEmpty(setupUrl))
            {
                return new UpdateCheckResult(
                    HasUpdate: true,
                    CurrentVersion: CurrentVersion,
                    LatestVersion: tagName,
                    DownloadUrl: setupUrl,
                    ReleaseNotes: body,
                    StatusMessage: $"Nova versão {tagName} disponível para instalação imediata!"
                );
            }

            return new UpdateCheckResult(
                HasUpdate: false,
                CurrentVersion: CurrentVersion,
                LatestVersion: tagName.Length > 0 ? tagName : CurrentVersion,
                DownloadUrl: null,
                ReleaseNotes: null,
                StatusMessage: "O Glide já está atualizado na versão mais recente."
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                HasUpdate: false,
                CurrentVersion: CurrentVersion,
                LatestVersion: CurrentVersion,
                DownloadUrl: null,
                ReleaseNotes: null,
                StatusMessage: $"Não foi possível conectar ao GitHub: {ex.Message}"
            );
        }
    }

    public async Task<string> DownloadUpdateAsync(string downloadUrl, IProgress<int> progress, CancellationToken ct = default)
    {
        string tempDir = Path.GetTempPath();
        string tempInstaller = Path.Combine(tempDir, $"Glide-Setup-{Guid.NewGuid():N}.exe");

        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempInstaller, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read, ct);
            totalRead += read;
            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                int percentage = (int)((totalRead * 100) / totalBytes.Value);
                progress?.Report(percentage);
            }
        }

        return tempInstaller;
    }

    public void LaunchInstallerAndShutdown(string installerPath)
    {
        if (!File.Exists(installerPath)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            };
            Process.Start(psi);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao iniciar o instalador de atualização: {ex.Message}", "Atualização do Glide", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
