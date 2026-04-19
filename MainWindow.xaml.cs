using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CCRouter.Models;
using CCRouter.Services;

namespace CCRouter;

public partial class MainWindow : Window
{
    private App App => (App)Application.Current;
    private Profile? _current;
    private bool _suppressEvents;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    // ---------- Refresh ----------

    public void Refresh()
    {
        _suppressEvents = true;

        ProfileList.ItemsSource = null;
        var vms = App.Profiles.Select(p => new ProfileVm(p, p.Id == App.Settings.ActiveProfileId)).ToList();
        ProfileList.ItemsSource = vms;

        ChkAutostart.IsChecked = App.Settings.AutostartEnabled;
        ChkStartMin.IsChecked = App.Settings.StartMinimized;
        ChkPromptRestart.IsChecked = App.Settings.PromptRestartAfterSwitch;
        TxtCurrentEnv.Text = EnvironmentSwitcher.ReadCurrentBaseUrl() ?? "(not set — default Claude)";
        TxtClaudeSettingsPath.Text = ClaudeSettingsManager.SettingsPath;

        _suppressEvents = false;

        if (_current != null)
        {
            var idx = App.Profiles.FindIndex(x => x.Id == _current.Id);
            if (idx >= 0) ProfileList.SelectedIndex = idx;
        }
    }

    public void RefreshActiveProfile() => Refresh();

    // ---------- Profile list ----------

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (ProfileList.SelectedItem is ProfileVm vm)
            LoadProfile(vm.Profile);
        else
            ClearForm();
    }

    private void LoadProfile(Profile p)
    {
        _current = p;
        _suppressEvents = true;

        FormTitle.Text = p.Name.Length > 0 ? p.Name : "New Profile";
        TxtName.Text = p.Name;
        ChkDefault.IsChecked = p.IsDefaultClaude;
        TxtBaseUrl.Text = p.BaseUrl ?? "";
        PwdToken.Password = string.IsNullOrWhiteSpace(p.AuthTokenEncrypted)
            ? ""
            : SecretProtector.Unprotect(p.AuthTokenEncrypted);
        TxtTimeout.Text = p.TimeoutMs ?? "";
        TxtOpus.Text = p.OpusModel ?? "";
        TxtSonnet.Text = p.SonnetModel ?? "";
        TxtHaiku.Text = p.HaikuModel ?? "";
        TxtNote.Text = p.Note ?? "";

        App.Settings.HotkeyBindings.TryGetValue(p.Id, out var hk);
        TxtHotkey.Text = hk ?? "";

        PanelApiFields.Visibility = p.IsDefaultClaude ? Visibility.Collapsed : Visibility.Visible;
        TxtStatus.Text = "";

        _suppressEvents = false;
    }

    private void ClearForm()
    {
        _current = null;
        FormTitle.Text = "Select or create a profile";
        TxtName.Text = ""; TxtBaseUrl.Text = ""; PwdToken.Password = "";
        TxtTimeout.Text = ""; TxtOpus.Text = ""; TxtSonnet.Text = ""; TxtHaiku.Text = "";
        TxtNote.Text = ""; TxtHotkey.Text = "";
        ChkDefault.IsChecked = false;
        PanelApiFields.Visibility = Visibility.Visible;
        TxtStatus.Text = "";
    }

    // ---------- Form actions ----------

    private void ChkDefault_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        PanelApiFields.Visibility = ChkDefault.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        ProfileList.SelectedItem = null;
        var p = new Profile { Name = "New Profile" };
        App.Profiles.Add(p);
        App.Store.SaveProfiles(App.Profiles);
        Refresh();
        // select new item
        var idx = App.Profiles.Count - 1;
        ProfileList.SelectedIndex = idx;
        TxtName.Focus();
        TxtName.SelectAll();
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (MessageBox.Show($"Delete profile \"{_current.Name}\"?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        App.Settings.HotkeyBindings.Remove(_current.Id);
        App.Profiles.Remove(_current);
        App.Store.SaveProfiles(App.Profiles);
        App.Store.SaveSettings(App.Settings);
        App.RegisterAllHotkeys();
        App.RefreshTrayMenu();
        ClearForm();
        Refresh();
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) { TxtStatus.Text = "No profile selected."; return; }
        if (string.IsNullOrWhiteSpace(TxtName.Text)) { TxtStatus.Text = "Name is required."; return; }

        _current.Name = TxtName.Text.Trim();
        _current.IsDefaultClaude = ChkDefault.IsChecked == true;
        _current.BaseUrl = _current.IsDefaultClaude ? null : NullIfEmpty(TxtBaseUrl.Text);
        _current.TimeoutMs = NullIfEmpty(TxtTimeout.Text);
        _current.OpusModel = NullIfEmpty(TxtOpus.Text);
        _current.SonnetModel = NullIfEmpty(TxtSonnet.Text);
        _current.HaikuModel = NullIfEmpty(TxtHaiku.Text);
        _current.Note = NullIfEmpty(TxtNote.Text);

        var pwd = PwdToken.Password;
        _current.AuthTokenEncrypted = string.IsNullOrWhiteSpace(pwd)
            ? null
            : SecretProtector.Protect(pwd);

        var hk = TxtHotkey.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(hk))
            App.Settings.HotkeyBindings.Remove(_current.Id);
        else
            App.Settings.HotkeyBindings[_current.Id] = hk;

        App.Store.SaveProfiles(App.Profiles);
        App.Store.SaveSettings(App.Settings);
        App.RegisterAllHotkeys();
        App.RefreshTrayMenu();

        TxtStatus.Text = $"✔ Saved \"{_current.Name}\"";
        FormTitle.Text = _current.Name;
        Refresh();
    }

    private void SetActive_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) { TxtStatus.Text = "No profile selected."; return; }
        App.SwitchToProfile(_current.Id);
        TxtCurrentEnv.Text = EnvironmentSwitcher.ReadCurrentBaseUrl() ?? "(not set — default Claude)";
        TxtStatus.Text = $"✔ Active: {_current.Name}";
        Refresh();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtBaseUrl.Text.Trim();
        var token = PwdToken.Password;
        if (string.IsNullOrWhiteSpace(url)) { TxtStatus.Text = "BaseUrl is empty."; return; }

        TxtStatus.Text = "Testing…";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("x-api-key", token);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            var resp = await http.GetAsync(url.TrimEnd('/') + "/v1/models");
            TxtStatus.Text = resp.IsSuccessStatusCode
                ? $"✔ Connected ({(int)resp.StatusCode})"
                : $"✘ HTTP {(int)resp.StatusCode}";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"✘ {ex.Message}";
        }
    }

    // ---------- App settings ----------

    private void Autostart_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        bool enable = ChkAutostart.IsChecked == true;
        App.Settings.AutostartEnabled = enable;
        App.Store.SaveSettings(App.Settings);
        if (enable) AutostartService.Enable(Environment.ProcessPath!);
        else AutostartService.Disable();
    }

    private void StartMin_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        App.Settings.StartMinimized = ChkStartMin.IsChecked == true;
        App.Store.SaveSettings(App.Settings);
    }

    private void PromptRestart_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        App.Settings.PromptRestartAfterSwitch = ChkPromptRestart.IsChecked == true;
        App.Store.SaveSettings(App.Settings);
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// Simple VM for list binding
internal class ProfileVm(Profile profile, bool isActive)
{
    public Profile Profile { get; } = profile;
    public string Name => Profile.Name;
    public Visibility IsActiveDisplay => isActive ? Visibility.Visible : Visibility.Collapsed;
}
