using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using GlobalHotKey;

namespace Hotkeys
{
    public partial class MainForm : Form
    {
        private IntPtr _GUID_VIDEO_SUBGROUP_ptr;
        private IntPtr _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr;

        [DllImport("PowrProf.dll")]
        private static extern int PowerGetActiveScheme(IntPtr UserRootPowerKey, IntPtr ActivePolicyGuid);

        [DllImport("PowrProf.dll")]
        private static extern int PowerReadACValueIndex(IntPtr RootPowerKey, IntPtr SchemeGuid,
            IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr AcValueIndex);

        [DllImport("PowrProf.dll")]
        private static extern int PowerWriteACValueIndex(IntPtr RootPowerKey, IntPtr SchemeGuid,
            IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, int AcValueIndex);

        [DllImport("PowrProf.dll")]
        private static extern void
            PowerApplySettingChanges(IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Hide();

            var hotkeyManager = new HotKeyManager();
            hotkeyManager.Register(Key.F5, System.Windows.Input.ModifierKeys.Shift);
            hotkeyManager.Register(Key.F6, System.Windows.Input.ModifierKeys.Shift);
            hotkeyManager.Register(Key.F7, System.Windows.Input.ModifierKeys.Shift);

            hotkeyManager.KeyPressed += OnKeyPressed;

            var GUID_VIDEO_SUBGROUP = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
            var GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS = new Guid("aded5e82-b909-4619-9949-f5d71dac0bcb");

            _GUID_VIDEO_SUBGROUP_ptr = Marshal.AllocHGlobal(16);
            _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr = Marshal.AllocHGlobal(16);

            WriteBytes(_GUID_VIDEO_SUBGROUP_ptr, GUID_VIDEO_SUBGROUP.ToByteArray());
            WriteBytes(_GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr,
                GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS.ToByteArray());
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            var ptrToActiveSchemePtr = Marshal.AllocHGlobal(4);

            PowerGetActiveScheme(IntPtr.Zero, ptrToActiveSchemePtr);

            var activeSchemePtr = Marshal.ReadIntPtr(ptrToActiveSchemePtr);
            var currentBrightnessPtr = Marshal.AllocHGlobal(4);

            PowerReadACValueIndex(IntPtr.Zero, activeSchemePtr, _GUID_VIDEO_SUBGROUP_ptr,
                _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr, currentBrightnessPtr);

            var currentBrightness = Marshal.ReadInt32(currentBrightnessPtr);
            Text = currentBrightness.ToString();

            switch (e.HotKey.Key)
            {
                case Key.F5:
                    PowerWriteACValueIndex(IntPtr.Zero, activeSchemePtr, _GUID_VIDEO_SUBGROUP_ptr,
                        _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr, currentBrightness - 2);
                    break;
                case Key.F6:
                    PowerWriteACValueIndex(IntPtr.Zero, activeSchemePtr, _GUID_VIDEO_SUBGROUP_ptr,
                        _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr, currentBrightness + 2);
                    break;
                case Key.F7:
                    PowerWriteACValueIndex(IntPtr.Zero, activeSchemePtr, _GUID_VIDEO_SUBGROUP_ptr,
                        _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr, 0);
                    Process.Start(@"C:\Program Files (x86)\SleepTimer Ultimate\SleepTimerUltimate.exe");
                    break;
            }

            PowerApplySettingChanges(_GUID_VIDEO_SUBGROUP_ptr, _GUID_DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS_ptr);

            LocalFree(activeSchemePtr);
            Marshal.FreeHGlobal(currentBrightnessPtr);
            Marshal.FreeHGlobal(ptrToActiveSchemePtr);
        }

        private byte[] ReadBytes(IntPtr ptr, int size)
        {
            var bytes = new byte[size];
            for (int i = 0; i < size; i++)
            {
                bytes[i] = Marshal.ReadByte(ptr, i);
            }

            return bytes;
        }

        private void WriteBytes(IntPtr ptr, byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(ptr, i, bytes[i]);
            }
        }
    }
}