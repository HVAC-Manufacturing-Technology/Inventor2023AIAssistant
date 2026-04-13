using System;
using System.Runtime.InteropServices;

namespace Inventor2023AIAssistant
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;
        private const int VK_SHIFT = 0x10;

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(
            IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr GetModuleHandle(
            string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(
            uint idAttach,
            uint idAttachTo,
            bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private IntPtr _panelHandle = IntPtr.Zero;

        public event Action OnSpacePressed;
        public event Action OnEnterPressed;
        public event Action OnShiftEnterPressed;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void SetPanelHandle(IntPtr handle)
        {
            _panelHandle = handle;
        }

        public void Install()
        {
            if (_hookID != IntPtr.Zero)
                return;

            using (var curProcess =
                System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        public void Uninstall()
        {
            if (_hookID == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        // ─── Focus check ──────────────────────────────────────────────

        private bool IsAssistantPanelFocused()
        {
            try
            {
                if (_panelHandle == IntPtr.Zero)
                    return false;

                IntPtr foregroundWindow =
                    GetForegroundWindow();

                if (foregroundWindow == IntPtr.Zero)
                    return false;

                uint foregroundThreadId =
                    GetWindowThreadProcessId(
                        foregroundWindow, out _);

                uint currentThreadId = GetCurrentThreadId();

                bool attached = false;

                if (foregroundThreadId != currentThreadId)
                {
                    attached = AttachThreadInput(
                        currentThreadId,
                        foregroundThreadId,
                        true);
                }

                IntPtr focusedWindow = GetFocus();

                if (attached)
                {
                    AttachThreadInput(
                        currentThreadId,
                        foregroundThreadId,
                        false);
                }

                if (focusedWindow == IntPtr.Zero)
                    return false;

                return IsChildOrSelf(
                    _panelHandle, focusedWindow);
            }
            catch
            {
                return false;
            }
        }

        private bool IsChildOrSelf(
            IntPtr parent, IntPtr candidate)
        {
            if (parent == IntPtr.Zero ||
                candidate == IntPtr.Zero)
                return false;

            if (parent == candidate)
                return true;

            IntPtr current = candidate;

            for (int i = 0; i < 20; i++)
            {
                current = GetParent(current);

                if (current == IntPtr.Zero)
                    break;

                if (current == parent)
                    return true;
            }

            return false;
        }

        // ─── Hook callback ────────────────────────────────────────────

        private IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 &&
                (wParam == (IntPtr)WM_KEYDOWN ||
                 wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (IsAssistantPanelFocused())
                {
                    if (vkCode == VK_SPACE)
                    {
                        OnSpacePressed?.Invoke();
                        return (IntPtr)1;
                    }

                    if (vkCode == VK_RETURN)
                    {
                        bool shiftDown =
                            (GetKeyState(VK_SHIFT) & 0x8000) != 0;

                        if (shiftDown)
                            OnShiftEnterPressed?.Invoke();
                        else
                            OnEnterPressed?.Invoke();

                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(
                _hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
        }
    }
}