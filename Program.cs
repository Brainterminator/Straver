using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardHook
{
    class Program
    {
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static Keys currentKey = Keys.None;
        private static Keys haltedKey = Keys.None;
        private static bool inSimulation = false;

        private static NotifyIcon trayIcon;
        private static ContextMenuStrip trayMenu;

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Activate Hook", null, OnActivateHook);
            trayMenu.Items.Add("Deactivate Hook", null, OnDeactivateHook);
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Keyboard Hook";
            trayIcon.Icon = new Icon("Resources/straver.ico");

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            Application.Run();
        }

        private static void OnActivateHook(object sender, EventArgs e)
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
                MessageBox.Show("Keyboard hook activated.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Keyboard hook is already activated.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void OnDeactivateHook(object sender, EventArgs e)
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                MessageBox.Show("Keyboard hook deactivated.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Keyboard hook is not active.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
            trayIcon.Visible = false;
            Application.Exit();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !inSimulation)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                if (key == Keys.A || key == Keys.D)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        if (currentKey != key)
                        {
                            if (currentKey != Keys.None)
                            {
                                haltedKey = currentKey;
                                SimulateKeyUp(currentKey);
                            }
                            currentKey = key;
                            SimulateKeyDown(currentKey);
                        }
                    }
                    else if (wParam == (IntPtr)WM_KEYUP)
                    {
                        if (key == currentKey)
                        {
                            SimulateKeyUp(currentKey);
                            if (haltedKey != key && haltedKey != Keys.None)
                            {
                                currentKey = haltedKey;
                                SimulateKeyDown(currentKey);
                            }
                            else
                            {
                                currentKey = Keys.None;
                            }
                        }
                        if (key == haltedKey && haltedKey != Keys.None)
                        {
                            haltedKey = Keys.None;
                        }
                    }
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        private static void SimulateKeyDown(Keys key)
        {
            inSimulation = true;
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            inSimulation = false;
        }

        private static void SimulateKeyUp(Keys key)
        {
            inSimulation = true;
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            inSimulation = false;
        }
    }
}
