# CCRouter

> Lightweight Windows tray app to quickly switch **Claude Code's base URL** between multiple Anthropic-compatible backends (Z.AI, Kimi, DeepSeek, local proxy…) and back to the default Claude subscription.

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/License-MIT-green)

🇻🇳 [Tiếng Việt](README.vi.md)

## Why this app?

Claude Code can be pointed at any OpenAI/Anthropic-compatible backend via env vars (`ANTHROPIC_BASE_URL`, `ANTHROPIC_AUTH_TOKEN`, …) and `~/.claude/settings.json`. But:

- Switching via PowerShell (`setx` / `[Environment]::SetEnvironmentVariable`) is tedious.
- You have to remember each backend's API key and paste it repeatedly.
- Going back to the default subscription means clearing each env var manually — easy to miss one.
- API tokens stored as plain text in shell history are a security risk.

CCRouter solves all of this: stores profiles, switches with one click from the tray, encrypts tokens with DPAPI, supports global hotkeys, and can auto-restart your terminal/VSCode after a switch.

## Features

- 🗂 **Multi-profile management** — Z.AI, Kimi, DeepSeek, local proxy, default Claude. Unlimited entries.
- 🎯 **One-click switching** from the system tray (right-click the icon).
- ⌨️ **Global hotkeys** (e.g. `Ctrl+Alt+1` for Z.AI, `Ctrl+Alt+0` for Default).
- 🔐 **DPAPI-encrypted tokens** (CurrentUser scope) — `profiles.json` is unreadable if copied to another machine.
- 🔄 **Mirrors to `~/.claude/settings.json`** — Claude Code only needs `/exit` then `claude` again, no terminal restart required.
- 🚀 **Auto-restart** open terminals / VSCode (with confirm dialog and an explicit warning about VSCode unsaved changes).
- 🪟 **Runs in the background tray**, optional auto-start with Windows.
- 🧪 **Test connection** per profile (calls `/v1/models` with the configured token).
- 📦 **Single instance** (Mutex), close-to-tray.

## Install

### Option 1 — Download a release (recommended)

1. Go to [Releases](../../releases) and download the latest `CCRouter.exe`.
   - **`CCRouter.exe`** — self-contained (~68 MB), no .NET runtime needed.
   - **`CCRouter-fxdep.exe`** — small (~1 MB), requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Move the .exe somewhere persistent (e.g. `C:\Tools\CCRouter\CCRouter.exe`).
3. Run it — the tray icon appears in the bottom-right of the taskbar.
4. (Optional) Right-click tray → **Open Settings** → tick **Start with Windows**.

> **SmartScreen note**: On first launch Windows may show "Windows protected your PC". Click **More info** → **Run anyway**. The release builds are not yet code-signed.

### Option 2 — Build from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```bash
git clone https://github.com/dajtvoxdev/cc-router.git
cd cc-router
dotnet run
```

Self-contained release build (no .NET runtime needed on the target machine):

```bash
dotnet publish -c Release -r win-x64 -p:SelfContained=true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true
```

Framework-dependent build (much smaller, target machine needs the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)):

```bash
dotnet publish -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true
```

Or just run `./build-release.ps1` to produce both flavors at once.

## Usage

### Add a Z.AI profile

1. Right-click the tray icon → **Open Settings**.
2. Click **+ New** and fill in:
   - **Name**: `Z.AI`
   - **Base URL**: `https://api.z.ai/api/anthropic`
   - **Auth Token**: API key from <https://z.ai/manage-apikey/apikey-list>
   - **API_TIMEOUT_MS**: `3000000` (recommended by Z.AI)
   - **Opus model**: `GLM-4.7`
   - **Sonnet model**: `GLM-4.7`
   - **Haiku model**: `GLM-4.5-Air`
   - **Hotkey** (optional): `CTRL+ALT+1`
3. Click **Save**.

### Add the Default Claude profile

1. Click **+ New** and set `Name: Default Claude`.
2. Tick **This is the Default Claude subscription** (this hides the API fields).
3. (Optional) Hotkey `CTRL+ALT+0`.
4. Click **Save**.

### Switch profiles

Three ways:

1. **Tray menu** — right-click the icon → pick a profile.
2. **Hotkey** — press the assigned shortcut from any application.
3. **Settings window** — pick a profile in the list → click **⚡ Set Active**.

After switching, a *"Restart applications?"* dialog appears (if open terminals/VSCode are detected):

- **Terminals** — checked by default; CCRouter kills and relaunches them (Windows Terminal / PowerShell).
- **VSCode** — *unchecked* by default (killing it loses unsaved changes). Tick to enable.
- **Don't ask me again** — only show a toast next time. Re-enable from Settings later.

