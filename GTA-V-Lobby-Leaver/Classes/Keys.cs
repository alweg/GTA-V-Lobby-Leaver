using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace GTA_V_Lobby_Leaver
{
    public class Keys
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookHandler lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public enum VKeys { KEY_F6 = 0x75 }

        public delegate void KeyboardHookCallback(VKeys key);
        private delegate IntPtr KeyboardHookHandler(int nCode, IntPtr wParam, IntPtr lParam);

        private KeyboardHookHandler hookHandler;
        private IntPtr hookID = IntPtr.Zero;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYUP = 0x105;

        public event KeyboardHookCallback KeyDown;
        public event KeyboardHookCallback KeyUp;

        public void Install()
        {
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);
        }
        public void Uninstall() { UnhookWindowsHookEx(hookID); }

        private IntPtr SetHook(KeyboardHookHandler proc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                return SetWindowsHookEx(13, proc, GetModuleHandle(module.ModuleName), 0);
            }
        }
        private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int iwParam = wParam.ToInt32();

                if (iwParam == WM_KEYDOWN || iwParam == WM_SYSKEYDOWN)
                {
                    KeyDown?.Invoke((VKeys)Marshal.ReadInt32(lParam));
                }
                if (iwParam == WM_KEYUP || iwParam == WM_SYSKEYUP)
                {
                    KeyUp?.Invoke((VKeys)Marshal.ReadInt32(lParam));
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        ~Keys()
        {
            Uninstall();
        }
    }
}