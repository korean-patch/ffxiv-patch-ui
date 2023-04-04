using System.ComponentModel;
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Text;

namespace FFXIVKoreanPatch.Main
{
    public partial class FFXIVKoreanPatch : Form
    {
        // Shared event handler for background workers to show progress using progress bar.
        private void progressChanged(object sender, ProgressChangedEventArgs e)
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

                // Check if any worker processes are already running and kill them if they are.
                foreach (Process p in Process.GetProcesses().Where(p => new string[] { patcherFileName, updaterFileName }.Contains(p.ProcessName)))
                {
                    p.Kill();
                    p.WaitForExit();
                }

                // Populate necessary paths.
                mainPath = Application.ExecutablePath;
                mainTempPath = Path.Combine(Application.CommonAppDataPath, $"{mainFileName}.exe");
                patcherPath = Path.Combine(Application.CommonAppDataPath, $"{patcherFileName}.exe");
                updaterPath = Path.Combine(Application.CommonAppDataPath, $"{updaterFileName}.exe");
                distribDir = Path.Combine(Application.CommonAppDataPath, "distrib");

                // Create the distrib directory if it doesn't exist.
                Directory.CreateDirectory(distribDir);

                // Grab the necessary worker processes from the server.
                UpdateStatusLabel("필요한 프로그램 가져오는 중...");

