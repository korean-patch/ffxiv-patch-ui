using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;

namespace FFXIVKoreanPatch.Patcher
{
    // Worker process for FFXIVKoreanPatch that do stuff that may require administrator access.
    internal class Program
    {
        private static string targetDir = string.Empty;
        private static string distribDir = string.Empty;

        private static string[] fontPatchFiles = new string[]
        {
            "000000.win32.dat1",
            "000000.win32.index"
        };

        private static string[] fullPatchFiles = new string[]
        {
            "0a0000.win32.dat1",
            "0a0000.win32.index"
        };

        private static string[] restoreFiles = new string[]
        {
            "000000.win32.index",
            "0a0000.win32.index"
        };

        // args[0] -> operation mode.
        //         -> 0 = install korean chat registry.
        //         -> 1 = install full korean patch.
        //         -> 2 = install chat only korean patch.
        //         -> 3 = remove korean patch and restore original.
        // args[1] -> path to the FFXIV client.
        // args[2] -> path to the cached korean patch files.
        static void Main(string[] args)
        {
            // Check arguments.
            if (args.Length != 3) return;
            if (args[0] != "0" && !Directory.Exists(args[1])) return;
            if (args[0] != "0" && !Directory.Exists(args[2])) return;

            // Populate paths.
            targetDir = args[1];
            distribDir = args[2];

            switch (args[0])
            {
                case "0":
                    InstallRegistry();
                    break;
                case "1":
                    InstallFull();
                    break;
                case "2":
                    InstallChatOnly();
                    break;
                case "3":
                    Remove();
                    break;
            }

            Console.WriteLine("작업이 성공적으로 완료되었습니다!");
            Console.WriteLine("이 창은 5초 후 자동으로 닫힙니다.");
            Thread.Sleep(5000);
        }

        // This installs korean chat registry.
        static void InstallRegistry()
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

        // This installs full korean patch.
        static void InstallFull()
        {
            foreach (string fullPatchFile in fullPatchFiles)
            {
                File.Copy(Path.Combine(distribDir, fullPatchFile), Path.Combine(targetDir, "sqpack", "ffxiv", fullPatchFile), true);
            }

            InstallChatOnly();
        }

        // This installs font only.
        static void InstallChatOnly()
        {
            foreach (string fontPatchFile in fontPatchFiles)
            {
                File.Copy(Path.Combine(distribDir, fontPatchFile), Path.Combine(targetDir, "sqpack", "ffxiv", fontPatchFile), true);
            }
        }

        // This removes the patch and restore the client to original.
        static void Remove()
        {
            foreach (string restoreFile in restoreFiles)
            {
                File.Copy(Path.Combine(distribDir, "orig", restoreFile), Path.Combine(targetDir, "sqpack", "ffxiv", restoreFile), true);
            }
        }
    }
}
