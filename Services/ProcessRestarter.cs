using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CCRouter.Services;

public static class ProcessRestarter
{
    public static void KillAndRelaunchTerminals(IEnumerable<int> pids)
    {
        bool anyKilled = KillAll(pids);
        if (!anyKilled) return;

        try
        {
            // Prefer Windows Terminal if available, fallback to PowerShell.
            var wt = ResolveOnPath("wt.exe");
            if (wt != null)
            {
                Process.Start(new ProcessStartInfo(wt) { UseShellExecute = true });
                return;
            }

            var pwsh = ResolveOnPath("pwsh.exe") ?? ResolveOnPath("powershell.exe") ?? "powershell.exe";
            Process.Start(new ProcessStartInfo(pwsh) { UseShellExecute = true });
        }
        catch { /* user can manually open terminal */ }
    }

    public static void KillAndRelaunchVsCode(IEnumerable<int> pids)
    {
        bool anyKilled = KillAll(pids);
        if (!anyKilled) return;

        try
        {
            // `code` is normally a .cmd shim; use ShellExecute so it resolves via PATH.
            Process.Start(new ProcessStartInfo("cmd.exe", "/c code")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { /* user can manually reopen */ }
    }

    private static bool KillAll(IEnumerable<int> pids)
    {
        bool any = false;
        foreach (var pid in pids)
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                p.Kill(entireProcessTree: true);
                p.WaitForExit(2000);
                any = true;
            }
            catch { /* already exited / no permission */ }
        }
        return any;
    }

    private static string? ResolveOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            try
            {
                var full = Path.Combine(dir, exe);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }
}