                try
                {
                    // Check and download patcher.
                    CheckAndDownload($"{serverUrl}/program", $"{patcherFileName}.exe", patcherPath, initialChecker);

                    // Check and download updater.
                    CheckAndDownload($"{serverUrl}/program", $"{updaterFileName}.exe", updaterPath, initialChecker);
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "필요한 프로그램들을 가져오는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Check main executable's version and update if necessary.
                UpdateStatusLabel("프로그램 버전 확인 중...");

                try
                {
                    // If SHA1 checksum doesn't match, main executable needs update.
                    if (!CheckSHA1(mainPath, $"{serverUrl}/program/{mainFileName}.exe.sha1", $"{mainFileName}.exe.sha1", initialChecker))
                    {
                        // Download the latest main executable from server and save it to temp path.
                        DownloadFile($"{serverUrl}/program/{mainFileName}.exe", $"{mainFileName}.exe", initialChecker, mainTempPath);

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
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "버전을 확인하는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
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
                            using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallSteamKeyName))
                            {
                                if (uninstallKey == null) continue;

                                object installLocation = uninstallKey.GetValue("InstallLocation");
                                if (installLocation == null) continue;

                                targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(installLocation.ToString(), "game")));
                                break;
                            }
                        }

                        // If target directory is still not set, search for square enix installation path.
                        if (string.IsNullOrEmpty(targetDir))
                        {
                            foreach (string uninstallKeyName in uninstallKeyNames)
                            {
                                using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallKeyName))
                                {
                                    if (uninstallKey == null) continue;

                                    foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                                    {
                                        using (RegistryKey subKey = uninstallKey.OpenSubKey(subKeyName))
                                        {
                                            if (subKey == null) continue;

                                            object displayName = subKey.GetValue("DisplayName");
                                            if (displayName == null || displayName.ToString() != "FINAL FANTASY XIV ONLINE") continue;

                                            object iconPath = subKey.GetValue("DisplayIcon");
                                            if (iconPath == null) continue;

                                            targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(iconPath.ToString()), "../game")));
                                            break;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(targetDir)) break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Any exception happened during detection, just set directory to not found so user can select it.
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

                            if (string.IsNullOrEmpty(targetDir))
                            {
                                ShowMessageBox(
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    "선택하신 경로가 올바르지 않아요.");
                                CloseForm();
                                return;
                            }
                        }
                        else
                        {
                            ShowMessageBox(
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                "선택하신 경로가 올바르지 않아요.");
                            CloseForm();
                            return;
                        }
                    }));
                }

                // If target directory is still invalid, quit.
                if (string.IsNullOrEmpty(targetDir))
                {
                    CloseForm();
                    return;
                }

                // Check if korean chat registry is installed.
                UpdateStatusLabel("한글 채팅 레지스트리 설치 확인 중...");

                // Check registry for scancode map.
                using (RegistryKey keyboardLayoutKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout"))
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

                            Process p = Process.Start(new ProcessStartInfo(patcherPath, "0 \"0\" \"0\"")
                            {
                                UseShellExecute = true,
                                Verb = "runas"
                            });

                            if (p == null)
                            {
                                ShowMessageBox(
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    "한글 채팅 레지스트리 설치 중 문제가 발생했어요.",
                                    "설치를 다시 시도해보세요.");
                                CloseForm();
                                return;
                            }
                            else
                            {
                                p.WaitForExit();

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
                }

                // Check the target client version.
                UpdateStatusLabel("클라이언트 버전 체크 중...");

                // Read the version from target client.
                targetVersion = File.ReadAllText(Path.Combine(targetDir, versionFileName));

                // Check if the server's client version is the same.
                UpdateStatusLabel($"서버에서 한글 패치 가져오는 중... 버전 {targetVersion}");

                try
                {
                    string serverVersion = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/distrib/{versionFileName}", versionFileName, initialChecker));

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

            // Start the background worker to install the korean patch...
            installWorker.RunWorkerAsync();
        }

        // Install the korean patch.
        private void installWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Check cached patch files and download from server only if SHA1 checksum is different.
                UpdateStatusLabel($"한글 패치 업데이트 확인 중... {targetVersion}");

                foreach (string distributedFileName in distributedFileNames)
                {
                    CheckAndDownload($"{serverUrl}/distrib", distributedFileName, Path.Combine(distribDir, distributedFileName), installWorker);
                }

                UpdateStatusLabel($"한글 패치 설치 중... {targetVersion}");

                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                // Run the child worker process with administrator access to copy the patch files.
                Process p = Process.Start(new ProcessStartInfo(patcherPath, $"1 \"{targetDir}\" \"{distribDir}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (p != null)
                {
                    p.WaitForExit();
                }

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

        private void chatOnlyInstallButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            installButton.Enabled = false;
            chatOnlyInstallButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to install the font patch...
            chatOnlyInstallWorker.RunWorkerAsync();
        }

        private void chatOnlyInstallWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Check cached patch files and download from server only if SHA1 checksum is different.
                UpdateStatusLabel($"한글 채팅 패치 업데이트 확인 중... {targetVersion}");

                foreach (string distributedFileName in distributedFileNames)
                {
                    CheckAndDownload($"{serverUrl}/distrib", distributedFileName, Path.Combine(distribDir, distributedFileName), chatOnlyInstallWorker);
                }

                UpdateStatusLabel($"한글 채팅 패치 설치 중... {targetVersion}");

                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                // Run the child worker process with administrator access to copy the patch files.
                Process p = Process.Start(new ProcessStartInfo(patcherPath, $"2 \"{targetDir}\" \"{distribDir}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (p != null)
                {
                    p.WaitForExit();
                }

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

        private void removeButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            installButton.Enabled = false;
            chatOnlyInstallButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to remove the korean patch...
            removeWorker.RunWorkerAsync();
        }

        private void removeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Create cache directory if it doesn't exist.
                Directory.CreateDirectory(Path.Combine(distribDir, "orig"));

                // Check cached patch files and download from server only if SHA1 checksum is different.
                UpdateStatusLabel($"한글 패치 업데이트 확인 중... {targetVersion}");

                foreach (string distributedOrigFileName in distributedOrigFileNames)
                {
                    CheckAndDownload($"{serverUrl}/distrib/orig", distributedOrigFileName, Path.Combine(distribDir, "orig", distributedOrigFileName), removeWorker);
                }

                UpdateStatusLabel($"한글 패치 삭제 중... {targetVersion}");

                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                // Run the child worker process with administrator access to remove the patch files with original unpatched files.
                Process p = Process.Start(new ProcessStartInfo(patcherPath, $"3 \"{targetDir}\" \"{distribDir}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (p != null)
                {
                    p.WaitForExit();
                }

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
    }
}
