using Microsoft.Win32;

namespace FFXIVKoreanPatch
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (RegistryKey keyboardLayoutKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout", true))
            {
                if (keyboardLayoutKey != null)
                {
                    keyboardLayoutKey.SetValue("Scancode Map", new byte[]
                    {
                        0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x00, 0x00,
                        0x72, 0x00, 0x38, 0xe0,
                        0x00, 0x00, 0x00, 0x00
                    });
                }
            }
        }
    }
}
