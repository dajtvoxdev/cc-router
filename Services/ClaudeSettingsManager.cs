using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CCRouter.Models;

namespace CCRouter.Services;

public static class ClaudeSettingsManager
{
    public static string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude",
        "settings.json");

    private static readonly string[] BuiltInKeys =
    [
        "ANTHROPIC_BASE_URL",
        "ANTHROPIC_AUTH_TOKEN",
        "ANTHROPIC_API_KEY",
        "API_TIMEOUT_MS",
        "ANTHROPIC_DEFAULT_OPUS_MODEL",
        "ANTHROPIC_DEFAULT_SONNET_MODEL",
        "ANTHROPIC_DEFAULT_HAIKU_MODEL",
    ];

    // Track extra keys written by the previous Apply() so they can be removed on next switch.
    private static string[] _prevExtraKeys = [];

    public static void Apply(Profile p)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        var root = LoadRoot();
        BackupOnce();

        var env = root["env"] as JsonObject ?? new JsonObject();

        // Remove built-in keys
        foreach (var k in BuiltInKeys)
            env.Remove(k);

        // Remove extra keys from the previous profile
        foreach (var k in _prevExtraKeys)
            env.Remove(k);

        _prevExtraKeys = [];

        if (!p.IsDefaultClaude)
        {
            SetIfPresent(env, "ANTHROPIC_BASE_URL", p.BaseUrl);

            string? token = null;
            if (!string.IsNullOrWhiteSpace(p.AuthTokenEncrypted))
            {
                try { token = SecretProtector.Unprotect(p.AuthTokenEncrypted); }
                catch { token = null; }
            }
            SetIfPresent(env, "ANTHROPIC_AUTH_TOKEN", token);
            if (p.UseApiKey)
                SetIfPresent(env, "ANTHROPIC_API_KEY", token);

            SetIfPresent(env, "API_TIMEOUT_MS", p.TimeoutMs);
            SetIfPresent(env, "ANTHROPIC_DEFAULT_OPUS_MODEL", p.OpusModel);
            SetIfPresent(env, "ANTHROPIC_DEFAULT_SONNET_MODEL", p.SonnetModel);
            SetIfPresent(env, "ANTHROPIC_DEFAULT_HAIKU_MODEL", p.HaikuModel);

            if (p.ExtraEnvVars != null)
            {
                var extraKeys = new System.Collections.Generic.List<string>();
                foreach (var (k, v) in p.ExtraEnvVars)
                {
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    if (!string.IsNullOrWhiteSpace(v))
                        env[k] = v;
                    extraKeys.Add(k);
                }
                _prevExtraKeys = extraKeys.ToArray();
            }
        }

        if (env.Count == 0) root.Remove("env");
        else root["env"] = env;

        SaveAtomic(root);
    }

    private static void SetIfPresent(JsonObject env, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            env[key] = value;
    }

    private static JsonObject LoadRoot()
    {
        if (!File.Exists(SettingsPath)) return new JsonObject();
        try
        {
            var text = File.ReadAllText(SettingsPath);
            if (string.IsNullOrWhiteSpace(text)) return new JsonObject();
            return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
        }
        catch { return new JsonObject(); }
    }

    // Keep one rolling backup so first write doesn't lose user's manual edits.
    private static void BackupOnce()
    {
        if (!File.Exists(SettingsPath)) return;
        var bak = SettingsPath + ".bak";
        try { File.Copy(SettingsPath, bak, overwrite: true); }
        catch { /* non-fatal */ }
    }

    private static void SaveAtomic(JsonObject root)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = root.ToJsonString(opts);
        var tmp = SettingsPath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, SettingsPath, overwrite: true);
    }
}
