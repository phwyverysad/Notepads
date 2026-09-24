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
        internal const string PackageDownloadUrl = "https://github.com/phwyverysad/Notepads/releases/latest/download/Notepads-x64.zip";
        internal const string FallbackDownloadUrl = "https://github.com/0x7c13/Notepads/releases/latest/download/Notepads-x64.zip";

        internal static readonly string InstallDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Notepads");

        internal static readonly string PackageDirectory = Path.Combine(InstallDirectory, "Package");
        internal static readonly string ManifestPath = Path.Combine(PackageDirectory, "AppxManifest.xml");
        internal static readonly string TargetLauncher = Path.Combine(InstallDirectory, "Notepads.exe");

        [STAThread]
        private static void Main(string[] args)
        {
            // 1. Instant check: If already installed and registered, launch immediately in milliseconds!
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
            if (!File.Exists(ManifestPath))
            {
                return false;
            }

            try
            {
                using (var key = Registry.ClassesRoot.OpenSubKey("notepads"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }
            }
            catch { }

            // If files exist but protocol registration is missing, auto-register silently
            try
            {
                RegisterAppxPackage(ManifestPath);

                using (var key = Registry.ClassesRoot.OpenSubKey("notepads"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        internal static void RegisterAppxPackage(string manifestPath)
        {
            try
            {
                var psPsi = new ProcessStartInfo("powershell.exe",
                    $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Add-AppxPackage -Register '{manifestPath}' -ForceApplicationShutdown\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psPsi);
                p?.WaitForExit(45000);
            }
            catch { }
        }

        internal static void LaunchInstalledApp(string[] args)
        {
            try
            {
                var protocolPsi = new ProcessStartInfo("notepads:")
                {
                    UseShellExecute = true
                };
                Process.Start(protocolPsi);
            }
            catch
            {
                // Fallback via PowerShell Start-Process
                try
                {
                    var psPsi = new ProcessStartInfo("powershell.exe", "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -Command \"Start-Process 'notepads:'\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psPsi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to launch Notepads: " + ex.Message, "Notepads", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                            await webClient.DownloadFileTaskAsync(new Uri(Program.PackageDownloadUrl), tempZipPath);
                        }
                        catch
                        {
                            await webClient.DownloadFileTaskAsync(new Uri(Program.FallbackDownloadUrl), tempZipPath);
                        }
                    }
                }

                _lblStatus.Text = "กำลังติดตั้ง Notepads...";
                _progressBar.Style = ProgressBarStyle.Marquee;

                await Task.Run(() =>
                {
                    // 1. Prepare target directories
                    Directory.CreateDirectory(Program.InstallDirectory);

                    string tempExtractDir = Path.Combine(Path.GetTempPath(), "Notepads-Extract-" + Guid.NewGuid());
                    try
                    {
                        Directory.CreateDirectory(tempExtractDir);
                        ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

                        // Locate AppxManifest.xml in extracted contents
                        string foundManifest = Directory.GetFiles(tempExtractDir, "AppxManifest.xml", SearchOption.AllDirectories).FirstOrDefault();
                        if (string.IsNullOrEmpty(foundManifest))
                        {
                            throw new FileNotFoundException("Cannot find AppxManifest.xml inside package.");
                        }

                        string sourcePackageDir = Path.GetDirectoryName(foundManifest);

                        // Clean old package dir if exists
                        if (Directory.Exists(Program.PackageDirectory))
                        {
                            try
                            {
                                Directory.Delete(Program.PackageDirectory, true);
                            }
                            catch { }
                        }
                        Directory.CreateDirectory(Program.PackageDirectory);

                        // Copy all package files to PackageDirectory
                        CopyDirectory(sourcePackageDir, Program.PackageDirectory);

                        // 2. Register AppX package via PowerShell
                        Program.RegisterAppxPackage(Program.ManifestPath);

                        // 3. Place self as the launcher into InstallDirectory\Notepads.exe
                        string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                        if (!string.Equals(currentExe, Program.TargetLauncher, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                File.Copy(currentExe, Program.TargetLauncher, true);
                            }
                            catch { }
                        }

                        // 4. Create Desktop & Start Menu shortcuts
                        CreateShortcuts(Program.TargetLauncher);

                        // 5. Register in App Paths for Win+R "notepads"
                        RegisterAppPaths(Program.TargetLauncher);
                    }
                    finally
                    {
                        try { Directory.Delete(tempExtractDir, true); } catch { }
                        if (tempZipPath != localZipFile && File.Exists(tempZipPath))
                        {
                            try { File.Delete(tempZipPath); } catch { }
                        }
                    }
                });

                _lblStatus.Text = "การติดตั้งเสร็จสมบูรณ์! กำลังเปิดโปรแกรม...";
                await Task.Delay(300);

                // 6. Launch Notepads immediately
                Program.LaunchInstalledApp(_launchArgs);

                // 7. Close installer
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
                shortcut.IconLocation = targetExe + ",0";
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
