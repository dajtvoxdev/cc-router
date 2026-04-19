using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CCRouter.Services;

public record ProcessInfo(int Pid, string Name, string WindowTitle);

public static class ProcessDetectionService
{
    private static readonly string[] TerminalNames =
    [
        "WindowsTerminal", "wt", "cmd", "powershell", "pwsh",
        "bash", "mintty", "alacritty", "ConEmu64", "ConEmu"
    ];

    private static readonly string[] VsCodeNames = ["Code", "Code - Insiders"];

    public static List<ProcessInfo> DetectTerminals() => Scan(TerminalNames);

    public static List<ProcessInfo> DetectVsCode() => Scan(VsCodeNames);

    private static List<ProcessInfo> Scan(string[] names)
    {
        var result = new List<ProcessInfo>();
        var current = Process.GetCurrentProcess().Id;

        foreach (var name in names)
        {
            Process[] procs;
            try { procs = Process.GetProcessesByName(name); }
            catch { continue; }

            foreach (var p in procs)
            {
                try
                {
                    if (p.Id == current) continue;
                    if (p.MainWindowHandle == IntPtr.Zero) continue;
                    var title = string.IsNullOrWhiteSpace(p.MainWindowTitle) ? p.ProcessName : p.MainWindowTitle;
                    result.Add(new ProcessInfo(p.Id, p.ProcessName, title));
                }
                catch { /* access denied / exited */ }
                finally { p.Dispose(); }
            }
        }

        return result.GroupBy(x => x.Pid).Select(g => g.First()).ToList();
    }
}
