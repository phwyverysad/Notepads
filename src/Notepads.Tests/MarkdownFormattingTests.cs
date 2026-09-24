// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Tests
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MarkdownFormattingTests
    {
        public static string WrapSelection(string selectedText, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(selectedText)) return prefix + suffix;
            return prefix + selectedText + suffix;
        }

        public static string FormatLinePrefix(string currentLine, string newPrefix)
        {
            if (currentLine == null) currentLine = string.Empty;
            string trimmed = currentLine.TrimStart();
            int leadingSpaces = currentLine.Length - trimmed.Length;
            string indent = leadingSpaces > 0 ? currentLine.Substring(0, leadingSpaces) : string.Empty;

            // Remove existing header or list markers
            var markerRegex = new Regex(@"^(#{1,6}\s+|-\s+|\*\s+|\d+\.\s+)");
            if (markerRegex.IsMatch(trimmed))
            {
                trimmed = markerRegex.Replace(trimmed, string.Empty);
            }

            return indent + newPrefix + trimmed;
        }

        public static string BuildMarkdownTable(int rows, int cols)
        {
            if (rows < 1) rows = 1;
            if (cols < 1) cols = 1;

            var sb = new StringBuilder();
            sb.AppendLine();

            // Header
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append($" คอลัมน์ {c} |");
            }
            sb.AppendLine();

            // Separator
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append(":---|");
            }
            sb.AppendLine();

            // Rows (rows - 1 data rows since row 1 is header)
            int dataRows = Math.Max(1, rows - 1);
            for (int r = 1; r <= dataRows; r++)
            {
                sb.Append("|");
                for (int c = 1; c <= cols; c++)
                {
                    sb.Append($" ข้อมูล {r},{c} |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string ClearMarkdownFormatting(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 1. Remove bold/italic: ***text***, **text**, *text*, ___text___, __text__, _text_
            text = Regex.Replace(text, @"\*{1,3}(.*?)\*{1,3}", "$1");
            text = Regex.Replace(text, @"_{1,3}(.*?)_{1,3}", "$1");

            // 2. Remove strikethrough: ~~text~~
            text = Regex.Replace(text, @"~~(.*?)~~", "$1");

            // 3. Remove inline code: `text`
            text = Regex.Replace(text, @"`(.*?)`", "$1");

            // 4. Remove markdown links: [text](url) -> text
            text = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1");

            // 5. Remove headings: # heading -> heading
            text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);

            // 6. Remove list markers: - item, * item, 1. item
            text = Regex.Replace(text, @"^(\s*)(?:[-*+]|\d+\.)\s+", "$1", RegexOptions.Multiline);

            // 7. Remove blockquotes: > quote -> quote
            text = Regex.Replace(text, @"^(\s*)>\s+", "$1", RegexOptions.Multiline);

            return text;
        }

        [TestMethod]
        public void Bold_WrapsSelectionWithDoubleAsterisks()
        {
            string result = WrapSelection("ข้อความตัวหนา", "**", "**");
            Assert.AreEqual("**ข้อความตัวหนา**", result);
        }

        [TestMethod]
        public void Italic_WrapsSelectionWithSingleAsterisk()
        {
            string result = WrapSelection("ข้อความตัวเอียง", "*", "*");
            Assert.AreEqual("*ข้อความตัวเอียง*", result);
        }

        [TestMethod]
        public void Strikethrough_WrapsSelectionWithTildes()
        {
            string result = WrapSelection("ข้อความขีดทับ", "~~", "~~");
            Assert.AreEqual("~~ข้อความขีดทับ~~", result);
        }

        [TestMethod]
        public void Link_WrapsSelectionWithMarkdownLinkSyntax()
        {
            string result = WrapSelection("กดตรงนี้", "[", "](https://example.com)");
            Assert.AreEqual("[กดตรงนี้](https://example.com)", result);
        }

        [TestMethod]
        public void Headings_FormatsLinePrefixH1ToH6()
        {
            string line = "บทความแนะนำ Notepads";

            string h1 = FormatLinePrefix(line, "# ");
            Assert.AreEqual("# บทความแนะนำ Notepads", h1);

            string h2 = FormatLinePrefix(h1, "## ");
            Assert.AreEqual("## บทความแนะนำ Notepads", h2);

            string h3 = FormatLinePrefix(h2, "### ");
            Assert.AreEqual("### บทความแนะนำ Notepads", h3);
        }

        [TestMethod]
        public void Lists_FormatsBulletAndNumberedLists()
        {
            string line = "รายการข้อที่หนึ่ง";

            string bullet = FormatLinePrefix(line, "- ");
            Assert.AreEqual("- รายการข้อที่หนึ่ง", bullet);

            string numbered = FormatLinePrefix(bullet, "1. ");
            Assert.AreEqual("1. รายการข้อที่หนึ่ง", numbered);
        }

        [TestMethod]
        public void TableGenerator_CreatesValid3x3MarkdownTable()
        {
            string table = BuildMarkdownTable(3, 3);

            StringAssert.Contains(table, "| คอลัมน์ 1 | คอลัมน์ 2 | คอลัมน์ 3 |");
            StringAssert.Contains(table, "|:---|:---|:---|");
            StringAssert.Contains(table, "| ข้อมูล 1,1 | ข้อมูล 1,2 | ข้อมูล 1,3 |");
            StringAssert.Contains(table, "| ข้อมูล 2,1 | ข้อมูล 2,2 | ข้อมูล 2,3 |");
        }

        [TestMethod]
        public void TableGenerator_CreatesValid5x5MarkdownTable()
        {
            string table = BuildMarkdownTable(5, 5);

            StringAssert.Contains(table, "| คอลัมน์ 1 | คอลัมน์ 2 | คอลัมน์ 3 | คอลัมน์ 4 | คอลัมน์ 5 |");
            StringAssert.Contains(table, "| ข้อมูล 4,5 |");
        }

        [TestMethod]
        public void ClearFormatting_RemovesAllMarkdownSyntax()
        {
            string input = "**ตัวหนา** และ *ตัวเอียง* และ ~~ขีดทับ~~ และ [ลิงก์](https://google.com) และ `โค้ด`";
            string cleared = ClearMarkdownFormatting(input);

            Assert.AreEqual("ตัวหนา และ ตัวเอียง และ ขีดทับ และ ลิงก์ และ โค้ด", cleared);
        }

        [TestMethod]
        public void ClearFormatting_RemovesHeadingsAndLists()
        {
            string input = "### หัวเรื่องขนาด 3\n- รายการแบบจุด\n1. รายการแบบตัวเลข";
            string cleared = ClearMarkdownFormatting(input);

            Assert.AreEqual("หัวเรื่องขนาด 3\nรายการแบบจุด\nรายการแบบตัวเลข", cleared);
        }

        [TestMethod]
        public void ClearFormatting_HandlesNestedAndComplexSyntax()
        {
            string input = "> ข้อความอ้างอิง\n***ข้อความตัวหนาและเอียง***\n[ข้อความลิงก์](https://example.com/page?id=1&name=test)";
            string cleared = ClearMarkdownFormatting(input);

            StringAssert.Contains(cleared, "ข้อความอ้างอิง");
            StringAssert.Contains(cleared, "ข้อความตัวหนาและเอียง");
            StringAssert.Contains(cleared, "ข้อความลิงก์");
            Assert.IsFalse(cleared.Contains("***"));
            Assert.IsFalse(cleared.Contains("https://"));
        }

        [TestMethod]
        public void TableGenerator_CreatesValid1x1Table()
        {
            string table = BuildMarkdownTable(1, 1);

            StringAssert.Contains(table, "| คอลัมน์ 1 |");
            StringAssert.Contains(table, "|:---|");
            StringAssert.Contains(table, "| ข้อมูล 1,1 |");
        }

        [TestMethod]
        public void TableGenerator_CreatesValid2x4Table()
        {
            string table = BuildMarkdownTable(2, 4);

            StringAssert.Contains(table, "| คอลัมน์ 1 | คอลัมน์ 2 | คอลัมน์ 3 | คอลัมน์ 4 |");
            StringAssert.Contains(table, "|:---|:---|:---|:---|");
            StringAssert.Contains(table, "| ข้อมูล 1,4 |");
        }

        [TestMethod]
        public void FormatLinePrefix_HandlesIndentedLines()
        {
            string line = "    ข้อความมีการเยื้อง 4 เคาะ";
            string result = FormatLinePrefix(line, "- ");

            Assert.AreEqual("    - ข้อความมีการเยื้อง 4 เคาะ", result);
        }

        [TestMethod]
        public void WrapSelection_HandlesEmptySelection()
        {
            string boldEmpty = WrapSelection(string.Empty, "**", "**");
            Assert.AreEqual("****", boldEmpty);

            string italicEmpty = WrapSelection(string.Empty, "*", "*");
            Assert.AreEqual("**", italicEmpty);

            string linkEmpty = WrapSelection(string.Empty, "[", "](https://)");
            Assert.AreEqual("[](https://)", linkEmpty);
        }
    }
}
