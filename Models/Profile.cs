using System;

namespace CCRouter.Models;

public class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public bool IsDefaultClaude { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthTokenEncrypted { get; set; } // base64-encoded DPAPI blob
    public string? TimeoutMs { get; set; }
    public string? OpusModel { get; set; }
    public string? SonnetModel { get; set; }
    public string? HaikuModel { get; set; }
    public string? Note { get; set; }
}
