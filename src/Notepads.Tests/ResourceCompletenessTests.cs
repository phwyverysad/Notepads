// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ResourceCompletenessTests
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

        private static Dictionary<string, string> LoadReswEntries(string reswPath)
        {
            Assert.IsTrue(File.Exists(reswPath), $"Resw file does not exist: {reswPath}");

            var doc = XDocument.Load(reswPath);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dataElement in doc.Descendants("data"))
            {
                string name = dataElement.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    string value = dataElement.Element("value")?.Value ?? string.Empty;
                    result[name] = value;
                }
            }

            return result;
        }

        [TestMethod]
        public void ResourcesResw_Thai_ContainsAllEnglishKeys()
        {
            string srcRoot = GetSolutionRoot();
            string enPath = Path.Combine(srcRoot, "Notepads", "Strings", "en-US", "Resources.resw");
            string thPath = Path.Combine(srcRoot, "Notepads", "Strings", "th-TH", "Resources.resw");

            var enKeys = LoadReswEntries(enPath);
            var thKeys = LoadReswEntries(thPath);

            var missingKeys = enKeys.Keys.Where(k => !thKeys.ContainsKey(k)).ToList();

            Assert.AreEqual(0, missingKeys.Count,
                $"The following keys from en-US are missing in th-TH Resources.resw: {string.Join(", ", missingKeys)}");

            foreach (var kvp in thKeys)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has an empty value in th-TH Resources.resw");
            }
        }

        [TestMethod]
        public void SettingsResw_Thai_ContainsAllEnglishKeys()
        {
            string srcRoot = GetSolutionRoot();
            string enPath = Path.Combine(srcRoot, "Notepads", "Strings", "en-US", "Settings.resw");
            string thPath = Path.Combine(srcRoot, "Notepads", "Strings", "th-TH", "Settings.resw");

            var enKeys = LoadReswEntries(enPath);
            var thKeys = LoadReswEntries(thPath);

            var missingKeys = enKeys.Keys.Where(k => !thKeys.ContainsKey(k)).ToList();

            Assert.AreEqual(0, missingKeys.Count,
                $"The following keys from en-US are missing in th-TH Settings.resw: {string.Join(", ", missingKeys)}");

            foreach (var kvp in thKeys)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has an empty value in th-TH Settings.resw");
            }
        }

        [TestMethod]
        public void ManifestResw_Thai_ContainsAllEnglishKeys()
        {
            string srcRoot = GetSolutionRoot();
            string enPath = Path.Combine(srcRoot, "Notepads", "Strings", "en-US", "Manifest.resw");
            string thPath = Path.Combine(srcRoot, "Notepads", "Strings", "th-TH", "Manifest.resw");

            var enKeys = LoadReswEntries(enPath);
            var thKeys = LoadReswEntries(thPath);

            var missingKeys = enKeys.Keys.Where(k => !thKeys.ContainsKey(k)).ToList();

            Assert.AreEqual(0, missingKeys.Count,
                $"The following keys from en-US are missing in th-TH Manifest.resw: {string.Join(", ", missingKeys)}");

            foreach (var kvp in thKeys)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has an empty value in th-TH Manifest.resw");
            }
        }
    }
}
