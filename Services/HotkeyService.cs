using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CCRouter.Services;

public class HotkeyService : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WmHotkey = 0x0312;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CTRL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    private readonly HwndSource _hwnd;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 9000;

    public HotkeyService()
    {
        var p = new HwndSourceParameters("HotkeyHost") { Width = 0, Height = 0, WindowStyle = 0 };
        _hwnd = new HwndSource(p);
        _hwnd.AddHook(WndProc);
    }

    public int Register(uint modifiers, uint vk, Action callback)
    {
        int id = _nextId++;
        if (!RegisterHotKey(_hwnd.Handle, id, modifiers, vk))
            return -1;
        _callbacks[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        if (_callbacks.Remove(id))
            UnregisterHotKey(_hwnd.Handle, id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys)
            UnregisterHotKey(_hwnd.Handle, id);
        _callbacks.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _callbacks.TryGetValue(wParam.ToInt32(), out var cb))
        {
            cb();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwnd.Dispose();
    }

    // Parse "CTRL+ALT+1" → (modifiers, vk)
    public static (uint mods, uint vk) Parse(string binding)
    {
        uint mods = 0;
        uint vk = 0;
        foreach (var part in binding.ToUpperInvariant().Split('+'))
        {
            switch (part.Trim())
            {
                case "CTRL": mods |= MOD_CTRL; break;
                case "ALT": mods |= MOD_ALT; break;
                case "SHIFT": mods |= MOD_SHIFT; break;
                case "WIN": mods |= MOD_WIN; break;
                default:
                    if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                        vk = (uint)char.ToUpper(part[0]);
                    break;
            }
        }
        return (mods, vk);
    }

    public static string Format(uint mods, uint vk)
    {
        var parts = new List<string>();
        if ((mods & MOD_CTRL) != 0) parts.Add("CTRL");
        if ((mods & MOD_ALT) != 0) parts.Add("ALT");
        if ((mods & MOD_SHIFT) != 0) parts.Add("SHIFT");
        if ((mods & MOD_WIN) != 0) parts.Add("WIN");
        if (vk > 0) parts.Add(((char)vk).ToString());
        return string.Join("+", parts);
    }
}
