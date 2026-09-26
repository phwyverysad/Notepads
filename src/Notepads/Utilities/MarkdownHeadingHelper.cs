// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    public enum MarkdownHeadingStyle
    {
        Title,       // ชื่อ (H1: "# ")
        Subtitle,    // คำบรรยาย (H2: "## ")
        Heading1,    // ส่วนหัว (H3: "### ")
        Heading2,    // หัวเรื่องย่อย (H4: "#### ")
        Heading3,    // ส่วน (H5: "##### ")
        Heading4,    // ส่วนย่อย (H6: "###### ")
        Body         // เนื้อความ (Normal text, no heading prefix)
    }

    public static class MarkdownHeadingHelper
    {
        public const string PrefixTitle = "# ";
        public const string PrefixSubtitle = "## ";
        public const string PrefixHeading1 = "### ";
        public const string PrefixHeading2 = "#### ";
        public const string PrefixHeading3 = "##### ";
        public const string PrefixHeading4 = "###### ";
        public const string PrefixBody = "";

        private static readonly Regex HeadingRegex = new Regex(@"^(\s*)(#{1,6})\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex HeadingStripRegex = new Regex(@"^(\s*)#{1,6}\s*(.*)$", RegexOptions.Compiled);

        public static string GetPrefixForStyle(MarkdownHeadingStyle style)
        {
            switch (style)
            {
                case MarkdownHeadingStyle.Title: return PrefixTitle;
                case MarkdownHeadingStyle.Subtitle: return PrefixSubtitle;
                case MarkdownHeadingStyle.Heading1: return PrefixHeading1;
                case MarkdownHeadingStyle.Heading2: return PrefixHeading2;
                case MarkdownHeadingStyle.Heading3: return PrefixHeading3;
                case MarkdownHeadingStyle.Heading4: return PrefixHeading4;
                default: return PrefixBody;
            }
        }

        public static MarkdownHeadingStyle GetStyleForPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return MarkdownHeadingStyle.Body;
            string trimmed = prefix.Trim();
            switch (trimmed)
            {
                case "#": return MarkdownHeadingStyle.Title;
                case "##": return MarkdownHeadingStyle.Subtitle;
                case "###": return MarkdownHeadingStyle.Heading1;
                case "####": return MarkdownHeadingStyle.Heading2;
                case "#####": return MarkdownHeadingStyle.Heading3;
                case "######": return MarkdownHeadingStyle.Heading4;
                default: return MarkdownHeadingStyle.Body;
            }
        }

        public static string GetDisplayName(MarkdownHeadingStyle style)
        {
            switch (style)
            {
                case MarkdownHeadingStyle.Title: return "ชื่อ";
                case MarkdownHeadingStyle.Subtitle: return "คำบรรยาย";
                case MarkdownHeadingStyle.Heading1: return "ส่วนหัว";
                case MarkdownHeadingStyle.Heading2: return "หัวเรื่องย่อย";
                case MarkdownHeadingStyle.Heading3: return "ส่วน";
                case MarkdownHeadingStyle.Heading4: return "ส่วนย่อย";
                default: return "เนื้อความ";
            }
        }

        public static MarkdownHeadingStyle DetectStyle(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return MarkdownHeadingStyle.Body;

            var match = HeadingRegex.Match(line);
            if (!match.Success) return MarkdownHeadingStyle.Body;

            string hashes = match.Groups[2].Value;
            switch (hashes.Length)
            {
                case 1: return MarkdownHeadingStyle.Title;
                case 2: return MarkdownHeadingStyle.Subtitle;
                case 3: return MarkdownHeadingStyle.Heading1;
                case 4: return MarkdownHeadingStyle.Heading2;
                case 5: return MarkdownHeadingStyle.Heading3;
                case 6: return MarkdownHeadingStyle.Heading4;
                default: return MarkdownHeadingStyle.Body;
            }
        }

        public static string FormatLine(string line, string targetPrefix)
        {
            if (line == null) line = string.Empty;

            // Preserve line ending
            string lineEnding = string.Empty;
            if (line.EndsWith("\r\n"))
            {
                lineEnding = "\r\n";
                line = line.Substring(0, line.Length - 2);
            }
            else if (line.EndsWith("\r") || line.EndsWith("\n"))
            {
                lineEnding = line.Substring(line.Length - 1);
                line = line.Substring(0, line.Length - 1);
            }

            var currentStyle = DetectStyle(line);
            var targetStyle = GetStyleForPrefix(targetPrefix);

            // Toggle behavior: If the line is ALREADY formatted with the requested heading style,
            // or if the target is Body, remove the heading prefix to convert back to Body!
            if (targetStyle == MarkdownHeadingStyle.Body || currentStyle == targetStyle)
            {
                var stripMatch = HeadingStripRegex.Match(line);
                if (stripMatch.Success)
                {
                    string indent = stripMatch.Groups[1].Value;
                    string content = stripMatch.Groups[2].Value;
                    return indent + content + lineEnding;
                }
                return line + lineEnding;
            }

            // Otherwise, apply the new heading prefix
            var match = HeadingStripRegex.Match(line);
            if (match.Success)
            {
                string indent = match.Groups[1].Value;
                string content = match.Groups[2].Value;
                return indent + targetPrefix + content + lineEnding;
            }
            else
            {
                // Line does not have a heading prefix
                string trimmed = line.TrimStart();
                int leadingSpaces = line.Length - trimmed.Length;
                string indent = leadingSpaces > 0 ? line.Substring(0, leadingSpaces) : string.Empty;
                return indent + targetPrefix + trimmed + lineEnding;
            }
        }

        public static string FormatLines(string block, string targetPrefix)
        {
            if (string.IsNullOrEmpty(block))
            {
                return targetPrefix;
            }

            var lines = block.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string delimiter = block.Contains("\r\n") ? "\r\n" : (block.Contains("\r") ? "\r" : "\n");

            var formatted = new List<string>(lines.Length);

            // Check if ALL non-empty lines already have the target style (for toggle)
            var targetStyle = GetStyleForPrefix(targetPrefix);
            bool allHaveTarget = true;
            int nonEmptyCount = 0;

            foreach (var l in lines)
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    nonEmptyCount++;
                    if (DetectStyle(l) != targetStyle)
                    {
                        allHaveTarget = false;
                        break;
                    }
                }
            }

            // If all already have target style, toggle all to Body
            string effectivePrefix = (allHaveTarget && nonEmptyCount > 0) ? PrefixBody : targetPrefix;

            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l))
                {
                    formatted.Add(l);
                }
                else
                {
                    formatted.Add(FormatLine(l, effectivePrefix));
                }
            }

            return string.Join(delimiter, formatted);
        }
    }

    public static class MarkdownInlineHelper
    {
        public static (int replaceStart, int replaceEnd, string replacementText, int cursorOffset) FormatInline(
            string fullDocument,
            int selStart,
            int selEnd,
            string delimiter)
        {
            if (fullDocument == null) fullDocument = string.Empty;
            if (selStart < 0) selStart = 0;
            if (selStart > fullDocument.Length) selStart = fullDocument.Length;
            if (selEnd < selStart) selEnd = selStart;
            if (selEnd > fullDocument.Length) selEnd = fullDocument.Length;

            int delLen = delimiter.Length;

            // Scenario 1: Empty selection (Cursor placed at a point)
            if (selStart == selEnd)
            {
                // Check if cursor is directly between delimiters, e.g. **|**
                if (selStart >= delLen && selStart + delLen <= fullDocument.Length &&
                    fullDocument.Substring(selStart - delLen, delLen) == delimiter &&
                    fullDocument.Substring(selStart, delLen) == delimiter)
                {
                    // Remove the delimiters
                    return (selStart - delLen, selStart + delLen, string.Empty, 0);
                }

                // Expand to word boundaries under the cursor
                int wordStart = selStart;
                while (wordStart > 0 && Notepads.Extensions.StringExtensions.IsWordCharacter(fullDocument[wordStart - 1]))
                {
                    wordStart--;
                }

                int wordEnd = selStart;
                while (wordEnd < fullDocument.Length && Notepads.Extensions.StringExtensions.IsWordCharacter(fullDocument[wordEnd]))
                {
                    wordEnd++;
                }

                // If not in a word (e.g. on whitespace or empty line)
                if (wordStart == wordEnd)
                {
                    // Insert delimiter pair and place cursor between them
                    return (selStart, selStart, delimiter + delimiter, delLen);
                }

                // Check if the word is already surrounded by delimiter
                if (wordStart >= delLen && wordEnd + delLen <= fullDocument.Length &&
                    fullDocument.Substring(wordStart - delLen, delLen) == delimiter &&
                    fullDocument.Substring(wordEnd, delLen) == delimiter)
                {
                    // Toggle off: remove delimiters around word
                    string word = fullDocument.Substring(wordStart, wordEnd - wordStart);
                    int relCursor = Math.Max(0, Math.Min(selStart - wordStart, word.Length));
                    return (wordStart - delLen, wordEnd + delLen, word, relCursor);
                }
                else
                {
                    // Wrap the word with delimiters
                    string word = fullDocument.Substring(wordStart, wordEnd - wordStart);
                    int relCursor = Math.Max(0, Math.Min(selStart - wordStart, word.Length));
                    return (wordStart, wordEnd, delimiter + word + delimiter, delLen + relCursor);
                }
            }

            // Scenario 2: User selected text
            string selectedText = fullDocument.Substring(selStart, selEnd - selStart);

            // Case A: Selection itself starts and ends with delimiter
            if (selectedText.Length >= delLen * 2 && selectedText.StartsWith(delimiter) && selectedText.EndsWith(delimiter))
            {
                string unwrapped = selectedText.Substring(delLen, selectedText.Length - delLen * 2);
                return (selStart, selEnd, unwrapped, unwrapped.Length);
            }

            // Case B: Delimiters are just outside the selection range
            if (selStart >= delLen && selEnd + delLen <= fullDocument.Length &&
                fullDocument.Substring(selStart - delLen, delLen) == delimiter &&
                fullDocument.Substring(selEnd, delLen) == delimiter)
            {
                return (selStart - delLen, selEnd + delLen, selectedText, selectedText.Length);
            }

            // Case C: Selection has leading or trailing whitespace
            int trimStart = 0;
            while (trimStart < selectedText.Length && char.IsWhiteSpace(selectedText[trimStart]))
            {
                trimStart++;
            }

            int trimEnd = selectedText.Length;
            while (trimEnd > trimStart && char.IsWhiteSpace(selectedText[trimEnd - 1]))
            {
                trimEnd--;
            }

            string leading = selectedText.Substring(0, trimStart);
            string trailing = selectedText.Substring(trimEnd);
            string core = selectedText.Substring(trimStart, trimEnd - trimStart);

            if (string.IsNullOrEmpty(core))
            {
                return (selStart, selEnd, selectedText, selectedText.Length);
            }

            // Check if core is already wrapped with delimiter
            if (core.Length >= delLen * 2 && core.StartsWith(delimiter) && core.EndsWith(delimiter))
            {
                string unwrapped = core.Substring(delLen, core.Length - delLen * 2);
                string res = leading + unwrapped + trailing;
                return (selStart, selEnd, res, res.Length);
            }

            string wrapped = leading + delimiter + core + delimiter + trailing;
            return (selStart, selEnd, wrapped, wrapped.Length);
        }
    }
}
