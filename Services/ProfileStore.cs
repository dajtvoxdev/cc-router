using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CCRouter.Models;

namespace CCRouter.Services;

public class ProfileStore
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CCRouter");

    private static readonly string ProfilesFile = Path.Combine(DataDir, "profiles.json");
    private static readonly string SettingsFile = Path.Combine(DataDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<Profile> LoadProfiles()
    {
        EnsureDir();
        if (!File.Exists(ProfilesFile)) return new();
        try
        {
            var json = File.ReadAllText(ProfilesFile);
            return JsonSerializer.Deserialize<List<Profile>>(json, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public void SaveProfiles(List<Profile> profiles)
    {
        EnsureDir();
        AtomicWrite(ProfilesFile, JsonSerializer.Serialize(profiles, JsonOpts));
    }

    public AppSettings LoadSettings()
    {
        EnsureDir();
        if (!File.Exists(SettingsFile)) return new();
        try
        {
            var json = File.ReadAllText(SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public void SaveSettings(AppSettings settings)
    {
        EnsureDir();
        AtomicWrite(SettingsFile, JsonSerializer.Serialize(settings, JsonOpts));
    }

    private static void EnsureDir() => Directory.CreateDirectory(DataDir);

    private static void AtomicWrite(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }
}
