using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFXIVKoreanPatch.Main
{
    public partial class FFXIVKoreanPatch : Form
    {
        #region Variables

        // Github release server URL that hosts distributed patch files.
        private const string serverUrl = "https://github.com/korean-patch/ffxiv-korean-patch/releases/download/release";

        // Path for the main patch program.
        private string mainPath = string.Empty;
        private const string mainFileName = "FFXIVKoreanPatch";
        private string mainTempPath = string.Empty;

        // Path for the updater program.
        private string updaterPath = string.Empty;
        private const string updaterFileName = "FFXIVKoreanPatchUpdater";

        // Process names to check for before doing the patch.
        private string[] gameProcessNames = new string[]
        {
            "ffxivboot", "ffxivboot64",
            "ffxivlauncher", "ffxivlauncher64",
            "ffxiv", "ffxiv_dx11"
        };

        // List of known files that will be used to verify installation path.
        private string[] requiredFiles = new string[]
        {
            "ffxiv_dx11.exe",
            "sqpack/ffxiv/000000.win32.index",
            "sqpack/ffxiv/000000.win32.dat0",
            "sqpack/ffxiv/0a0000.win32.index",
            "sqpack/ffxiv/0a0000.win32.dat0",
            "../boot/ffxivboot.exe"
        };

        // List of file names that need to be manipulated.
        private string[] fontPatchFiles = new string[]
        {
            "000000.win32.dat1",
            "000000.win32.index"
        };

        private string[] fullPatchFiles = new string[]
        {
            "0a0000.win32.dat1",
            "0a0000.win32.index"
        };

        private string[] restoreFiles = new string[]
        {
            "000000.win32.index",
            "0a0000.win32.index"
        };

        // Scancode Map value for registry.
        private byte[] scancodeMap = new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x72, 0x00, 0x38, 0xe0,
            0x00, 0x00, 0x00, 0x00
        };

        // Name of the version file that denotes target game client version for the patch.
        private const string versionFileName = "ffxivgame.ver";

        // Target client directory.
        private string targetDir = string.Empty;

        // Target client version.
        private string targetVersion = string.Empty;

        #endregion

        public FFXIVKoreanPatch()
        {
            InitializeComponent();

            // Adjust the background to apply gradient effect.
            AdjustBackground();

            // Empty the labels.
            statusLabel.Text = "";
            downloadLabel.Text = "";

            // Run the initial checker to verify and set up environment.
            initialChecker.RunWorkerAsync();
        }

        #region Functions

        // Grab the background from the form and apply gradient effect.
        private void AdjustBackground()
        {
            // Get the background image as Bitmap first.
            Bitmap origImage = (Bitmap)BackgroundImage;

            // Create a new image that will be used as a new background.
            // This should have the same width as the form, and the same width-height ratio.
            Bitmap newImage = new Bitmap(ClientSize.Width, ClientSize.Width * origImage.Height / origImage.Width);
            newImage.SetResolution(origImage.HorizontalResolution, origImage.VerticalResolution);

            // Starting drawing in the new image...
            using (Graphics g = Graphics.FromImage(newImage))
            {
                // Prepare a rectangle to copy over the original image.
                Rectangle rect = new Rectangle(0, 0, newImage.Width, newImage.Height);

                // Set Graphics parameters...
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Copy over the original image.
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(origImage, rect, 0, 0, origImage.Width, origImage.Height, GraphicsUnit.Pixel, wrapMode);
                }

                // Prepare a linear gradient brush that is transparent on top and form back color at the bottom.
                LinearGradientBrush brush = new LinearGradientBrush(rect, Color.Transparent, BackColor, 90f);

                // Draw on top of the original image.
                g.FillRectangle(brush, rect);
            }

            // Set the new image as background.
            BackgroundImage = newImage;
        }

        // Display message box from UI thread.
        private DialogResult ShowMessageBox(MessageBoxButtons buttons, MessageBoxIcon icon, params string[] lines)
        {
            // Compile a single string from given text lines.
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(lines[i]);

                // Append 2 new lines in between...
                if (i != lines.Length - 1)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
                }
            }

            // Display message box on UI thread with given parameters.
            return (DialogResult)Invoke(new Func<DialogResult>(() =>
            {
                return MessageBox.Show(sb.ToString(), Text, buttons, icon);
            }));
        }

        // Update status label text always from UI thread.
        private void UpdateStatusLabel(string text)
        {
            Invoke(new Action(() =>
            {
                statusLabel.Text = text;
            }));
        }

        // Update download label text always from UI thread.
        private void UpdateDownloadLabel(string text)
        {
            Invoke(new Action(() =>
            {
                downloadLabel.Text = text;
            }));
        }

        // Close the form always from UI thread.
        private void CloseForm()
        {
            Invoke(new Action(() =>
            {
                Close();
            }));
        }

        // Validates the target client directory by checking if required files are present.
        // Return the directory back if valid, else return null.
        private string CheckTargetDir(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return null;
            if (!requiredFiles.All(requiredFile => File.Exists(Path.Combine(targetDir, requiredFile)))) return null;

            return targetDir;
        }

        // Downloads a file from given url while reporting progress on given background worker, then return the response as byte array.
        // If filePath is given, it will write to FileStream instead of memory.
        private byte[] DownloadFile(string url, string fileName, BackgroundWorker worker, string filePath = null)
        {
            using (HttpClient client = new HttpClient())
            {
                // Default user agent and timeout values.
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromMinutes(5);

                // Indicate what file we're downloading...
                UpdateDownloadLabel($"다운로드중: {fileName}");

                // Download the header first to look at the content length.
                using (HttpResponseMessage responseMessage = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (responseMessage.Content.Headers.ContentLength != null)
                    {
                        long contentLength = (long)responseMessage.Content.Headers.ContentLength;

                        // Create memory stream or file stream based on param and feed it with the client stream.
                        using (Stream s = string.IsNullOrEmpty(filePath) ? (Stream)new MemoryStream() : new FileStream(filePath, FileMode.Create))
                        using (Stream inStream = responseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            // Create a progress reporter that reports the progress to the designated background worker.
                            Progress<int> p = new Progress<int>(new Action<int>((value) =>
                            {
                                worker.ReportProgress(value);
                            }));

                            // Buffer size is 1/10 of the total content.
                            // Grab data from http client stream and copy to destination stream.
                            inStream.CopyToAsync(s, (int)(contentLength / 10), contentLength, p).GetAwaiter().GetResult();

                            // Empty the progress bar after download is complete.
                            worker.ReportProgress(0);

                            // Reset the label.
                            UpdateDownloadLabel("");

                            // If no file path was specified, return as byte array. Else just return.
                            if (string.IsNullOrEmpty(filePath))
                            {
                                return ((MemoryStream)s).ToArray();
                            }
                            else
                            {
                                return new byte[0];
                            }
                        }
                    }
                }
            }

            // If anything happened and didn't reach successful download, throw an exception.
            throw new Exception($"다음 파일을 다운로드하는 중 오류가 발생하였습니다. {url}");
        }

        // Clear all cached files.
        private void ClearCache()
        {
            foreach (string s in Directory.GetFiles(Application.CommonAppDataPath))
            {
                File.Delete(s);
            }
        }

        // Get the SHA1 checksum from a file.
        private string ComputeSHA1(string filePath)
        {
            using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(filePath))).Replace("-", "");
            }
        }

        // Check SHA1 checksum between given file and server record and return true if they match.
        private bool CheckSHA1(string filePath, string url, string fileName, BackgroundWorker worker)
        {
            return ComputeSHA1(filePath) == Encoding.ASCII.GetString(DownloadFile(url, fileName, worker));
        }

        #endregion

        #region Event Handlers

        // Show progress using progress bar.
        private void initialChecker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        // Background worker that does initial checks.
        private void initialChecker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Wait until the UI is ready.
                while (!statusLabel.IsHandleCreated) { }

                UpdateStatusLabel("환경 체크 중...");

                // Check if the patch program is already running, and terminate if it is.
                if (Process.GetProcessesByName(mainFileName).Length > 1)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "FFXIV 한글 패치 프로그램이 이미 실행중이에요.");
                    CloseForm();
                    return;
                }

                // Check if FFXIV game process is running.
                if (Process.GetProcesses().Any(p => gameProcessNames.Contains(p.ProcessName.ToLower())))
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "FFXIV가 이미 실행중이에요.",
                        "FFXIV를 종료한 후 한글 패치 프로그램을 다시 실행해주세요.");
                    CloseForm();
                    return;
                }

                // Populate necessary paths.
                mainPath = Application.ExecutablePath;
                mainTempPath = Path.Combine(Application.CommonAppDataPath, $"{mainFileName}.exe");
                updaterPath = Path.Combine(Application.CommonAppDataPath, $"{updaterFileName}.exe");

                // Clean up some stuff.
                ClearCache();

                // Check main executable's version and update if necessary.
                UpdateStatusLabel("프로그램 버전 확인 중...");

                try
                {
#if DEBUG
#else
                    // Check the current executable's checksum with the server.
                    if (!CheckSHA1(mainPath, $"{serverUrl}/{mainFileName}.exe.sha1", $"{mainFileName}.exe.sha1", initialChecker))
                    {
                        // If doesn't match, need to download the new binary and updater, then trigger an update.
                        DownloadFile($"{serverUrl}/{mainFileName}.exe", $"{mainFileName}.exe", initialChecker, mainTempPath);
                        DownloadFile($"{serverUrl}/{updaterFileName}.exe", $"{updaterFileName}.exe", initialChecker, updaterPath);

                        // Run updater worker process to update the main executable.
                        ShowMessageBox(
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            "업데이트가 필요해 프로그램을 종료할 거예요.",
                            "업데이트가 완료되면 자동으로 재실행할게요.");
                        Process.Start(new ProcessStartInfo(updaterPath, $"\"{mainPath}\" \"{mainTempPath}\""));
                        CloseForm();
                        return;
                    }
#endif
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "버전을 확인하는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용: ",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Try to detect FFXIV client path.
                UpdateStatusLabel("FFXIV 클라이언트를 찾는 중...");

                try
                {
                    // Check windows registry uninstall list to find the FFXIV installation.
                    string[] uninstallKeyNames = new string[]
                    {
                        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                        "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                    };

                    string[] uninstallSteamKeyNames = new string[]
                    {
                        $"{uninstallKeyNames[0]}\\Steam App 39210",
                        $"{uninstallKeyNames[1]}\\Steam App 39210"
                    };

                    // Check steam registry first...
                    using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
                    {
                        foreach (string uninstallSteamKeyName in uninstallSteamKeyNames)
                        {
                            if (!string.IsNullOrEmpty(targetDir)) break;

                            using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallSteamKeyName))
                            {
                                if (uninstallKey == null) continue;

                                object installLocation = uninstallKey.GetValue("InstallLocation");
                                if (installLocation == null) continue;

                                targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(installLocation.ToString(), "game")));
                            }
                        }

                        // If target directory is still not set, search for square enix installation path.
                        if (string.IsNullOrEmpty(targetDir))
                        {
                            foreach(string uninstallKeyName in uninstallKeyNames)
                            {
                                if (!string.IsNullOrEmpty(targetDir)) break;

                                using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallKeyName))
                                {
                                    if (uninstallKey == null) continue;

                                    foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                                    {
                                        if (!string.IsNullOrEmpty(targetDir)) break;

                                        using (RegistryKey subKey = uninstallKey.OpenSubKey(subKeyName))
                                        {
                                            if (subKey == null) continue;

                                            object displayName = subKey.GetValue("DisplayName");
                                            if (displayName == null || displayName.ToString() != "FINAL FANTASY XIV ONLINE") continue;

                                            object iconPath = subKey.GetValue("DisplayIcon");
                                            if (iconPath == null) continue;

                                            targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(iconPath.ToString()), "../game")));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Any exception happened during dtection, just set directory to not found so user can select it.
                    targetDir = string.Empty;
                }

                // If the installation location is found, ask user to confirm.
                bool isTargetDirVerified = false;

                if (!string.IsNullOrEmpty(targetDir))
                {
                    isTargetDirVerified = ShowMessageBox(
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information,
                        "다음 위치에서 FFXIV 클라이언트가 발견되었어요.",
                        targetDir,
                        "이 클라이언트에 한글 패치를 설치할까요?") == DialogResult.Yes;
                }

                // If the target directory is not verified, let user choose.
                if (!isTargetDirVerified)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        "FFXIV 클라이언트가 설치된 장소에서 ffxiv_dx11.exe 파일을 찾아 선택해주세요.",
                        "(보통 game 폴더 내부에 있어요.)");

                    // Start the dialog from UI thread.
                    Invoke(new Action(() =>
                    {
                        OpenFileDialog dialog = new OpenFileDialog()
                        {
                            CheckFileExists = true,
                            CheckPathExists = true,
                            DefaultExt = "exe",
                            Filter = "FFXIV|ffxiv_dx11.exe",
                            Multiselect = false,
                            Title = "ffxiv_dx11.exe 파일을 선택해주세요..."
                        };

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // Check if the selected directory is valid.
                            targetDir = CheckTargetDir(Path.GetFullPath(Path.GetDirectoryName(dialog.FileName)));
                        }
                    }));
                }

                // Last check for the targetDir.
                if (string.IsNullOrEmpty(targetDir))
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "선택된 경로가 올바르지 않아요.");
                    CloseForm();
                    return;
                }

                // Check if korean chat registry is installed.
                UpdateStatusLabel("한글 채팅 레지스트리 설치 확인 중...");

                // Check registry for scancode map.
                using (RegistryKey keyboardLayoutKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout", true))
                {
                    if (keyboardLayoutKey != null)
                    {
                        object scancodeMap = keyboardLayoutKey.GetValue("Scancode Map");

                        // If scancode map doesn't exist or is invalid...
                        if (scancodeMap == null || !this.scancodeMap.SequenceEqual((byte[])scancodeMap))
                        {
                            // Install scancode map.
                            UpdateStatusLabel("한글 채팅 레지스트리 설치 중...");

                            ShowMessageBox(
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information,
                                "한글 채팅 레지스트리를 설치할게요.",
                                "설치가 끝난 후 컴퓨터를 재시작하지 않으면 FFXIV 클라이언트 내부에서 한/영 키 입력이 제대로 동작하지 않을 수 있어요.");

                            keyboardLayoutKey.SetValue("Scancode Map", this.scancodeMap);

                            ShowMessageBox(
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information,
                                "한글 채팅 레지스트리 설치가 완료되었어요.",
                                "컴퓨터를 재시작한 후 다시 실행해주세요.");

                            CloseForm();
                            return;
                        }
                    }
                }

                // Check the target client version.
                UpdateStatusLabel("클라이언트 버전 체크 중...");

                // Read the version from target client.
                targetVersion = File.ReadAllText(Path.Combine(targetDir, versionFileName));

                // Check if the server's client version is the same.
                UpdateStatusLabel($"서버에서 한글 패치 가져오는 중... 버전 {targetVersion}");

                try
                {
                    string serverVersion = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/{versionFileName}", versionFileName, initialChecker));

                    if (!serverVersion.Equals(targetVersion))
                    {
                        ShowMessageBox(
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            "현재 설치된 게임 클라이언트의 버전이 서버의 버전과 달라요!",
                            $"클라이언트 버전: {targetVersion}, 서버 버전: {serverVersion}",
                            "문제가 지속되면 디스코드를 통해 문의해주세요.");
                        CloseForm();
                        return;
                    }
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "한글 패치 서버 버전을 확인하는데 실패했어요.",
                        "문제가 지속되면 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Check all done!
                UpdateStatusLabel($"버전 {targetVersion}");

                Invoke(new Action(() =>
                {
                    installButton.Enabled = true;
                    chatOnlyInstallButton.Enabled = true;
                    removeButton.Enabled = true;
                    progressBar.Value = 0;
                    downloadLabel.Text = "";
                }));
            }
            catch (Exception exception)
            {
                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    "처리되지 않은 예외가 발생했어요.",
                    "에러 내용:",
                    exception.ToString());
                CloseForm();
                return;
            }
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            installButton.Enabled = false;
            chatOnlyInstallButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to install the korean patch.
            installWorker.RunWorkerAsync();
        }

        private void DownloadWork(string[] patchFiles, bool isRemove = false)
        {
            try
            {
                // Clear cache.
                ClearCache();

                // Download all files.
                UpdateStatusLabel($"파일 다운로드 중... {targetVersion}");

                foreach (string patchFile in patchFiles)
                {
                    DownloadFile($"{serverUrl}/{(isRemove ? "orig." : "")}{patchFile}", patchFile, installWorker, Path.Combine(Application.CommonAppDataPath, patchFile));
                }

                UpdateStatusLabel($"파일 설치 중... {targetVersion}");
                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                foreach (string patchFile in patchFiles)
                {
                    File.Copy(Path.Combine(Application.CommonAppDataPath, patchFile), Path.Combine(targetDir, "sqpack", "ffxiv", patchFile), true);
                }

                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    $"{(isRemove ? "제거" : "설치")}가 성공적으로 완료되었어요!");
                CloseForm();
                return;
            }
            catch (Exception exception)
            {
                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    "처리되지 않은 예외가 발생했어요.",
                    "에러 내용:",
                    exception.ToString());
                CloseForm();
                return;
            }
        }

        private void installWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadWork(fontPatchFiles.Concat(fullPatchFiles).ToArray());
        }

        private void chatOnlyInstallButton_Click(object sender, EventArgs e)
        {
            DownloadWork(fontPatchFiles);
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            DownloadWork(restoreFiles, true);
        }

        #endregion
    }

    // Extending HTTP client stream to report download progress while copying.
    public static class StreamExtensions
    {
        // Extending CopyToAsync to accept interface that reports an integer progress.
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, long totalLength, IProgress<int> progress)
        {
            // Check parameters.
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("Has to be readable.", nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite) throw new ArgumentException("Has to be writable.", nameof(destination));
            if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            // Make a buffer with given buffer size.
            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            int progressReport = 0;

            // Fill buffer.
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                // Write buffer to destination.
                await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

                // Up the total counter.
                totalBytesRead += bytesRead;
                int newProgressReport = (int)(totalBytesRead * 100 / totalLength);

                // Only report if progress became higher.
                if (newProgressReport > progressReport)
                {
                    // Report the progress.
                    progressReport = newProgressReport;
                    progress.Report(progressReport);
                }
            }
        }
    }
}
