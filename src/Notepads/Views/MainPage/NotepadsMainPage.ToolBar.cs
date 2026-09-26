// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2026, Notepads contributors. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System;
    using System.Threading.Tasks;
    using Notepads.Controls.TextEditor;
    using Notepads.Services;
    using Notepads.Utilities;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;

    public sealed partial class NotepadsMainPage
    {
        private void InitializeTopToolBar()
        {
            UpdateToolBarState();
        }

        private void UpdateToolBarState()
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            bool hasEditor = editor != null;
            bool isEditing = hasEditor && editor.Mode == TextEditorMode.Editing;

            if (ToolBarMenuSaveItem != null) ToolBarMenuSaveItem.IsEnabled = hasEditor && editor.IsModified;
            if (ToolBarMenuSaveAsItem != null) ToolBarMenuSaveAsItem.IsEnabled = hasEditor;
            if (ToolBarMenuPrintItem != null) ToolBarMenuPrintItem.IsEnabled = hasEditor && !string.IsNullOrEmpty(editor.GetText());
            if (ToolBarMenuCloseTabItem != null) ToolBarMenuCloseTabItem.IsEnabled = hasEditor;

            if (ToolBarMenuUndoItem != null) ToolBarMenuUndoItem.IsEnabled = isEditing;
            if (ToolBarMenuCutItem != null) ToolBarMenuCutItem.IsEnabled = isEditing;
            if (ToolBarMenuCopyItem != null) ToolBarMenuCopyItem.IsEnabled = hasEditor;
            if (ToolBarMenuPasteItem != null) ToolBarMenuPasteItem.IsEnabled = isEditing;
            if (ToolBarMenuDeleteItem != null) ToolBarMenuDeleteItem.IsEnabled = isEditing;
            if (ToolBarMenuSelectAllItem != null) ToolBarMenuSelectAllItem.IsEnabled = hasEditor;
            if (ToolBarMenuFindItem != null) ToolBarMenuFindItem.IsEnabled = hasEditor;
            if (ToolBarMenuReplaceItem != null) ToolBarMenuReplaceItem.IsEnabled = isEditing;
            if (ToolBarMenuGoToItem != null) ToolBarMenuGoToItem.IsEnabled = isEditing;

            if (ToolBarHeadingButton != null) ToolBarHeadingButton.IsEnabled = isEditing;
            if (ToolBarListButton != null) ToolBarListButton.IsEnabled = isEditing;
            if (ToolBarBoldButton != null) ToolBarBoldButton.IsEnabled = isEditing;
            if (ToolBarItalicButton != null) ToolBarItalicButton.IsEnabled = isEditing;
            if (ToolBarStrikethroughButton != null) ToolBarStrikethroughButton.IsEnabled = isEditing;
            if (ToolBarLinkButton != null) ToolBarLinkButton.IsEnabled = isEditing;
            if (ToolBarTableButton != null) ToolBarTableButton.IsEnabled = isEditing;
            if (ToolBarClearFormatButton != null) ToolBarClearFormatButton.IsEnabled = isEditing;
            if (ToolBarPreviewButton != null) ToolBarPreviewButton.IsEnabled = hasEditor;

            if (ToolBarMenuWordWrapToggleItem != null && editor != null)
            {
                ToolBarMenuWordWrapToggleItem.IsChecked = editor.IsWordWrap();
            }
            if (ToolBarMenuStatusBarToggleItem != null)
            {
                ToolBarMenuStatusBarToggleItem.IsChecked = AppSettingsService.ShowStatusBar;
            }

            UpdateActiveFormattingUI();
        }

        // ==================== ไฟล์ (File) Menu Handlers ====================

        private void ToolBarMenuNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.OpenNewTextEditor(_defaultNewFileName);
        }

        private async void ToolBarMenuNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            await OpenNewAppInstanceAsync();
        }

        private void ToolBarMenuNewMarkdownTabItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.OpenNewTextEditor("Untitled.md");
        }

        private async void ToolBarMenuOpenItem_Click(object sender, RoutedEventArgs e)
        {
            await OpenNewFilesAsync();
        }

        private async void ToolBarMenuSaveItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) await SaveAsync(editor, saveAs: false);
        }

        private async void ToolBarMenuSaveAsItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) await SaveAsync(editor, saveAs: true);
        }

        private async void ToolBarMenuSaveAllItem_Click(object sender, RoutedEventArgs e)
        {
            await SaveAllAsync(NotepadsCore.GetAllTextEditors());
        }

        private async void ToolBarMenuPageSetupItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) await PrintAsync(editor);
        }

        private async void ToolBarMenuPrintItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) await PrintAsync(editor);
        }

        private void ToolBarMenuCloseTabItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) NotepadsCore.CloseTextEditor(editor);
        }

        private void ToolBarMenuCloseWindowItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void ToolBarMenuExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        // ==================== แก้ไข (Edit) Menu Handlers ====================

        private void ToolBarMenuUndoItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Undo();
        }

        private void ToolBarMenuCutItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Cut();
        }

        private void ToolBarMenuCopyItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Copy();
        }

        private void ToolBarMenuPasteItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Paste();
        }

        private void ToolBarMenuDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Delete();
        }

        private void ToolBarMenuClearFormattingItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ClearFormatting();
        }

        private async void ToolBarMenuSearchWithBingItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null)
            {
                var query = editor.GetContentForSharing();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var uri = new Uri($"https://www.bing.com/search?q={Uri.EscapeDataString(query.Trim())}");
                    await Launcher.LaunchUriAsync(uri);
                }
            }
        }

        private void ToolBarMenuFindItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowFindAndReplaceControl(showReplaceBar: false);
        }

        private void ToolBarMenuFindNextItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowFindAndReplaceControl(showReplaceBar: false);
        }

        private void ToolBarMenuFindPrevItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowFindAndReplaceControl(showReplaceBar: false);
        }

        private void ToolBarMenuReplaceItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowFindAndReplaceControl(showReplaceBar: true);
        }

        private void ToolBarMenuGoToItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowGoToControl();
        }

        private void ToolBarMenuSelectAllItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.SelectAll();
        }

        private void ToolBarMenuDateTimeItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.InsertDateTime();
        }

        private void ToolBarMenuFontSettingsItem_Click(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = true;
        }

        // ==================== มุมมอง (View) Menu Handlers ====================

        private void ToolBarMenuZoomInItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) editor.SetFontZoomFactor(editor.GetFontZoomFactor() + 10);
        }

        private void ToolBarMenuZoomOutItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            if (editor != null) editor.SetFontZoomFactor(Math.Max(10, editor.GetFontZoomFactor() - 10));
        }

        private void ToolBarMenuRestoreZoomItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.SetFontZoomFactor(100);
        }

        private void ToolBarMenuStatusBarToggleItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem item)
            {
                AppSettingsService.ShowStatusBar = item.IsChecked;
            }
        }

        private void ToolBarMenuWordWrapToggleItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ToggleWordWrap();
        }

        private void ToolBarMenuToggleMarkdownPreviewItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowHideContentPreview();
        }

        // ==================== Markdown Formatting Toolbar Handlers ====================

        private void HeadingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string prefix = null;
            if (sender is Button btn && btn.Tag is string btnPrefix)
            {
                prefix = btnPrefix;
            }
            else if (sender is MenuFlyoutItem item && item.Tag is string itemPrefix)
            {
                prefix = itemPrefix;
            }

            if (prefix != null)
            {
                var editor = NotepadsCore.GetSelectedTextEditor();
                editor?.FormatHeading(prefix);
                ToolBarHeadingFlyout?.Hide();
                UpdateActiveFormattingUI();
            }
        }

        private void UpdateActiveHeadingUI()
        {
            UpdateActiveFormattingUI();
        }

        private void UpdateActiveFormattingUI()
        {
            try
            {
                var editor = NotepadsCore.GetSelectedTextEditor();
                var style = editor != null ? editor.GetCurrentLineHeadingStyle() : Notepads.Utilities.MarkdownHeadingStyle.Body;

                if (ToolBarHeadingButtonText != null)
                {
                    ToolBarHeadingButtonText.Text = Notepads.Utilities.MarkdownHeadingHelper.GetDisplayName(style);
                }

                SetHeadingItemState(HeadingItem_Title, HeadingIndicator_Title, style == Notepads.Utilities.MarkdownHeadingStyle.Title);
                SetHeadingItemState(HeadingItem_Subtitle, HeadingIndicator_Subtitle, style == Notepads.Utilities.MarkdownHeadingStyle.Subtitle);
                SetHeadingItemState(HeadingItem_H1, HeadingIndicator_H1, style == Notepads.Utilities.MarkdownHeadingStyle.Heading1);
                SetHeadingItemState(HeadingItem_H2, HeadingIndicator_H2, style == Notepads.Utilities.MarkdownHeadingStyle.Heading2);
                SetHeadingItemState(HeadingItem_H3, HeadingIndicator_H3, style == Notepads.Utilities.MarkdownHeadingStyle.Heading3);
                SetHeadingItemState(HeadingItem_H4, HeadingIndicator_H4, style == Notepads.Utilities.MarkdownHeadingStyle.Heading4);
                SetHeadingItemState(HeadingItem_Body, HeadingIndicator_Body, style == Notepads.Utilities.MarkdownHeadingStyle.Body);

                if (ToolBarBoldButton != null)
                {
                    bool isBold = editor != null && editor.IsBold();
                    ToolBarBoldButton.Background = isBold
                        ? (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlHighlightListLowBrush"]
                        : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
                }

                if (ToolBarItalicButton != null)
                {
                    bool isItalic = editor != null && editor.IsItalic();
                    ToolBarItalicButton.Background = isItalic
                        ? (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlHighlightListLowBrush"]
                        : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
                }

                if (ToolBarStrikethroughButton != null)
                {
                    bool isStrikethrough = editor != null && editor.IsStrikethrough();
                    ToolBarStrikethroughButton.Background = isStrikethrough
                        ? (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlHighlightListLowBrush"]
                        : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
                }
            }
            catch { }
        }

        private void SetHeadingItemState(Button item, Border indicator, bool isActive)
        {
            if (indicator != null)
            {
                indicator.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            }
            if (item != null)
            {
                if (isActive)
                {
                    item.Background = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlHighlightListLowBrush"];
                }
                else
                {
                    item.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
                }
            }
        }

        private void BulletListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.FormatLinePrefix("- ");
        }

        private void NumberedListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.FormatLinePrefix("1. ");
        }

        private void IndentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Indent();
        }

        private void UnindentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.Unindent();
        }

        private void ToolBarBoldButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            editor?.FormatBold();
            UpdateActiveFormattingUI();
        }

        private void ToolBarItalicButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            editor?.FormatItalic();
            UpdateActiveFormattingUI();
        }

        private void ToolBarStrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = NotepadsCore.GetSelectedTextEditor();
            editor?.FormatStrikethrough();
            UpdateActiveFormattingUI();
        }

        private void ToolBarLinkButton_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.WrapSelection("[", "](https://)");
        }

        private void ToolBarTableGridCell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string sizeStr)
            {
                var parts = sizeStr.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int rows) && int.TryParse(parts[1], out int cols))
                {
                    NotepadsCore.GetSelectedTextEditor()?.InsertTable(rows, cols);
                    ToolBarTableButton?.Flyout?.Hide();
                }
            }
        }

        private void ToolBarInsertTableDefault_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.InsertTable(3, 3);
            ToolBarTableButton?.Flyout?.Hide();
        }

        private void ToolBarClearFormatButton_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ClearFormatting();
        }

        private void ToolBarPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            NotepadsCore.GetSelectedTextEditor()?.ShowHideContentPreview();
        }

        private void ToolBarSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = true;
        }
    }
}
