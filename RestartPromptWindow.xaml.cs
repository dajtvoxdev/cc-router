using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CCRouter.Services;

namespace CCRouter;

public partial class RestartPromptWindow : Window
{
    private readonly List<(ProcessInfo Info, CheckBox Checkbox)> _terminalRows = new();
    private readonly List<(ProcessInfo Info, CheckBox Checkbox)> _vsCodeRows = new();

    public List<int> TerminalsToKill { get; private set; } = new();
    public List<int> VsCodeToKill { get; private set; } = new();
    public bool DontAskAgain { get; private set; }
    public bool RestartConfirmed { get; private set; }

    public RestartPromptWindow(string profileName, List<ProcessInfo> terminals, List<ProcessInfo> vsCodes)
    {
        InitializeComponent();
        TxtTitle.Text = $"Switched to {profileName}";

        if (terminals.Count == 0)
        {
            TxtTermSection.Visibility = Visibility.Collapsed;
            PanelTerminals.Visibility = Visibility.Collapsed;
        }
        else
        {
            foreach (var p in terminals)
            {
                var cb = new CheckBox { Content = $"{p.Name} — {Trim(p.WindowTitle, 60)}  (PID {p.Pid})", IsChecked = true };
                PanelTerminals.Children.Add(cb);
                _terminalRows.Add((p, cb));
            }
        }

        if (vsCodes.Count == 0)
        {
            TxtVsSection.Visibility = Visibility.Collapsed;
            PanelVsCode.Visibility = Visibility.Collapsed;
        }
        else
        {
            foreach (var p in vsCodes)
            {
                var cb = new CheckBox { Content = $"{p.Name} — {Trim(p.WindowTitle, 60)}  (PID {p.Pid})", IsChecked = false };
                PanelVsCode.Children.Add(cb);
                _vsCodeRows.Add((p, cb));
            }
        }
    }

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        RestartConfirmed = false;
        DontAskAgain = ChkDontAskAgain.IsChecked == true;
        Close();
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        TerminalsToKill = _terminalRows.Where(r => r.Checkbox.IsChecked == true).Select(r => r.Info.Pid).ToList();
        VsCodeToKill = _vsCodeRows.Where(r => r.Checkbox.IsChecked == true).Select(r => r.Info.Pid).ToList();
        DontAskAgain = ChkDontAskAgain.IsChecked == true;
        RestartConfirmed = true;
        Close();
    }
}