> **Tip**: If you only use Claude Code, you don't actually need to restart anything — Claude Code reads `~/.claude/settings.json` every session. Just `/exit` then run `claude` again.

## Where settings are stored

| Content | Path |
|---|---|
| Profiles + tokens (DPAPI-encrypted) | `%APPDATA%\CCRouter\profiles.json` |
| App settings (active profile, hotkeys, autostart…) | `%APPDATA%\CCRouter\settings.json` |
| Mirror that Claude Code reads | `%USERPROFILE%\.claude\settings.json` (`env` block) |
| Backup of `.claude/settings.json` | `%USERPROFILE%\.claude\settings.json.bak` |

CCRouter **never** writes tokens in plaintext. DPAPI's `CurrentUser` scope ensures the file cannot be decrypted on another user account or machine.

## Managed environment variables

When you switch profiles, CCRouter sets/clears these User-scope variables:

| Variable | Purpose |
|---|---|
| `ANTHROPIC_BASE_URL` | Backend endpoint |
| `ANTHROPIC_AUTH_TOKEN` | API key (decrypted at apply time) |
| `API_TIMEOUT_MS` | Request timeout (ms) |
| `ANTHROPIC_DEFAULT_OPUS_MODEL` | Opus model mapping |
| `ANTHROPIC_DEFAULT_SONNET_MODEL` | Sonnet model mapping |
| `ANTHROPIC_DEFAULT_HAIKU_MODEL` | Haiku model mapping |

The **Default Claude** profile clears all six.

## Troubleshooting

**Q: A new terminal still doesn't see the new env vars after switching.**
A: The terminal must be a freshly launched process inheriting from `explorer.exe` (e.g. PowerShell opened via `Win+R`). VSCode's integrated terminal needs the entire `Code.exe` restarted — Reload Window is not enough.

**Q: A hotkey doesn't work.**
A: It's likely already taken by another app. Pick something more unusual like `CTRL+ALT+SHIFT+...`.

**Q: SmartScreen blocks the .exe.**
A: Click "More info" → "Run anyway". The release builds are not yet code-signed.

**Q: The app is running but I can't see the tray icon.**
A: Click the overflow arrow on the taskbar, then drag the `CCRouter` icon out so it stays visible.

**Q: I broke `~/.claude/settings.json`.**
A: Restore from `~/.claude/settings.json.bak` (CCRouter writes a backup on every save).

**Q: I want to wipe CCRouter's config completely.**
A: Exit the app from the tray → delete `%APPDATA%\CCRouter\`.

## Tech stack

- **.NET 8** + **WPF** (`net8.0-windows`)
- **Hardcodet.NotifyIcon.Wpf** — system tray icon
- **System.Security.Cryptography.ProtectedData** — DPAPI encryption
- **System.Text.Json.Nodes** — merge `~/.claude/settings.json` without touching unrelated keys
- **P/Invoke** `RegisterHotKey` for global hotkeys

## Project layout

```
CCRouter/
├── Models/
│   ├── Profile.cs           # Profile data model
│   └── AppSettings.cs       # App settings (active, hotkeys, autostart…)
├── Services/
│   ├── ProfileStore.cs              # JSON I/O for profiles + settings
│   ├── SecretProtector.cs           # DPAPI wrapper
│   ├── EnvironmentSwitcher.cs       # Set/clear User env vars
│   ├── ClaudeSettingsManager.cs     # Mirror to ~/.claude/settings.json
│   ├── AutostartService.cs          # HKCU\...\Run entry
│   ├── HotkeyService.cs             # P/Invoke RegisterHotKey
│   ├── ProcessDetectionService.cs   # Detect open terminals/VSCode
│   └── ProcessRestarter.cs          # Kill + relaunch
├── App.xaml(.cs)            # Mutex + tray + switch flow
├── MainWindow.xaml(.cs)     # Profile management UI
├── RestartPromptWindow.xaml(.cs)    # Restart-confirmation dialog
└── Resources/app.ico
```

## Roadmap

- [ ] Profile import/export (needs to handle DPAPI keys across machines).
- [ ] Extended cleanup similar to the `chiasegpu` script (remove `statusLine`, `disableLoginPrompt`, `statusline.ps1`).
- [ ] Restore the specific VSCode workspace when killing + relaunching.
- [ ] Portable mode (config next to the exe instead of `%APPDATA%`).
- [ ] Dark theme.

## Contributing

PRs and issues are welcome. Before opening a PR:

1. `dotnet build` must produce no warnings.
2. Manually test the switch + restart-prompt flow.
3. Avoid adding unnecessary NuGet dependencies.

## License

[MIT](LICENSE) © 2026 CCRouter contributors

---

> ⚠ **Disclaimer**: This app is not affiliated with Anthropic, Z.AI, or any other provider. You are responsible for your own API key usage and for complying with each backend's Terms of Service.
