using CCRouter.Models;

namespace CCRouter.Services;

public static class EnvironmentSwitcher
{
    private static readonly string[] AllVars =
    [
        "ANTHROPIC_BASE_URL",
        "ANTHROPIC_AUTH_TOKEN",
        "API_TIMEOUT_MS",
        "ANTHROPIC_DEFAULT_OPUS_MODEL",
        "ANTHROPIC_DEFAULT_SONNET_MODEL",
        "ANTHROPIC_DEFAULT_HAIKU_MODEL",
    ];

    public static void Apply(Profile p)
    {
        if (p.IsDefaultClaude)
        {
            foreach (var v in AllVars)
                SetUser(v, null);
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

        SetUser("API_TIMEOUT_MS", string.IsNullOrWhiteSpace(p.TimeoutMs) ? null : p.TimeoutMs);
        SetUser("ANTHROPIC_DEFAULT_OPUS_MODEL", string.IsNullOrWhiteSpace(p.OpusModel) ? null : p.OpusModel);
        SetUser("ANTHROPIC_DEFAULT_SONNET_MODEL", string.IsNullOrWhiteSpace(p.SonnetModel) ? null : p.SonnetModel);
        SetUser("ANTHROPIC_DEFAULT_HAIKU_MODEL", string.IsNullOrWhiteSpace(p.HaikuModel) ? null : p.HaikuModel);
    }

    public static string? ReadCurrentBaseUrl() =>
        Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL", EnvironmentVariableTarget.User);

    private static void SetUser(string name, string? value) =>
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
}
