using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using CCRouter.Models;
using CCRouter.Services;

namespace CCRouter;

public partial class App : Application
{
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    public ProfileStore Store { get; } = new();
    public List<Profile> Profiles { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();
    public HotkeyService HotkeyService { get; } = new();

    private readonly Dictionary<string, int> _registeredHotkeys = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!AcquireMutex()) { Shutdown(); return; }

        Profiles = Store.LoadProfiles();
        Settings = Store.LoadSettings();

        _trayIcon = BuildTrayIcon();
        _mainWindow = new MainWindow();

        RegisterAllHotkeys();

        bool startHidden = e.Args.Contains("--tray") || Settings.StartMinimized;
        if (!startHidden)
            ShowMainWindow();
    }

    private bool AcquireMutex()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? "default";
        _mutex = new Mutex(true, $"Local\\CCRouter_{sid}", out bool created);
        return created;
    }

    // ---------- Tray ----------

    private TaskbarIcon BuildTrayIcon()
    {
        var icon = new TaskbarIcon
        {
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Resources/app.ico")),
            ToolTipText = "CCRouter",
            ContextMenu = BuildContextMenu()
        };
        icon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
        return icon;
    }

    public ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        var active = Settings.ActiveProfileId;

        foreach (var p in Profiles)
        {
            var item = new MenuItem
            {
                Header = p.Name,
                IsCheckable = true,
                IsChecked = p.Id == active,
                Tag = p.Id
            };
            item.Click += ProfileMenuItemClick;
            menu.Items.Add(item);
        }

        if (Profiles.Count > 0) menu.Items.Add(new Separator());

        var openItem = new MenuItem { Header = "Open Settings…" };
        openItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(openItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ProfileMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is string id)
            SwitchToProfile(id);
    }

    public void RefreshTrayMenu()
    {
        if (_trayIcon != null)
            _trayIcon.ContextMenu = BuildContextMenu();
    }

    public void UpdateTrayTooltip(string text)
    {
        if (_trayIcon != null)
            _trayIcon.ToolTipText = text;
    }

    // ---------- Switch ----------

    public void SwitchToProfile(string profileId)
    {
        var p = Profiles.FirstOrDefault(x => x.Id == profileId);
        if (p == null) return;

        EnvironmentSwitcher.Apply(p);
        ClaudeSettingsManager.Apply(p);

        Settings.ActiveProfileId = profileId;
        Store.SaveSettings(Settings);

        RefreshTrayMenu();
        UpdateTrayTooltip($"Active: {p.Name}");
        _mainWindow?.RefreshActiveProfile();

        // Always run prompt logic on the UI thread (hotkeys may fire from another thread).
        Dispatcher.Invoke(() => PostSwitchPrompt(p));
    }

    private void PostSwitchPrompt(Profile p)
    {
        if (!Settings.PromptRestartAfterSwitch)
        {
            ShowSwitchedToast(p);
            return;
        }

        var terminals = ProcessDetectionService.DetectTerminals();
        var vsCodes = ProcessDetectionService.DetectVsCode();

        if (terminals.Count == 0 && vsCodes.Count == 0)
        {
            ShowSwitchedToast(p);
            return;
        }

        var dlg = new RestartPromptWindow(p.Name, terminals, vsCodes)
        {
            Owner = (_mainWindow != null && _mainWindow.IsVisible) ? _mainWindow : null
        };
        dlg.ShowDialog();

        if (dlg.DontAskAgain)
        {
            Settings.PromptRestartAfterSwitch = false;
            Store.SaveSettings(Settings);
            _mainWindow?.RefreshActiveProfile();
        }

        if (dlg.RestartConfirmed)
        {
            if (dlg.TerminalsToKill.Count > 0)
                ProcessRestarter.KillAndRelaunchTerminals(dlg.TerminalsToKill);
            if (dlg.VsCodeToKill.Count > 0)
                ProcessRestarter.KillAndRelaunchVsCode(dlg.VsCodeToKill);
        }

        ShowSwitchedToast(p);
    }

    private void ShowSwitchedToast(Profile p)
    {
        _trayIcon?.ShowBalloonTip(
            $"Switched to {p.Name}",
            "Claude Code: chỉ cần /exit rồi gõ claude lại. Tool khác đọc env: restart terminal.",
            BalloonIcon.Info);
    }

    // ---------- Hotkeys ----------

    public void RegisterAllHotkeys()
    {
        HotkeyService.UnregisterAll();
        _registeredHotkeys.Clear();

        foreach (var (profileId, binding) in Settings.HotkeyBindings)
        {
            if (string.IsNullOrWhiteSpace(binding)) continue;
            var (mods, vk) = HotkeyService.Parse(binding);
            if (vk == 0) continue;

            var id = HotkeyService.Register(mods, vk, () => SwitchToProfile(profileId));
            if (id >= 0) _registeredHotkeys[profileId] = id;
        }
    }

    // ---------- Windows ----------

    public void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void ExitApp()
    {
        HotkeyService.Dispose();
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        Shutdown();
    }
}
