using System;
using System.Collections.Generic;

namespace CCRouter.Models;

public class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public bool IsDefaultClaude { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthTokenEncrypted { get; set; } // base64-encoded DPAPI blob
    // When true, also sets ANTHROPIC_API_KEY (in addition to ANTHROPIC_AUTH_TOKEN)
    public bool UseApiKey { get; set; } = true;
    public string? TimeoutMs { get; set; }
    public string? OpusModel { get; set; }
    public string? SonnetModel { get; set; }
    public string? HaikuModel { get; set; }
    // Extra env vars: key=value pairs set alongside the standard ones
    public Dictionary<string, string>? ExtraEnvVars { get; set; }
    public string? Note { get; set; }
}
