using System;
using System.Collections.Generic;
using CCRouter.Models;

namespace CCRouter.Services;

public static class EnvironmentSwitcher
{
    private static readonly string[] BuiltInVars =
    [
        "ANTHROPIC_BASE_URL",
        "ANTHROPIC_AUTH_TOKEN",
        "ANTHROPIC_API_KEY",
        "API_TIMEOUT_MS",
        "ANTHROPIC_DEFAULT_OPUS_MODEL",
        "ANTHROPIC_DEFAULT_SONNET_MODEL",
        "ANTHROPIC_DEFAULT_HAIKU_MODEL",
    ];

    // Extra vars from the previous active profile that we need to clear on switch.
    // Stored so we can clean up keys not present in the new profile.
    private static HashSet<string> _prevExtraKeys = [];

    public static void Apply(Profile p)
    {
        // Clear all built-in vars first
        foreach (var v in BuiltInVars)
            SetUser(v, null);

        // Clear any extra vars from the previous profile
        foreach (var k in _prevExtraKeys)
            SetUser(k, null);

        if (p.IsDefaultClaude)
        {
            _prevExtraKeys = [];
            return;
        }

        SetUser("ANTHROPIC_BASE_URL", string.IsNullOrWhiteSpace(p.BaseUrl) ? null : p.BaseUrl);

        string? token = null;
        if (!string.IsNullOrWhiteSpace(p.AuthTokenEncrypted))
        {
            try { token = SecretProtector.Unprotect(p.AuthTokenEncrypted); }
            catch { token = null; }
        }
        SetUser("ANTHROPIC_AUTH_TOKEN", token);
        if (p.UseApiKey)
            SetUser("ANTHROPIC_API_KEY", token);

        SetUser("API_TIMEOUT_MS", string.IsNullOrWhiteSpace(p.TimeoutMs) ? null : p.TimeoutMs);
        SetUser("ANTHROPIC_DEFAULT_OPUS_MODEL", string.IsNullOrWhiteSpace(p.OpusModel) ? null : p.OpusModel);
        SetUser("ANTHROPIC_DEFAULT_SONNET_MODEL", string.IsNullOrWhiteSpace(p.SonnetModel) ? null : p.SonnetModel);
        SetUser("ANTHROPIC_DEFAULT_HAIKU_MODEL", string.IsNullOrWhiteSpace(p.HaikuModel) ? null : p.HaikuModel);

        _prevExtraKeys = [];
        if (p.ExtraEnvVars != null)
        {
            foreach (var (k, v) in p.ExtraEnvVars)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                SetUser(k, string.IsNullOrWhiteSpace(v) ? null : v);
                _prevExtraKeys.Add(k);
            }
        }
    }

    public static string? ReadCurrentBaseUrl() =>
        Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL", EnvironmentVariableTarget.User);

    private static void SetUser(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        // Also update process-level so current session and children pick up the change
        Environment.SetEnvironmentVariable(name, value);
    }
}
