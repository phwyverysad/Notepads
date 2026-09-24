// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Tests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebInstallerTests
    {
        private static string GetInstallDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Notepads");
        }

        private static string GetTargetExecutable()
        {
            return Path.Combine(GetInstallDirectory(), "Notepads.exe");
        }

        private static string BuildQuotedArguments(string[] args)
        {
            if (args == null || args.Length == 0) return string.Empty;
            return string.Join(" ", args.Select(a => $"\"{a}\""));
        }

        [TestMethod]
        public void InstallDirectory_IsUnderLocalPrograms()
        {
            string installDir = GetInstallDirectory();
            StringAssert.Contains(installDir, "Programs\\Notepads");
            StringAssert.StartsWith(installDir, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        [TestMethod]
        public void TargetExecutable_HasNotepadsExeName()
        {
            string targetExe = GetTargetExecutable();
            Assert.IsTrue(targetExe.EndsWith("Notepads.exe", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ArgumentForwarding_CorrectlyQuotesArgumentsWithSpaces()
        {
            string[] args = new[] { @"C:\Users\John Doe\Documents\Notes.md", "--read-only" };
            string quoted = BuildQuotedArguments(args);

            Assert.AreEqual("\"C:\\Users\\John Doe\\Documents\\Notes.md\" \"--read-only\"", quoted);
        }

        [TestMethod]
        public void ArgumentForwarding_HandlesEmptyOrNull()
        {
            Assert.AreEqual(string.Empty, BuildQuotedArguments(null));
            Assert.AreEqual(string.Empty, BuildQuotedArguments(Array.Empty<string>()));
        }

        [TestMethod]
        public void ZipPackage_CanBeExtractedSuccessfully()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Notepads-TestExtract-" + Guid.NewGuid());
            string tempZip = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid() + ".zip");

            try
            {
                // Create a test zip file
                using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry("Notepads.exe");
                    using (var writer = new StreamWriter(entry.Open()))
                    {
                        writer.Write("Mock Notepads Binary");
                    }
                }

                // Test extraction
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(tempZip, tempDir);

                string extractedExe = Path.Combine(tempDir, "Notepads.exe");
                Assert.IsTrue(File.Exists(extractedExe), "Extracted Notepads.exe must exist");
                Assert.AreEqual("Mock Notepads Binary", File.ReadAllText(extractedExe));
            }
            finally
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
