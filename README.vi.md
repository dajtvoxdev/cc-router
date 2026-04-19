# CCRouter

> Tray app Windows nhỏ gọn để chuyển nhanh **base URL của Claude Code** giữa nhiều backend tương thích Anthropic (Z.AI, Kimi, DeepSeek, local proxy…) và quay về subscription mặc định của Claude.

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/License-MIT-green)

## Tại sao cần app này?

Claude Code hỗ trợ trỏ sang các backend OpenAI/Anthropic-compatible bằng env vars (`ANTHROPIC_BASE_URL`, `ANTHROPIC_AUTH_TOKEN`…) và file `~/.claude/settings.json`. Nhưng:

- Đổi bằng PowerShell (`setx` / `[Environment]::SetEnvironmentVariable`) rất rườm rà.
- Phải nhớ key của từng backend, dán đi dán lại dễ sai.
- Quay về subscription mặc định = phải xóa từng env var, dễ sót.
- Token API lưu plain text trong shell history rất rủi ro.

App này giải quyết tất cả: lưu profile, switch 1 click ở taskbar, mã hóa token, có hotkey và tự động restart terminal/VSCode.

## Tính năng

- 🗂 **Quản lý nhiều profile**: Z.AI, Kimi, DeepSeek, local proxy, default Claude — không giới hạn số lượng.
- 🎯 **Switch 1 click** từ system tray (right-click icon).
- ⌨️ **Global hotkey** (vd `Ctrl+Alt+1` cho Z.AI, `Ctrl+Alt+0` cho Default).
- 🔐 **Token mã hóa DPAPI** (CurrentUser scope) — file `profiles.json` copy sang máy khác cũng không đọc được.
- 🔄 **Mirror sang `~/.claude/settings.json`** — Claude Code chỉ cần `/exit` rồi `claude` lại, không cần restart terminal.
- 🚀 **Auto restart** terminal/VSCode đang mở (có dialog confirm + cảnh báo unsaved changes của VSCode).
- 🪟 **Chạy nền ở taskbar**, autostart cùng Windows tùy chọn.
- 🧪 **Test connection** từng profile (gọi `/v1/models` với token).
- 📦 **Single instance** (Mutex), close-to-tray.

## Cài đặt

### Cách 1: Tải bản release (khuyến nghị)

