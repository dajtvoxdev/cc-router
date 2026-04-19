using System.Collections.Generic;

namespace CCRouter.Models;

public class AppSettings
{
    public string? ActiveProfileId { get; set; }
    public bool AutostartEnabled { get; set; }
    public bool StartMinimized { get; set; }
    public bool PromptRestartAfterSwitch { get; set; } = true;
    // profileId -> "MOD+KEY" e.g. "CTRL+ALT+1"
    public Dictionary<string, string> HotkeyBindings { get; set; } = new();
}
