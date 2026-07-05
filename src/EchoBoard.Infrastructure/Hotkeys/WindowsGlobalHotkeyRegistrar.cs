using System.ComponentModel;
using System.Runtime.InteropServices;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Enums;

namespace EchoBoard.Infrastructure.Hotkeys;

public sealed class WindowsGlobalHotkeyRegistrar : IGlobalHotkeyRegistrar, IAsyncDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private const int ErrorHotkeyAlreadyRegistered = 1409;
    private static readonly UIntPtr SubclassId = new(1);

    private readonly object syncRoot = new();
    private readonly Dictionary<Guid, int> registrationIdByBindingId = [];
    private readonly Dictionary<int, Guid> bindingIdByRegistrationId = [];
    private readonly SubclassProc subclassProc;
    private IntPtr windowHandle;
    private int nextRegistrationId = 1000;
    private bool subclassed;

    public WindowsGlobalHotkeyRegistrar()
    {
        subclassProc = WindowSubclassProc;
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public void Initialize(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle is required.", nameof(hwnd));
        }

        lock (syncRoot)
        {
            if (windowHandle == hwnd && subclassed)
            {
                return;
            }

            windowHandle = hwnd;
            if (!SetWindowSubclass(windowHandle, subclassProc, SubclassId, UIntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not attach the global hotkey window subclass.");
            }

            subclassed = true;
        }
    }

    public Task<HotkeyRegistrationResult> RegisterAsync(HotkeyRegistrationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult(new HotkeyRegistrationResult(HotkeyRegistrationState.Unavailable, "Main window is not ready for global hotkeys."));
        }

        var virtualKey = VirtualKeyCode(request.PrimaryKey);
        if (virtualKey == 0)
        {
            return Task.FromResult(new HotkeyRegistrationResult(HotkeyRegistrationState.Invalid, "Primary key is not supported by Windows registration."));
        }

        lock (syncRoot)
        {
            UnregisterNoLock(request.BindingId);
            var registrationId = nextRegistrationId++;
            var modifiers = ModifierFlags(request.Modifiers) | ModNoRepeat;
            if (!RegisterHotKey(windowHandle, registrationId, modifiers, virtualKey))
            {
                var errorCode = Marshal.GetLastWin32Error();
                var state = errorCode == ErrorHotkeyAlreadyRegistered
                    ? HotkeyRegistrationState.Conflicting
                    : HotkeyRegistrationState.Unavailable;
                var message = state == HotkeyRegistrationState.Conflicting
                    ? $"{request.NormalizedKeyCombination} is already registered by Windows or another app."
                    : $"Windows could not register {request.NormalizedKeyCombination}: {new Win32Exception(errorCode).Message}";

                return Task.FromResult(new HotkeyRegistrationResult(state, message));
            }

            registrationIdByBindingId[request.BindingId] = registrationId;
            bindingIdByRegistrationId[registrationId] = request.BindingId;
            return Task.FromResult(HotkeyRegistrationResult.Active($"{request.NormalizedKeyCombination} registered."));
        }
    }

    public Task UnregisterAsync(Guid bindingId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (syncRoot)
        {
            UnregisterNoLock(bindingId);
        }

        return Task.CompletedTask;
    }

    public Task UnregisterAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (syncRoot)
        {
            foreach (var registrationId in registrationIdByBindingId.Values)
            {
                _ = UnregisterHotKey(windowHandle, registrationId);
            }

            registrationIdByBindingId.Clear();
            bindingIdByRegistrationId.Clear();
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await UnregisterAllAsync(CancellationToken.None);
        lock (syncRoot)
        {
            if (subclassed && windowHandle != IntPtr.Zero)
            {
                _ = RemoveWindowSubclass(windowHandle, subclassProc, SubclassId);
                subclassed = false;
            }
        }
    }

    private IntPtr WindowSubclassProc(IntPtr hwnd, uint message, UIntPtr wParam, IntPtr lParam, UIntPtr subclassId, UIntPtr referenceData)
    {
        if (message == WmHotkey)
        {
            var registrationId = unchecked((int)wParam);
            Guid? bindingId = null;
            lock (syncRoot)
            {
                if (bindingIdByRegistrationId.TryGetValue(registrationId, out var mappedBindingId))
                {
                    bindingId = mappedBindingId;
                }
            }

            if (bindingId is not null)
            {
                HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(bindingId.Value));
                return IntPtr.Zero;
            }
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void UnregisterNoLock(Guid bindingId)
    {
        if (!registrationIdByBindingId.Remove(bindingId, out var registrationId))
        {
            return;
        }

        bindingIdByRegistrationId.Remove(registrationId);
        _ = UnregisterHotKey(windowHandle, registrationId);
    }

    private static uint ModifierFlags(HotkeyModifiers modifiers)
    {
        uint flags = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            flags |= ModControl;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            flags |= ModAlt;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            flags |= ModShift;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            flags |= ModWin;
        }

        return flags;
    }

    private static uint VirtualKeyCode(string primaryKey)
    {
        if (primaryKey.Length == 1)
        {
            var character = primaryKey[0];
            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                return character;
            }
        }

        if (primaryKey.StartsWith('F') &&
            int.TryParse(primaryKey[1..], System.Globalization.CultureInfo.InvariantCulture, out var functionKey) &&
            functionKey is >= 1 and <= 24)
        {
            return (uint)(0x70 + functionKey - 1);
        }

        return primaryKey switch
        {
            "Insert" => 0x2D,
            "Delete" => 0x2E,
            "Home" => 0x24,
            "End" => 0x23,
            "PageUp" => 0x21,
            "PageDown" => 0x22,
            "ArrowUp" => 0x26,
            "ArrowDown" => 0x28,
            "ArrowLeft" => 0x25,
            "ArrowRight" => 0x27,
            _ => 0
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("comctl32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, UIntPtr wParam, IntPtr lParam);

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint message, UIntPtr wParam, IntPtr lParam, UIntPtr subclassId, UIntPtr referenceData);
}
