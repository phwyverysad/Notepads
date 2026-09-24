// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Tests
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AutoSaveSettingsTests
    {
        private static string GetSolutionRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            while (current != null && !File.Exists(Path.Combine(current, "Notepads.sln")) && !File.Exists(Path.Combine(current, "src", "Notepads.sln")))
            {
                current = Directory.GetParent(current)?.FullName;
            }

            if (current == null)
            {
                throw new InvalidOperationException("Could not find solution root");
            }

            return File.Exists(Path.Combine(current, "src", "Notepads.sln"))
                ? Path.Combine(current, "src")
                : current;
        }

        [TestMethod]
        public void AutoSaveSession_ThaiResourceStrings_AreValidAndPresent()
        {
            string srcRoot = GetSolutionRoot();
            string thSettingsPath = Path.Combine(srcRoot, "Notepads", "Strings", "th-TH", "Settings.resw");
            Assert.IsTrue(File.Exists(thSettingsPath), "th-TH Settings.resw must exist");

            var doc = XDocument.Load(thSettingsPath);
            string title = null;
            string onContent = null;
            string desc = null;

            foreach (var elem in doc.Descendants("data"))
            {
                string name = elem.Attribute("name")?.Value;
                if (name == "AdvancedPage_SessionSnapshotSettings_Title.Text")
                {
                    title = elem.Element("value")?.Value;
                }
                else if (name == "AdvancedPage_SessionSnapshotSettings_OnOffToggleSwitch.OnContent")
                {
                    onContent = elem.Element("value")?.Value;
                }
                else if (name == "AdvancedPage_SessionSnapshotSettings_Description.Text")
                {
                    desc = elem.Element("value")?.Value;
                }
            }

            Assert.IsNotNull(title, "Title for SessionSnapshot must be present in th-TH");
            Assert.IsNotNull(onContent, "OnContent for SessionSnapshot must be present in th-TH");
            Assert.IsNotNull(desc, "Description for SessionSnapshot must be present in th-TH");

            StringAssert.Contains(title, "บันทึกอัตโนมัติและจำแท็บเมื่อปิดโปรแกรม");
            StringAssert.Contains(onContent, "บันทึกอัตโนมัติและจำแท็บในโปรแกรม");
            StringAssert.Contains(desc, "ไม่ต้องบันทึกเป็นไฟล์แยก");
        }

        [TestMethod]
        public void AutoSaveSession_EnglishResourceStrings_AreValidAndPresent()
        {
            string srcRoot = GetSolutionRoot();
            string enSettingsPath = Path.Combine(srcRoot, "Notepads", "Strings", "en-US", "Settings.resw");
            Assert.IsTrue(File.Exists(enSettingsPath), "en-US Settings.resw must exist");

            var doc = XDocument.Load(enSettingsPath);
            string title = null;
            string onContent = null;
            string desc = null;

            foreach (var elem in doc.Descendants("data"))
            {
                string name = elem.Attribute("name")?.Value;
                if (name == "AdvancedPage_SessionSnapshotSettings_Title.Text")
                {
                    title = elem.Element("value")?.Value;
                }
                else if (name == "AdvancedPage_SessionSnapshotSettings_OnOffToggleSwitch.OnContent")
                {
                    onContent = elem.Element("value")?.Value;
                }
                else if (name == "AdvancedPage_SessionSnapshotSettings_Description.Text")
                {
                    desc = elem.Element("value")?.Value;
                }
            }

            Assert.IsNotNull(title, "Title for SessionSnapshot must be present in en-US");
            Assert.IsNotNull(onContent, "OnContent for SessionSnapshot must be present in en-US");
            Assert.IsNotNull(desc, "Description for SessionSnapshot must be present in en-US");
        }
    }
}
