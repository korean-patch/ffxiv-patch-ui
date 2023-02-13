using System.IO;

namespace FFXIVKoreanPatch
{
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
        //         -> 0 = install full korean patch.
        //         -> 1 = install only font patch.
        //         -> 2 = remove korean patch and restore original.
        // args[1] -> path to the ffxiv client.
        // args[2] -> path to the cached korean patch files.
        static void Main(string[] args)
        {
            // Check arguments.
            if (args.Length != 3) return;
            if (!Directory.Exists(args[1])) return;
            if (!Directory.Exists(args[2])) return;

            // Populate paths.
            targetDir = args[1];
            distribDir = args[2];

            Cleanup();

            switch (args[0])
            {
                case "0":
                    InstallFull();
                    break;
                case "1":
                    InstallFont();
                    break;
                case "2":
                    Remove();
                    break;
            }
        }

        // This cleans up old korean chat patch with dinput hook.
        static void Cleanup()
        {
            string dllPath = Path.Combine(targetDir, "dinput8.dll");
            string dataDir = Path.Combine(targetDir, "data");

            if (File.Exists(dllPath)) File.Delete(dllPath);
            if (Directory.Exists(dataDir)) Directory.Delete(dataDir, true);
        }

        // This installs font and text patches.
        static void InstallFull()
        {
            InstallFont();

            foreach (string fullPatchFile in fullPatchFiles)
            {
                File.Copy(Path.Combine(distribDir, fullPatchFile), Path.Combine(targetDir, "sqpack/ffxiv", fullPatchFile), true);
            }
        }

        // This installs font patch.
        static void InstallFont()
        {
            foreach (string fontPatchFile in fontPatchFiles)
            {
                File.Copy(Path.Combine(distribDir, fontPatchFile), Path.Combine(targetDir, "sqpack/ffxiv", fontPatchFile), true);
            }
        }

        // This removes the patch and restore the client to original.
        static void Remove()
        {
            foreach (string restoreFile in restoreFiles)
            {
                File.Copy(Path.Combine(distribDir, "orig", restoreFile), Path.Combine(targetDir, "sqpack/ffxiv", restoreFile), true);
            }
        }
    }
}
