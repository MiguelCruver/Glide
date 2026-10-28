using System.IO;
using System.Text.Json;
using Glide.Core.Models;

namespace Glide.App.Services;

public class UserProfileService
{
    private static UserProfileService? _instance;
    public static UserProfileService Instance => _instance ??= new UserProfileService();

    private readonly string _profileDir;
    private readonly string _profileJsonPath;
    private UserProfile _currentProfile;

    public event Action<UserProfile>? ProfileUpdated;

    public UserProfile CurrentProfile => _currentProfile;

    public UserProfileService(string? customDir = null)
    {
        if (customDir != null)
        {
            _profileDir = customDir;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _profileDir = Path.Combine(appData, "Glide", "Profile");
        }

        Directory.CreateDirectory(_profileDir);
        _profileJsonPath = Path.Combine(_profileDir, "profile.json");
        _currentProfile = LoadProfile();
    }

    private UserProfile LoadProfile()
    {
        try
        {
            if (File.Exists(_profileJsonPath))
            {
                string json = File.ReadAllText(_profileJsonPath);
                var loaded = JsonSerializer.Deserialize<UserProfile>(json);
                if (loaded != null) return loaded;
            }
        }
        catch {}

        return new UserProfile(
            Nickname: "Raposo",
            StatusMessage: "Online no Glide • Focado no CS2",
            AvatarPath: null,
            BannerPath: null
        );
    }

    public void SaveProfile(UserProfile profile)
    {
        _currentProfile = profile;
        try
        {
            string json = JsonSerializer.Serialize(_currentProfile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_profileJsonPath, json);
        }
        catch {}

        ProfileUpdated?.Invoke(_currentProfile);
    }

    public string SetAvatar(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return string.Empty;

        string ext = Path.GetExtension(sourceFilePath);
        string destPath = Path.Combine(_profileDir, $"avatar{ext}");
        File.Copy(sourceFilePath, destPath, true);

        SaveProfile(_currentProfile with { AvatarPath = destPath });
        return destPath;
    }

    public string SetBanner(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return string.Empty;

        string ext = Path.GetExtension(sourceFilePath);
        string destPath = Path.Combine(_profileDir, $"banner{ext}");
        File.Copy(sourceFilePath, destPath, true);

        SaveProfile(_currentProfile with { BannerPath = destPath });
        return destPath;
    }
}