1. Vào [Releases](../../releases) và tải `CCRouter-win-x64.zip` mới nhất.
2. Giải nén ra folder bất kỳ (vd `C:\Tools\CCRouter\`).
3. Chạy `CCRouter.exe` — icon tray xuất hiện ở góc phải taskbar.
4. (Tùy chọn) Right-click tray → **Open Settings** → tick **Start with Windows**.

> **Lưu ý SmartScreen**: Lần đầu chạy Windows có thể cảnh báo "Windows protected your PC". Click **More info** → **Run anyway**. Bản release chưa code-sign.

### Cách 2: Build từ source

Yêu cầu: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trên Windows.

```bash
git clone https://github.com/<your-username>/CCRouter.git
cd CCRouter
dotnet run
```

Build release self-contained (không cần .NET runtime trên máy đích):

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\CCRouter.exe` (~150 MB).

Build framework-dependent (nhỏ hơn ~5 MB, máy đích cần [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)):

```bash
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

## Hướng dẫn sử dụng

### Thêm profile Z.AI

1. Right-click icon tray → **Open Settings**.
2. Click **+ New** → điền:
   - **Name**: `Z.AI`
   - **Base URL**: `https://api.z.ai/api/anthropic`
   - **Auth Token**: API key lấy từ <https://z.ai/manage-apikey/apikey-list>
   - **API_TIMEOUT_MS**: `3000000` (Z.AI khuyến nghị)
   - **Opus model**: `GLM-4.7`
   - **Sonnet model**: `GLM-4.7`
   - **Haiku model**: `GLM-4.5-Air`
   - **Hotkey** (tùy chọn): `CTRL+ALT+1`
3. Click **Save**.

### Thêm profile Default Claude

1. Click **+ New** → điền `Name: Default Claude`.
2. Tick **This is the Default Claude subscription** (sẽ ẩn các field API).
3. (Tùy chọn) Hotkey `CTRL+ALT+0`.
4. Click **Save**.

### Switch profile

3 cách:

1. **Tray menu**: Right-click icon → chọn profile.
2. **Hotkey**: Nhấn tổ hợp đã gán ở bất kỳ app nào.
3. **Settings window**: Chọn profile trong list → click **⚡ Set Active**.

Sau khi switch, dialog *"Restart applications?"* sẽ hiện (nếu detect được terminal/VSCode đang mở):

- **Terminals**: default tick — kill rồi mở lại (Windows Terminal / PowerShell).
- **VSCode**: default *uncheck* (vì kill = mất unsaved changes). Tick nếu muốn.
- **Don't ask me again**: chỉ hiện toast lần sau (bật lại trong Settings).

> **Mẹo**: Nếu bạn chỉ dùng Claude Code, **không cần restart gì cả** — Claude Code đọc `~/.claude/settings.json` mỗi session. Chỉ cần `/exit` rồi gõ `claude` lại.

## Cấu hình lưu ở đâu

| Nội dung | Đường dẫn |
|---|---|
| Profile + token (DPAPI encrypted) | `%APPDATA%\CCRouter\profiles.json` |
| App settings (active, hotkeys, autostart…) | `%APPDATA%\CCRouter\settings.json` |
| Mirror cho Claude Code đọc | `%USERPROFILE%\.claude\settings.json` (`env` block) |
| Backup `.claude/settings.json` | `%USERPROFILE%\.claude\settings.json.bak` |

App **không bao giờ** ghi token plain text. DPAPI scope `CurrentUser` đảm bảo file copy sang user/máy khác không decrypt được.

## Env vars được quản lý

App set/clear các biến sau ở User scope khi switch:

| Variable | Vai trò |
|---|---|
| `ANTHROPIC_BASE_URL` | Endpoint backend |
| `ANTHROPIC_AUTH_TOKEN` | API key (decrypt khi apply) |
| `API_TIMEOUT_MS` | Timeout request (ms) |
| `ANTHROPIC_DEFAULT_OPUS_MODEL` | Map model Opus |
| `ANTHROPIC_DEFAULT_SONNET_MODEL` | Map model Sonnet |
| `ANTHROPIC_DEFAULT_HAIKU_MODEL` | Map model Haiku |

Profile **Default Claude** = clear hết 6 biến này.

## Troubleshooting

**Q: Sau khi switch, terminal mới vẫn chưa thấy biến mới?**
A: Kiểm tra terminal có inherit từ `explorer.exe` không (PowerShell mở qua `Win+R` mới đảm bảo). VS Code integrated terminal cần restart toàn bộ Code.exe (Reload Window không đủ).

**Q: Hotkey không hoạt động?**
A: Có thể tổ hợp đó đã bị app khác giữ. Đổi sang tổ hợp khác (`CTRL+ALT+SHIFT+...`).

**Q: SmartScreen chặn?**
A: Click "More info" → "Run anyway". Bản release chưa code-sign.

**Q: App đã chạy nhưng không thấy icon tray?**
A: Click mũi tên hiển thị icon ẩn ở taskbar, kéo icon `CCRouter` ra ngoài cho hiện cố định.

**Q: Lỡ làm hỏng `~/.claude/settings.json`?**
A: Restore từ `~/.claude/settings.json.bak` (app tự backup mỗi lần ghi).

**Q: Muốn xóa hoàn toàn cấu hình của app?**
A: Exit app từ tray → xóa folder `%APPDATA%\CCRouter\`.

## Tech stack

- **.NET 8** + **WPF** (`net8.0-windows`)
- **Hardcodet.NotifyIcon.Wpf** — system tray icon
- **System.Security.Cryptography.ProtectedData** — DPAPI encryption
- **System.Text.Json.Nodes** — merge `~/.claude/settings.json` không đụng key khác
- **P/Invoke** `RegisterHotKey` cho global hotkey

## Cấu trúc project

```
CCRouter/
├── Models/
│   ├── Profile.cs           # Profile data model
│   └── AppSettings.cs       # App settings (active, hotkeys, autostart…)
├── Services/
│   ├── ProfileStore.cs      # JSON I/O cho profiles + settings
│   ├── SecretProtector.cs   # DPAPI wrap
│   ├── EnvironmentSwitcher.cs       # Set/clear User env vars
│   ├── ClaudeSettingsManager.cs     # Mirror sang ~/.claude/settings.json
│   ├── AutostartService.cs  # HKCU\...\Run entry
│   ├── HotkeyService.cs     # P/Invoke RegisterHotKey
│   ├── ProcessDetectionService.cs   # Quét terminal/VSCode đang mở
│   └── ProcessRestarter.cs  # Kill + relaunch
├── App.xaml(.cs)            # Mutex + tray + switch flow
├── MainWindow.xaml(.cs)     # UI quản lý profile
├── RestartPromptWindow.xaml(.cs)    # Dialog hỏi restart
└── Resources/app.ico
```

## Roadmap

- [ ] Import/export profile (cần xử lý DPAPI key giữa máy).
- [ ] Cleanup mở rộng giống script `chiasegpu` (xóa `statusLine`, `disableLoginPrompt`, `statusline.ps1`).
- [ ] Restore VSCode workspace cụ thể khi kill+relaunch.
- [ ] Portable mode (config cạnh exe thay vì `%APPDATA%`).
- [ ] Theme dark, localization English.

## Đóng góp

PR và issue đều welcome. Trước khi PR, vui lòng:

1. `dotnet build` không có warning.
2. Test thủ công flow switch + restart prompt.
3. Tránh thêm dependency NuGet không cần thiết.

## License

[MIT](LICENSE) © 2026 CCRouter contributors

---

> ⚠ **Disclaimer**: App này không liên kết với Anthropic, Z.AI hoặc bất kỳ provider nào. Tự chịu trách nhiệm về việc sử dụng API key và tuân thủ ToS của các backend.
