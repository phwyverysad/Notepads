// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Controls.TextEditor
{
    using System;
    using System.Text.RegularExpressions;
    using Windows.UI.Text;
    using Notepads.Controls.FindAndReplace;
    using Notepads.Extensions;

    public partial class TextEditorCore
    {
        public string GetSearchString()
        {
            var searchString = Document.Selection.Text.Trim();

            if (searchString.Contains(RichEditBoxDefaultLineEnding.ToString())) return string.Empty;

            var document = GetText();

            if (string.IsNullOrEmpty(searchString) && Document.Selection.StartPosition < document.Length)
            {
                var startIndex = Document.Selection.StartPosition;
                var endIndex = startIndex;

                for (; startIndex >= 0; startIndex--)
                {
                    if (!StringExtensions.IsWordCharacter(document[startIndex]))
                        break;
                }

                for (; endIndex < document.Length; endIndex++)
                {
                    if (!StringExtensions.IsWordCharacter(document[endIndex]))
                        break;
                }

                if (startIndex != endIndex) return document.Substring(startIndex + 1, endIndex - startIndex - 1);
            }

            return searchString;
        }

        public bool TryFindNextAndSelect(SearchContext searchContext, bool stopAtEof, out bool regexError)
        {
            regexError = false;

            if (string.IsNullOrEmpty(searchContext.SearchText))
            {
                return false;
            }

            var document = GetText();

            if (Document.Selection.EndPosition > document.Length) Document.Selection.EndPosition = document.Length;

            if (searchContext.UseRegex)
            {
                return TryFindNextAndSelectUsingRegex(document, searchContext, stopAtEof, out regexError);
            }
            else
            {
                StringComparison comparison = searchContext.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                var index = searchContext.MatchWholeWord
                    ? document.IndexOfWholeWord(searchContext.SearchText, Document.Selection.EndPosition, comparison)
                    : document.IndexOf(searchContext.SearchText, Document.Selection.EndPosition, comparison);

                if (index != -1)
                {
                    Document.Selection.StartPosition = index;
                    Document.Selection.EndPosition = index + searchContext.SearchText.Length;
                    Document.Selection.ScrollIntoView(PointOptions.None);
                }
                else
                {
                    if (!stopAtEof)
                    {
                        index = searchContext.MatchWholeWord
                            ? document.IndexOfWholeWord(searchContext.SearchText, 0, comparison)
                            : document.IndexOf(searchContext.SearchText, 0, comparison);

                        if (index != -1)
                        {
                            Document.Selection.StartPosition = index;
                            Document.Selection.EndPosition = index + searchContext.SearchText.Length;
                            Document.Selection.ScrollIntoView(PointOptions.None);
                        }
                    }
                }

                if (index == -1)
                {
                    Document.Selection.StartPosition = Document.Selection.EndPosition;
                    return false;
                }

                return true;
            }
        }

        public bool TryFindPreviousAndSelect(SearchContext searchContext, bool stopAtBof, out bool regexError)
        {
            regexError = false;

            if (string.IsNullOrEmpty(searchContext.SearchText))
            {
                return false;
            }

            var document = GetText();

            if (searchContext.UseRegex)
            {
                return TryFindPreviousAndSelectUsingRegex(document, searchContext, out regexError, stopAtBof);
            }
            else
            {
                var searchIndex = Document.Selection.StartPosition - 1;
                if (searchIndex < 0)
                {
                    if (stopAtBof)
                    {
                        return false;
                    }
                    else
                    {
                        searchIndex = document.Length - 1;
                    }
                }

                StringComparison comparison = searchContext.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                var index = searchContext.MatchWholeWord
                    ? document.LastIndexOfWholeWord(searchContext.SearchText, searchIndex, comparison)
                    : document.LastIndexOf(searchContext.SearchText, searchIndex, comparison);

                if (index != -1)
                {
                    Document.Selection.StartPosition = index;
                    Document.Selection.EndPosition = index + searchContext.SearchText.Length;
                    Document.Selection.ScrollIntoView(PointOptions.None);
                }
                else
                {
                    index = searchContext.MatchWholeWord
                        ? document.LastIndexOfWholeWord(searchContext.SearchText, document.Length - 1, comparison)
                        : document.LastIndexOf(searchContext.SearchText, document.Length - 1, comparison);

                    if (index != -1)
                    {
                        Document.Selection.StartPosition = index;
                        Document.Selection.EndPosition = index + searchContext.SearchText.Length;
                        Document.Selection.ScrollIntoView(PointOptions.None);
                    }
                }

                if (index == -1)
                {
                    Document.Selection.StartPosition = Document.Selection.EndPosition;
                    return false;
                }

                return true;
            }
        }

        public bool TryFindNextAndReplace(SearchContext searchContext, string replaceText, out bool regexError)
        {
            Document.Selection.EndPosition = Document.Selection.StartPosition;

            if (TryFindNextAndSelect(searchContext, stopAtEof: true, out regexError))
            {
                if (searchContext.UseRegex)
                {
                    replaceText = ApplyTabAndLineEndingFix(replaceText);
                }

                Document.Selection.SetText(TextSetOptions.None, replaceText);
                TryFindNextAndSelect(searchContext, stopAtEof: true, out _);
                return true;
            }

            return false;
        }

        public bool TryFindPreviousAndReplace(SearchContext searchContext, string replaceText, out bool regexError)
        {
            Document.Selection.StartPosition = Document.Selection.EndPosition;

            if (TryFindPreviousAndSelect(searchContext, stopAtBof: true, out regexError))
            {
                if (searchContext.UseRegex)
                {
                    replaceText = ApplyTabAndLineEndingFix(replaceText);
                }

                Document.Selection.SetText(TextSetOptions.None, replaceText);
                TryFindPreviousAndSelect(searchContext, stopAtBof: true, out _);
                return true;
            }

            return false;
        }

        public (int currentMatchIndex, int totalMatches) GetSearchMatchesCount(SearchContext searchContext)
        {
            if (string.IsNullOrEmpty(searchContext.SearchText))
            {
                return (0, 0);
            }

            var document = GetText();
            if (string.IsNullOrEmpty(document))
            {
                return (0, 0);
            }

            var matchIndices = new System.Collections.Generic.List<int>();

            if (searchContext.UseRegex)
            {
                try
                {
                    string content = document.Replace(RichEditBoxDefaultLineEnding, RegexDefaultLineEnding);
                    Regex regex = new Regex(searchContext.SearchText,
                        RegexOptions.Multiline | (searchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));

                    var matches = regex.Matches(content);
                    foreach (Match m in matches)
                    {
                        matchIndices.Add(m.Index);
                    }
                }
                catch
                {
                    return (0, 0);
                }
            }
            else
            {
                StringComparison comparison = searchContext.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int pos = 0;
                int step = Math.Max(1, searchContext.SearchText.Length);

                while (pos < document.Length)
                {
                    int found = searchContext.MatchWholeWord
                        ? document.IndexOfWholeWord(searchContext.SearchText, pos, comparison)
                        : document.IndexOf(searchContext.SearchText, pos, comparison);

                    if (found == -1) break;
                    matchIndices.Add(found);
                    pos = found + step;
                }
            }

            int total = matchIndices.Count;
            if (total == 0) return (0, 0);

            int currentSel = Document.Selection.StartPosition;
            int currentIdx = 0;

            for (int i = 0; i < matchIndices.Count; i++)
            {
                if (matchIndices[i] == currentSel)
                {
                    currentIdx = i + 1;
                    break;
                }
            }

            if (currentIdx == 0)
            {
                for (int i = 0; i < matchIndices.Count; i++)
                {
                    if (matchIndices[i] >= currentSel)
                    {
                        currentIdx = i + 1;
                        break;
                    }
                }
                if (currentIdx == 0) currentIdx = 1;
            }

            return (currentIdx, total);
        }

        public bool TryFindAndReplaceAll(SearchContext searchContext, string replaceText, out bool regexError)
        {
            return TryFindAndReplaceAll(searchContext, replaceText, out _, out regexError);
        }

        public bool TryFindAndReplaceAll(SearchContext searchContext, string replaceText, out int replacedCount, out bool regexError)
        {
            regexError = false;
            replacedCount = 0;
            var found = false;
            var text = GetText();

            if (string.IsNullOrEmpty(searchContext.SearchText))
            {
                return false;
            }

            if (searchContext.UseRegex)
            {
                try
                {
                    string content = text.Replace(RichEditBoxDefaultLineEnding, RegexDefaultLineEnding);
                    Regex regex = new Regex(searchContext.SearchText, RegexOptions.Multiline | (searchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));
                    var matches = regex.Matches(content);
                    if (matches.Count > 0)
                    {
                        found = true;
                        replacedCount = matches.Count;
                        if (searchContext.UseRegex)
                        {
                            replaceText = ApplyTabAndLineEndingFix(replaceText);
                        }
                        text = regex.Replace(content, replaceText).Replace(RegexDefaultLineEnding, RichEditBoxDefaultLineEnding);
                    }
                }
                catch (Exception)
                {
                    regexError = true;
                    return false;
                }
            }
            else
            {
                var pos = 0;
                var searchTextLength = searchContext.SearchText.Length;
                var replaceTextLength = replaceText.Length;

                StringComparison comparison = searchContext.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                pos = searchContext.MatchWholeWord
                    ? text.IndexOfWholeWord(searchContext.SearchText, pos, comparison)
                    : text.IndexOf(searchContext.SearchText, pos, comparison);

                while (pos != -1)
                {
                    found = true;
                    replacedCount++;
                    text = text.Remove(pos, searchTextLength).Insert(pos, replaceText);
                    pos += replaceTextLength;
                    pos = searchContext.MatchWholeWord
                        ? text.IndexOfWholeWord(searchContext.SearchText, pos, comparison)
                        : text.IndexOf(searchContext.SearchText, pos, comparison);
                }
            }

            if (found)
            {
                SetText(text);
                Document.Selection.StartPosition = int.MaxValue;
                Document.Selection.EndPosition = Document.Selection.StartPosition;
            }

            return found;
        }

        private bool TryFindNextAndSelectUsingRegex(string content, SearchContext searchContext, bool stopAtEof,
            out bool regexError)
        {
            try
            {
                regexError = false;
                content = content.Replace(RichEditBoxDefaultLineEnding, RegexDefaultLineEnding);
                Regex regex = new Regex(searchContext.SearchText,
                    RegexOptions.Multiline | (searchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));

                var match = regex.Match(content, Document.Selection.EndPosition);

                if (!match.Success && !stopAtEof)
                {
                    match = regex.Match(content, 0);
                }

                if (match.Success)
                {
                    var index = match.Index;
                    Document.Selection.StartPosition = index;
                    Document.Selection.EndPosition = index + match.Length;
                    Document.Selection.ScrollIntoView(PointOptions.None);
                    return true;
                }
                else
                {
                    Document.Selection.StartPosition = Document.Selection.EndPosition;
                    return false;
                }
            }
            catch (Exception)
            {
                regexError = true;
                return false;
            }
        }

        private bool TryFindPreviousAndSelectUsingRegex(string content, SearchContext searchContext, out bool regexError, bool stopAtBof)
        {
            try
            {
                regexError = false;
                content = content.Replace(RichEditBoxDefaultLineEnding, RegexDefaultLineEnding);
                Regex regex = new Regex(searchContext.SearchText,
                    RegexOptions.RightToLeft | RegexOptions.Multiline |
                    (searchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));

                var match = regex.Match(content, Document.Selection.StartPosition);

                if (!match.Success && !stopAtBof)
                {
                    match = regex.Match(content, content.Length);
                }

                if (match.Success)
                {
                    var index = match.Index;
                    Document.Selection.StartPosition = index;
                    Document.Selection.EndPosition = index + match.Length;
                    Document.Selection.ScrollIntoView(PointOptions.None);
                    return true;
                }
                else
                {
                    Document.Selection.StartPosition = Document.Selection.EndPosition;
                    return false;
                }
            }
            catch (Exception)
            {
                regexError = true;
                return false;
            }
        }

        private static bool TryFindAndReplaceAllUsingRegex(string content, SearchContext searchContext, string replaceText, out bool regexError, out string output)
        {
            regexError = false;
            output = string.Empty;
            content = content.Replace(RichEditBoxDefaultLineEnding, RegexDefaultLineEnding);

            try
            {
                Regex regex = new Regex(searchContext.SearchText, RegexOptions.Multiline | (searchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase));

                if (regex.IsMatch(content))
                {
                    if (searchContext.UseRegex)
                    {
                        replaceText = ApplyTabAndLineEndingFix(replaceText);
                    }

                    output = regex.Replace(content, replaceText).Replace(RegexDefaultLineEnding, RichEditBoxDefaultLineEnding);
                    return true;
                }
            }
            catch (Exception)
            {
                regexError = true;
                return false;
            }

            return false;
        }

        private static string ApplyTabAndLineEndingFix(string text)
        {
            return text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
        }
    }
}