// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.WebInstaller
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Win32;

    internal static class Program
    {
        private const string PackageDownloadUrl = "https://github.com/phwyverysad/Notepads/releases/latest/download/Notepads-x64.zip";
        private const string FallbackDownloadUrl = "https://github.com/0x7c13/Notepads/releases/latest/download/Notepads-x64.zip";

        internal static readonly string InstallDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Notepads");

        internal static readonly string TargetExecutable = Path.Combine(InstallDirectory, "Notepads.exe");

        [STAThread]
        private static void Main(string[] args)
        {
            // 1. Instant check: If already installed (standalone exe or Appx protocol), launch immediately in milliseconds!
            if (IsAppInstalled())
            {
                LaunchInstalledApp(args);
                return;
            }

            // 2. Otherwise, run zero-click automatic installer UI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm(args));
        }

        internal static bool IsAppInstalled()
        {
            if (File.Exists(TargetExecutable)) return true;

            try
            {
                using (var key = Registry.ClassesRoot.OpenSubKey("notepads"))
                {
                    if (key != null) return true;
                }
            }
            catch { }

            return false;
        }

        internal static void LaunchInstalledApp(string[] args)
        {
            try
            {
                if (File.Exists(TargetExecutable))
                {
                    var psi = new ProcessStartInfo(TargetExecutable)
                    {
                        UseShellExecute = false,
                        WorkingDirectory = InstallDirectory
                    };

                    if (args != null && args.Length > 0)
                    {
                        psi.Arguments = string.Join(" ", args.Select(a => $"\"{a}\""));
                    }

                    Process.Start(psi);
                    return;
                }

                // If installed via UWP Appx package, launch via protocol or shell
                string argStr = (args != null && args.Length > 0)
                    ? string.Join(" ", args.Select(a => $"\"{a}\""))
                    : string.Empty;

                var protocolPsi = new ProcessStartInfo("notepads:", argStr)
                {
                    UseShellExecute = true
                };
                Process.Start(protocolPsi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch Notepads: " + ex.Message, "Notepads", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class InstallerForm : Form
    {
        private readonly string[] _launchArgs;
        private Label _lblTitle;
        private Label _lblStatus;
        private ProgressBar _progressBar;

        public InstallerForm(string[] launchArgs)
        {
            _launchArgs = launchArgs;
            InitializeComponent();
            Shown += async (s, e) => await StartInstallationAsync();
        }

        private void InitializeComponent()
        {
            Text = "Notepads Installer";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 160);
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            _lblTitle = new Label
            {
                Text = "Notepads (ภาษาไทย)",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Location = new Point(24, 20),
                AutoSize = true
            };

            _lblStatus = new Label
            {
                Text = "กำลังเตรียมการติดตั้ง...",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(108, 117, 125),
                Location = new Point(25, 55),
                Size = new Size(370, 25)
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(25, 88),
                Size = new Size(370, 22),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            Controls.Add(_lblTitle);
            Controls.Add(_lblStatus);
            Controls.Add(_progressBar);
        }

        private async Task StartInstallationAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                string localZipFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notepads-x64.zip");
                string tempZipPath = Path.Combine(Path.GetTempPath(), "Notepads-Install-" + Guid.NewGuid() + ".zip");

                if (File.Exists(localZipFile))
                {
                    _lblStatus.Text = "กำลังแตกไฟล์ติดตั้งจากแพ็กเกจท้องถิ่น...";
                    tempZipPath = localZipFile;
                }
                else
                {
                    _lblStatus.Text = "กำลังดาวน์โหลด Notepads...";
                    _progressBar.Style = ProgressBarStyle.Continuous;

                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("User-Agent", "Notepads-WebInstaller/1.0");
                        webClient.DownloadProgressChanged += (s, e) =>
                        {
                            _progressBar.Value = e.ProgressPercentage;
                            _lblStatus.Text = $"กำลังดาวน์โหลด Notepads... ({e.ProgressPercentage}%)";
                        };

                        try
                        {
                            await webClient.DownloadFileTaskAsync(new Uri("https://github.com/phwyverysad/Notepads/releases/latest/download/Notepads-x64.zip"), tempZipPath);
                        }
                        catch
                        {
                            await webClient.DownloadFileTaskAsync(new Uri("https://github.com/0x7c13/Notepads/releases/latest/download/Notepads-x64.zip"), tempZipPath);
                        }
                    }
                }

                _lblStatus.Text = "กำลังติดตั้ง Notepads...";
                _progressBar.Style = ProgressBarStyle.Marquee;

                await Task.Run(() =>
                {
                    string extractDir = Path.Combine(Path.GetTempPath(), "Notepads-Extract-" + Guid.NewGuid());
                    try
                    {
                        Directory.CreateDirectory(extractDir);
                        ZipFile.ExtractToDirectory(tempZipPath, extractDir);

                        // If zip contains .msix package, install silently via Add-AppxPackage
                        string[] msixFiles = Directory.GetFiles(extractDir, "*.msix", SearchOption.AllDirectories);
                        if (msixFiles.Length > 0)
                        {
                            string msixPath = msixFiles[0];
                            var psPsi = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -Command \"Add-AppxPackage -Path '{msixPath}'\"")
                            {
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            var p = Process.Start(psPsi);
                            p?.WaitForExit(30000);
                        }

                        // Also place binaries into LocalAppData Programs directory if available
                        if (Directory.Exists(Program.InstallDirectory))
                        {
                            try { Directory.Delete(Program.InstallDirectory, true); } catch { }
                        }
                        Directory.CreateDirectory(Program.InstallDirectory);

                        CopyDirectory(extractDir, Program.InstallDirectory);

                        // Create Desktop and Start Menu shortcuts
                        CreateShortcuts(Program.TargetExecutable);

                        // Register in App Paths for Win+R "notepads"
                        RegisterAppPaths(Program.TargetExecutable);
                    }
                    finally
                    {
                        try { Directory.Delete(extractDir, true); } catch { }
                        if (tempZipPath != localZipFile && File.Exists(tempZipPath))
                        {
                            try { File.Delete(tempZipPath); } catch { }
                        }
                    }
                });

                _lblStatus.Text = "การติดตั้งเสร็จสมบูรณ์! กำลังเปิดโปรแกรม...";
                await Task.Delay(300);

                // Launch application immediately
                Program.LaunchInstalledApp(_launchArgs);

                // Close installer
                Close();
            }
            catch (Exception ex)
            {
                _progressBar.Visible = false;
                _lblStatus.ForeColor = Color.Red;
                _lblStatus.Text = "ข้อผิดพลาดในการติดตั้ง: " + ex.Message;
                MessageBox.Show("เกิดข้อผิดพลาดในการติดตั้ง:\n" + ex.Message, "Notepads Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, dest);
            }
        }

        private static void CreateShortcuts(string targetExe)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

                string desktopLnk = Path.Combine(desktopPath, "Notepads.lnk");
                string startMenuLnk = Path.Combine(startMenuPath, "Notepads.lnk");

                CreateWScriptShortcut(desktopLnk, targetExe, "Notepads - Text Editor with Markdown");
                CreateWScriptShortcut(startMenuLnk, targetExe, "Notepads - Text Editor with Markdown");
            }
            catch { }
        }

        private static void CreateWScriptShortcut(string lnkPath, string targetExe, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = targetExe;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
                shortcut.Description = description;
                shortcut.Save();
            }
            catch { }
        }

        private static void RegisterAppPaths(string targetExe)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\Notepads.exe"))
                {
                    if (key != null)
                    {
                        key.SetValue("", targetExe);
                        key.SetValue("Path", Path.GetDirectoryName(targetExe));
                    }
                }
            }
            catch { }
        }
    }
}
