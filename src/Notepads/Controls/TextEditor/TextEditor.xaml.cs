namespace Notepads.Controls.TextEditor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Notepads.Commands;
    using Notepads.Controls.FindAndReplace;
    using Notepads.Controls.GoTo;
    using Notepads.Extensions;
    using Notepads.Models;
    using Notepads.Services;
    using Notepads.Utilities;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.ApplicationModel.Resources;
    using Windows.Storage;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Text;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Input;

    public enum TextEditorMode
    {
        Editing = 0,
        DiffPreview
    }

    public enum FileModificationState
    {
        Untouched,
        Modified,
        RenamedMovedOrDeleted
    }

    public sealed partial class TextEditor : ITextEditor, IDisposable
    {
        public new event RoutedEventHandler Loaded;
        public new event RoutedEventHandler Unloaded;
        public new event KeyEventHandler KeyDown;
        public event EventHandler ModeChanged;
        public event EventHandler ModificationStateChanged;
        public event EventHandler FileModificationStateChanged;
        public event EventHandler LineEndingChanged;
        public event EventHandler EncodingChanged;
        public event EventHandler TextChanging;
        public event EventHandler ChangeReverted;
        public event EventHandler SelectionChanged;
        public event EventHandler FontZoomFactorChanged;
        public event EventHandler FileSaved;
        public event EventHandler FileReloaded;
        public event EventHandler FileRenamed;

        public Guid Id { get; set; }

        public INotepadsExtensionProvider ExtensionProvider;

        public string FileNamePlaceholder { get; set; } = string.Empty;

        public FileType FileType { get; private set; }

        public TextFile LastSavedSnapshot { get; private set; }

        public LineEnding? RequestedLineEnding { get; private set; }

        public Encoding RequestedEncoding { get; private set; }

        public string EditingFileName { get; private set; }

        public string EditingFilePath { get; private set; }

        private StorageFile _editingFile;

        public StorageFile EditingFile
        {
            get => _editingFile;
            private set
            {
                _editingFile = value;
                UpdateDocumentInfo();
            }
        }

        private void UpdateDocumentInfo()
        {
            if (EditingFile == null)
            {
                EditingFileName = null;
                EditingFilePath = null;
                FileType = FileTypeUtility.GetFileTypeByFileName(FileNamePlaceholder);
            }
            else
            {
                EditingFileName = EditingFile.Name;
                EditingFilePath = EditingFile.Path;
                FileType = FileTypeUtility.GetFileTypeByFileName(EditingFile.Name);
            }

            // Hide content preview if current file type is not supported for previewing
            if (!FileTypeUtility.IsPreviewSupported(FileType))
            {
                if (SplitPanel != null && SplitPanel.Visibility == Visibility.Visible)
                {
                    ShowHideContentPreview();
                }
            }
        }

        private bool _isModified;

        public bool IsModified
        {
            get => _isModified;
            private set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    ModificationStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public FileModificationState FileModificationState
        {
            get => _fileModificationState;
            private set
            {
                if (_fileModificationState != value)
                {
                    _fileModificationState = value;
                    FileModificationStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _loaded;

        private FileModificationState _fileModificationState;

        private bool _isContentPreviewPanelOpened;

        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        private CancellationTokenSource _fileStatusCheckerCancellationTokenSource;

        private readonly int _fileStatusCheckerPollingRateInSec = 6;

        private readonly double _fileStatusCheckerDelayInSec = 0.3;

        private readonly SemaphoreSlim _fileStatusSemaphoreSlim = new SemaphoreSlim(1, 1);

        private TextEditorMode _mode = TextEditorMode.Editing;

        private readonly ICommandHandler<KeyRoutedEventArgs> _keyboardCommandHandler;

        private IContentPreviewExtension _contentPreviewExtension;

        private SearchContext _lastSearchContext = new SearchContext(string.Empty);

        public TextEditorMode Mode
        {
            get => _mode;
            private set
            {
                if (_mode != value)
                {
                    _mode = value;
                    ModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool DisplayLineNumbers
        {
            get => TextEditorCore.DisplayLineNumbers;
            set => TextEditorCore.DisplayLineNumbers = value;
        }

        public bool DisplayLineHighlighter
        {
            get => TextEditorCore.DisplayLineHighlighter;
            set => TextEditorCore.DisplayLineHighlighter = value;
        }

        public TextEditor()
        {
            InitializeComponent();

            TextEditorCore.TextChanging += TextEditorCore_OnTextChanging;
            TextEditorCore.SelectionChanged += TextEditorCore_OnSelectionChanged;
            TextEditorCore.KeyDown += TextEditorCore_OnKeyDown;
            TextEditorCore.CopyTextToWindowsClipboardRequested += TextEditorCore_CopyTextToWindowsClipboardRequested;
            TextEditorCore.CutSelectedTextToWindowsClipboardRequested += TextEditorCore_CutSelectedTextToWindowsClipboardRequested;
            TextEditorCore.ContextFlyout = new TextEditorContextFlyout(this, TextEditorCore);

            // Init shortcuts
            _keyboardCommandHandler = GetKeyboardCommandHandler();

            ThemeSettingsService.OnThemeChanged += ThemeSettingsService_OnThemeChanged;

            base.Loaded += TextEditor_Loaded;
            base.Unloaded += TextEditor_Unloaded;
            base.KeyDown += TextEditor_KeyDown;

            TextEditorCore.FontZoomFactorChanged += TextEditorCore_OnFontZoomFactorChanged;
            TextEditorCore.WrapSelectionRequested += TextEditorCore_OnWrapSelectionRequested;
        }

        private void TextEditorCore_OnWrapSelectionRequested(string prefix, string suffix)
        {
            WrapSelection(prefix, suffix);
        }

        private void TextEditor_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            KeyDown?.Invoke(this, e);
        }

        // Unhook events and clear state
        public void Dispose()
        {
            StopCheckingFileStatus();

            TextEditorCore.TextChanging -= TextEditorCore_OnTextChanging;
            TextEditorCore.SelectionChanged -= TextEditorCore_OnSelectionChanged;
            TextEditorCore.KeyDown -= TextEditorCore_OnKeyDown;
            TextEditorCore.CopyTextToWindowsClipboardRequested -= TextEditorCore_CopyTextToWindowsClipboardRequested;
            TextEditorCore.CutSelectedTextToWindowsClipboardRequested -= TextEditorCore_CutSelectedTextToWindowsClipboardRequested;
            TextEditorCore.WrapSelectionRequested -= TextEditorCore_OnWrapSelectionRequested;

            if (TextEditorCore.ContextFlyout is TextEditorContextFlyout contextFlyout)
            {
                contextFlyout.Dispose();
            }

            ThemeSettingsService.OnThemeChanged -= ThemeSettingsService_OnThemeChanged;

            Unloaded?.Invoke(this, new RoutedEventArgs());

            base.Loaded -= TextEditor_Loaded;
            base.Unloaded -= TextEditor_Unloaded;
            base.KeyDown -= TextEditor_KeyDown;

            TextEditorCore.FontZoomFactorChanged -= TextEditorCore_OnFontZoomFactorChanged;

            _contentPreviewExtension?.Dispose();

            if (SplitPanel != null)
            {
                SplitPanel.KeyDown -= SplitPanel_OnKeyDown;
                UnloadObject(SplitPanel);
            }

            if (SideBySideDiffViewer != null)
            {
                SideBySideDiffViewer.OnCloseEvent -= SideBySideDiffViewer_OnCloseEvent;
                SideBySideDiffViewer.Dispose();
                UnloadObject(SideBySideDiffViewer);
            }

            if (FindAndReplacePlaceholder != null && FindAndReplacePlaceholder.Content is FindAndReplaceControl findAndReplaceControl)
            {
                findAndReplaceControl.Dispose();
                UnloadObject(FindAndReplacePlaceholder);
            }

            if (GoToPlaceholder != null && GoToPlaceholder.Content is GoToControl goToControl)
            {
                goToControl.Dispose();
                UnloadObject(GoToPlaceholder);
            }

            if (GridSplitter != null)
            {
                UnloadObject(GridSplitter);
            }

            _fileStatusSemaphoreSlim.Dispose();
            TextEditorCore.Dispose();
        }

        private async void ThemeSettingsService_OnThemeChanged(object sender, ElementTheme theme)
        {
            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                if (Mode == TextEditorMode.DiffPreview)
                {
                    SideBySideDiffViewer.RenderDiff(LastSavedSnapshot.Content, TextEditorCore.GetText(), theme);
                    Task.Factory.StartNew(async () =>
                    {
                        await Dispatcher.CallOnUIThreadAsync(() => { SideBySideDiffViewer.Focus(); });
                    });
                }
            });
        }

        public async Task RenameAsync(string newFileName)
        {
            if (EditingFile == null)
            {
                FileNamePlaceholder = newFileName;
            }
            else
            {
                await EditingFile.RenameAsync(newFileName);
            }

            UpdateDocumentInfo();

            FileRenamed?.Invoke(this, EventArgs.Empty);
        }

        public string GetText()
        {
            return TextEditorCore.GetText();
        }

        // Make sure this method is thread safe
        public TextEditorStateMetaData GetTextEditorStateMetaData()
        {
            TextEditorCore.GetScrollViewerPosition(out var horizontalOffset, out var verticalOffset);
            TextEditorCore.GetTextSelectionPosition(out var textSelectionStartPosition, out var textSelectionEndPosition);

            var metaData = new TextEditorStateMetaData
            {
                FileNamePlaceholder = FileNamePlaceholder,
                LastSavedEncoding = EncodingUtility.GetEncodingName(LastSavedSnapshot.Encoding),
                LastSavedLineEnding = LineEndingUtility.GetLineEndingName(LastSavedSnapshot.LineEnding),
                DateModifiedFileTime = LastSavedSnapshot.DateModifiedFileTime,
                HasEditingFile = EditingFile != null,
                IsModified = IsModified,
                SelectionStartPosition = textSelectionStartPosition,
                SelectionEndPosition = textSelectionEndPosition,
                WrapWord = TextEditorCore.TextWrapping == TextWrapping.Wrap ||
                           TextEditorCore.TextWrapping == TextWrapping.WrapWholeWords,
                ScrollViewerHorizontalOffset = horizontalOffset,
                ScrollViewerVerticalOffset = verticalOffset,
                FontZoomFactor = TextEditorCore.GetFontZoomFactor() / 100,
                IsContentPreviewPanelOpened = _isContentPreviewPanelOpened,
                IsInDiffPreviewMode = (Mode == TextEditorMode.DiffPreview)
            };

            if (RequestedEncoding != null)
            {
                metaData.RequestedEncoding = EncodingUtility.GetEncodingName(RequestedEncoding);
            }

            if (RequestedLineEnding != null)
            {
                metaData.RequestedLineEnding = LineEndingUtility.GetLineEndingName(RequestedLineEnding.Value);
            }

            return metaData;
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded?.Invoke(this, e);

            StartCheckingFileStatusPeriodically();

            // Insert "Legacy Windows Notepad" style date and time if document starts with ".LOG"
            TextEditorCore.TryInsertNewLogEntry();
        }

        private void TextEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded?.Invoke(this, e);
            StopCheckingFileStatus();
        }

        public async void StartCheckingFileStatusPeriodically()
        {
            if (EditingFile == null) return;
            StopCheckingFileStatus();
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            _fileStatusCheckerCancellationTokenSource = cancellationTokenSource;

            try
            {
                await Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_fileStatusCheckerDelayInSec), cancellationToken);
                        LoggingService.LogInfo($"[{nameof(TextEditor)}] Checking file status for \"{EditingFile.Path}\".", consoleOnly: true);
                        await CheckAndUpdateFileStatusAsync(cancellationToken);
                        await Task.Delay(TimeSpan.FromSeconds(_fileStatusCheckerPollingRateInSec), cancellationToken);
                    }
                }, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[{nameof(TextEditor)}] Failed to check status for file [{EditingFile?.Path}]: {ex.Message}");
            }
        }

        public void StopCheckingFileStatus()
        {
            if (_fileStatusCheckerCancellationTokenSource?.IsCancellationRequested == false)
            {
                _fileStatusCheckerCancellationTokenSource.Cancel();
            }
        }

        private async Task CheckAndUpdateFileStatusAsync(CancellationToken cancellationToken)
        {
            if (EditingFile == null) return;

            await _fileStatusSemaphoreSlim.WaitAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                _fileStatusSemaphoreSlim.Release();
                return;
            }

            FileModificationState? newState = null;

            if (!await FileSystemUtility.FileExistsAsync(EditingFile))
            {
                newState = FileModificationState.RenamedMovedOrDeleted;
            }
            else
            {
                long fileModifiedTime = await FileSystemUtility.GetDateModifiedAsync(EditingFile);
                newState = fileModifiedTime != LastSavedSnapshot.DateModifiedFileTime ?
                    FileModificationState.Modified :
                    FileModificationState.Untouched;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _fileStatusSemaphoreSlim.Release();
                return;
            }

            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                FileModificationState = newState.Value;
            });

            _fileStatusSemaphoreSlim.Release();
        }

        private KeyboardCommandHandler GetKeyboardCommandHandler()
        {
            return new KeyboardCommandHandler(new List<IKeyboardCommand<KeyRoutedEventArgs>>
            {
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.F, (args) => ShowFindAndReplaceControl(showReplaceBar: false)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.F, (args) => ShowFindAndReplaceControl(showReplaceBar: true)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.H, (args) => ShowFindAndReplaceControl(showReplaceBar: true)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.G, (args) => ShowGoToControl()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.B, (args) => FormatBold()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.I, (args) => FormatItalic()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.X, (args) => FormatStrikethrough()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.K, (args) => WrapSelection("[", "](https://)")),
                new KeyboardCommand<KeyRoutedEventArgs>(false, true, false, VirtualKey.P, (args) => { if (FileTypeUtility.IsPreviewSupported(FileType)) ShowHideContentPreview(); }),
                new KeyboardCommand<KeyRoutedEventArgs>(false, true, false, VirtualKey.D, (args) => ShowHideSideBySideDiffViewer()),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F3, (args) =>
                    InitiateFindAndReplace(new FindAndReplaceEventArgs (_lastSearchContext, string.Empty, FindAndReplaceMode.FindOnly, SearchDirection.Next), out _)),
                new KeyboardCommand<KeyRoutedEventArgs>(false, false, true, VirtualKey.F3, (args) =>
                    InitiateFindAndReplace(new FindAndReplaceEventArgs (_lastSearchContext, string.Empty, FindAndReplaceMode.FindOnly, SearchDirection.Previous), out _)),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.Escape, (args) => { OnEscapeKeyDown(); }, shouldHandle: false, shouldSwallow: true)
            });
        }

        public void Init(TextFile textFile, StorageFile file, bool resetLastSavedSnapshot = true, bool clearUndoQueue = true, bool isModified = false, bool resetText = true)
        {
            _loaded = false;
            EditingFile = file;
            RequestedEncoding = null;
            RequestedLineEnding = null;
            if (resetText)
            {
                TextEditorCore.SetText(textFile.Content);
            }
            if (resetLastSavedSnapshot)
            {
                textFile.Content = TextEditorCore.GetText();
                LastSavedSnapshot = textFile;
            }
            if (clearUndoQueue)
            {
                TextEditorCore.ClearUndoQueue();
            }
            IsModified = isModified;
            _loaded = true;
        }

        public async Task ReloadFromEditingFileAsync(Encoding encoding = null)
        {
            if (EditingFile != null)
            {
                var textFile = await FileSystemUtility.ReadFileAsync(EditingFile, ignoreFileSizeLimit: false, encoding: encoding);
                Init(textFile, EditingFile, clearUndoQueue: false);
                LineEndingChanged?.Invoke(this, EventArgs.Empty);
                EncodingChanged?.Invoke(this, EventArgs.Empty);
                StartCheckingFileStatusPeriodically();
                CloseSideBySideDiffViewer();
                HideGoToControl();
                FileReloaded?.Invoke(this, EventArgs.Empty);
                AnalyticsService.TrackEvent(encoding == null ? "OnFileReloaded" : "OnFileReopenedWithEncoding");
            }
        }

        public void ResetEditorState(TextEditorStateMetaData metadata, string newText = null)
        {
            if (!string.IsNullOrEmpty(metadata.RequestedEncoding))
            {
                TryChangeEncoding(EncodingUtility.GetEncodingByName(metadata.RequestedEncoding));
            }

            if (!string.IsNullOrEmpty(metadata.RequestedLineEnding))
            {
                TryChangeLineEnding(LineEndingUtility.GetLineEndingByName(metadata.RequestedLineEnding));
            }

            if (newText != null)
            {
                TextEditorCore.SetText(newText);
            }

            TextEditorCore.TextWrapping = metadata.WrapWord ? TextWrapping.Wrap : TextWrapping.NoWrap;
            TextEditorCore.FontSize = metadata.FontZoomFactor * AppSettingsService.EditorFontSize;
            TextEditorCore.SetTextSelectionPosition(metadata.SelectionStartPosition, metadata.SelectionEndPosition);
            TextEditorCore.SetScrollViewerInitPosition(metadata.ScrollViewerHorizontalOffset, metadata.ScrollViewerVerticalOffset);
            TextEditorCore.ClearUndoQueue();
        }

        public void RevertAllChanges()
        {
            Init(LastSavedSnapshot, EditingFile, clearUndoQueue: false);
            ChangeReverted?.Invoke(this, EventArgs.Empty);
        }

        public bool TryChangeEncoding(Encoding encoding)
        {
            if (encoding == null) return false;

            if (!EncodingUtility.Equals(LastSavedSnapshot.Encoding, encoding))
            {
                RequestedEncoding = encoding;
                IsModified = true;
                EncodingChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            if (RequestedEncoding != null && EncodingUtility.Equals(LastSavedSnapshot.Encoding, encoding))
            {
                RequestedEncoding = null;
                IsModified = !NoChangesSinceLastSaved();
                EncodingChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool TryChangeLineEnding(LineEnding lineEnding)
        {
            if (LastSavedSnapshot.LineEnding != lineEnding)
            {
                RequestedLineEnding = lineEnding;
                IsModified = true;
                LineEndingChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            if (RequestedLineEnding != null && LastSavedSnapshot.LineEnding == lineEnding)
            {
                RequestedLineEnding = null;
                IsModified = !NoChangesSinceLastSaved();
                LineEndingChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public LineEnding GetLineEnding()
        {
            return RequestedLineEnding ?? LastSavedSnapshot.LineEnding;
        }

        public Encoding GetEncoding()
        {
            return RequestedEncoding ?? LastSavedSnapshot.Encoding;
        }

        private void OpenSplitView(IContentPreviewExtension extension)
        {
            SplitPanel.Content = extension;
            SplitPanelColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
            SplitPanelColumnDefinition.MinWidth = 100.0f;
            SplitPanel.Visibility = Visibility.Visible;
            GridSplitter.Visibility = Visibility.Visible;
            AnalyticsService.TrackEvent("MarkdownContentPreview_Opened");
            _isContentPreviewPanelOpened = true;
        }

        private void CloseSplitView()
        {
            SplitPanelColumnDefinition.Width = new GridLength(0);
            EditorColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
            SplitPanelColumnDefinition.MinWidth = 0.0f;
            SplitPanel.Visibility = Visibility.Collapsed;
            GridSplitter.Visibility = Visibility.Collapsed;
            TextEditorCore.ResetFocusAndScrollToPreviousPosition();
            _isContentPreviewPanelOpened = false;
        }

        public void ShowHideContentPreview()
        {
            if (_contentPreviewExtension == null)
            {
                _contentPreviewExtension = ExtensionProvider?.GetContentPreviewExtension(FileType);
                if (_contentPreviewExtension == null) return;
                _contentPreviewExtension.Bind(TextEditorCore);
            }

            if (SplitPanel == null) LoadSplitView();

            if (SplitPanel.Visibility == Visibility.Collapsed)
            {
                _contentPreviewExtension.IsExtensionEnabled = true;
                OpenSplitView(_contentPreviewExtension);
            }
            else
            {
                _contentPreviewExtension.IsExtensionEnabled = false;
                CloseSplitView();
            }
        }

        public void OpenSideBySideDiffViewer()
        {
            if (string.Equals(LastSavedSnapshot.Content, TextEditorCore.GetText())) return;
            if (Mode == TextEditorMode.DiffPreview) return;
            if (SideBySideDiffViewer == null) LoadSideBySideDiffViewer();
            Mode = TextEditorMode.DiffPreview;
            TextEditorCore.IsEnabled = false;
            EditorRowDefinition.Height = new GridLength(0);
            SideBySideDiffViewRowDefinition.Height = new GridLength(1, GridUnitType.Star);
            SideBySideDiffViewer.Visibility = Visibility.Visible;
            SideBySideDiffViewer.RenderDiff(LastSavedSnapshot.Content, TextEditorCore.GetText(), ThemeSettingsService.ThemeMode);
            SideBySideDiffViewer.Focus();
            AnalyticsService.TrackEvent("SideBySideDiffViewer_Opened");
        }

        public void CloseSideBySideDiffViewer()
        {
            if (Mode != TextEditorMode.DiffPreview) return;
            Mode = TextEditorMode.Editing;
            TextEditorCore.IsEnabled = true;
            EditorRowDefinition.Height = new GridLength(1, GridUnitType.Star);
            SideBySideDiffViewRowDefinition.Height = new GridLength(0);
            SideBySideDiffViewer.Visibility = Visibility.Collapsed;
            SideBySideDiffViewer.StopRenderingAndClearCache();
            TextEditorCore.ResetFocusAndScrollToPreviousPosition();
        }

        private void ShowHideSideBySideDiffViewer()
        {
            if (Mode != TextEditorMode.DiffPreview)
            {
                OpenSideBySideDiffViewer();
            }
            else
            {
                CloseSideBySideDiffViewer();
            }
        }

        /// <summary>
        /// Returns 1-based indexing values
        /// </summary>
        public void GetLineColumnSelection(
            out int startLine,
            out int endLine,
            out int startColumn,
            out int endColumn,
            out int selected,
            out int lineCount)
        {
            TextEditorCore.GetLineColumnSelection(
                out startLine,
                out endLine,
                out startColumn,
                out endColumn,
                out selected,
                out lineCount,
                GetLineEnding());
        }

        public double GetFontZoomFactor()
        {
            return TextEditorCore.GetFontZoomFactor();
        }

        public void SetFontZoomFactor(double fontZoomFactor)
        {
            TextEditorCore.SetFontZoomFactor(fontZoomFactor);
        }

        public bool IsEditorEnabled()
        {
            return TextEditorCore.IsEnabled;
        }

        public async Task SaveContentToFileAndUpdateEditorStateAsync(StorageFile file)
        {
            if (Mode == TextEditorMode.DiffPreview) CloseSideBySideDiffViewer();
            TextFile textFile = await SaveContentToFileAsync(file); // Will throw if not succeeded
            FileModificationState = FileModificationState.Untouched;
            Init(textFile, file, clearUndoQueue: false, resetText: false);
            FileSaved?.Invoke(this, EventArgs.Empty);
            StartCheckingFileStatusPeriodically();
        }

        private async Task<TextFile> SaveContentToFileAsync(StorageFile file)
        {
            var text = TextEditorCore.GetText();
            var encoding = RequestedEncoding ?? LastSavedSnapshot.Encoding;
            var lineEnding = RequestedLineEnding ?? LastSavedSnapshot.LineEnding;
            await FileSystemUtility.WriteTextToFileAsync(file, LineEndingUtility.ApplyLineEnding(text, lineEnding), encoding); // Will throw if not succeeded
            var newFileModifiedTime = await FileSystemUtility.GetDateModifiedAsync(file);
            return new TextFile(text, encoding, lineEnding, newFileModifiedTime);
        }

        public string GetContentForSharing()
        {
            return TextEditorCore.Document.Selection.StartPosition == TextEditorCore.Document.Selection.EndPosition ?
                TextEditorCore.GetText() :
                TextEditorCore.Document.Selection.Text;
        }

        public void TypeText(string text)
        {
            if (TextEditorCore.IsEnabled)
            {
                TextEditorCore.Document.Selection.TypeText(text);
            }
        }

        public void Focus()
        {
            if (Mode == TextEditorMode.DiffPreview)
            {
                SideBySideDiffViewer.Focus();
            }
            else if (Mode == TextEditorMode.Editing)
            {
                TextEditorCore.ResetFocusAndScrollToPreviousPosition();
            }
        }

        public FlyoutBase GetContextFlyout()
        {
            return TextEditorCore.ContextFlyout;
        }

        public void CopyTextToWindowsClipboard(TextControlCopyingToClipboardEventArgs args)
        {
            if (args != null)
            {
                args.Handled = true;
            }

            if (AppSettingsService.IsSmartCopyEnabled)
            {
                TextEditorCore.SmartlyTrimTextSelection();
            }

            CopyTextToWindowsClipboardInternal(true);
        }

        public void CutSelectedTextToWindowsClipboard(TextControlCuttingToClipboardEventArgs args)
        {
            if (args != null)
            {
                args.Handled = true;
            }

            CopyTextToWindowsClipboardInternal(false);
            TextEditorCore.Document.Selection.SetText(TextSetOptions.None, string.Empty);
        }

        private void CopyTextToWindowsClipboardInternal(bool clearLineSelection)
        {
            try
            {
                DataPackage dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };

                // Selection.Length can be negative when user selecting text from right to left
                var isTextSelected = TextEditorCore.Document.Selection.Length != 0;
                var cursorPosition = TextEditorCore.Document.Selection.StartPosition;

                if (!isTextSelected)
                {
                    TextEditorCore.Document.Selection.Expand(TextRangeUnit.Paragraph);
                }

                var text = LineEndingUtility.ApplyLineEnding(TextEditorCore.Document.Selection.Text, GetLineEnding());
                dataPackage.SetText(text);

                if (clearLineSelection && !isTextSelected)
                {
                    TextEditorCore.Document.Selection.SetRange(cursorPosition, cursorPosition);
                }

                Clipboard.SetContentWithOptions(dataPackage, new ClipboardContentOptions() { IsAllowedInHistory = true, IsRoamable = true });
                Clipboard.Flush(); // This method allows the content to remain available after the application shuts down.
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[{nameof(TextEditor)}] Failed to copy plain text to Windows clipboard: {ex.Message}");
            }
        }

        public bool NoChangesSinceLastSaved(bool compareTextOnly = false)
        {
            if (!_loaded) return true;

            if (!compareTextOnly)
            {
                if (RequestedLineEnding != null)
                {
                    return false;
                }

                if (RequestedEncoding != null)
                {
                    return false;
                }
            }

            return string.Equals(LastSavedSnapshot.Content, TextEditorCore.GetText());
        }

        private void OnEscapeKeyDown()
        {
            if (_isContentPreviewPanelOpened)
            {
                _contentPreviewExtension.IsExtensionEnabled = false;
                CloseSplitView();
            }
            else if (FindAndReplacePlaceholder != null && FindAndReplacePlaceholder.Visibility == Visibility.Visible)
            {
                HideFindAndReplaceControl();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
            else if (GoToPlaceholder != null && GoToPlaceholder.Visibility == Visibility.Visible)
            {
                HideGoToControl();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        private void LoadSplitView()
        {
            FindName("SplitPanel");
            FindName("GridSplitter");
            SplitPanel.Visibility = Visibility.Collapsed;
            GridSplitter.Visibility = Visibility.Collapsed;
            SplitPanel.KeyDown += SplitPanel_OnKeyDown;
        }

        private void LoadSideBySideDiffViewer()
        {
            FindName("SideBySideDiffViewer");
            SideBySideDiffViewer.Visibility = Visibility.Collapsed;
            SideBySideDiffViewer.OnCloseEvent += SideBySideDiffViewer_OnCloseEvent;
        }

        private void SideBySideDiffViewer_OnCloseEvent(object sender, EventArgs e)
        {
            CloseSideBySideDiffViewer();
        }

        private void SplitPanel_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var result = _keyboardCommandHandler.Handle(e);
            if (result.ShouldHandle)
            {
                e.Handled = true;
            }
        }

        private void TextEditorCore_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TextEditorCore_OnFontZoomFactorChanged(object sender, double e)
        {
            FontZoomFactorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TextEditorCore_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);

            if (FindAndReplacePlaceholder?.Visibility == Visibility.Visible && !ctrl.HasFlag(CoreVirtualKeyStates.Down) && !alt.HasFlag(CoreVirtualKeyStates.Down))
            {
                if (e.Key == VirtualKey.F3)
                {
                    return;
                }
            }

            var result = _keyboardCommandHandler.Handle(e);
            if (result.ShouldHandle)
            {
                e.Handled = true;
            }
        }

        private void TextEditorCore_OnTextChanging(RichEditBox textEditor, RichEditBoxTextChangingEventArgs args)
        {
            if (!args.IsContentChanging || !_loaded) return;
            if (IsModified)
            {
                IsModified = !NoChangesSinceLastSaved();
            }
            else
            {
                IsModified = !NoChangesSinceLastSaved(compareTextOnly: true);
            }
            TextChanging?.Invoke(this, EventArgs.Empty);

            GoToPlaceholder?.Dismiss();
        }

        private void TextEditorCore_CopyTextToWindowsClipboardRequested(object sender, TextControlCopyingToClipboardEventArgs e)
        {
            CopyTextToWindowsClipboard(e);
        }

        private void TextEditorCore_CutSelectedTextToWindowsClipboardRequested(object sender, TextControlCuttingToClipboardEventArgs e)
        {
            CutSelectedTextToWindowsClipboard(e);
        }

        private void FindAndReplaceControl_OnToggleReplaceModeButtonClicked(object sender, bool showReplaceBar)
        {
            ShowFindAndReplaceControl(showReplaceBar);
        }

        public void ShowFindAndReplaceControl(bool showReplaceBar)
        {
            if (!TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing)
            {
                return;
            }

            GoToPlaceholder?.Dismiss();

            if (FindAndReplacePlaceholder == null)
            {
                FindName("FindAndReplacePlaceholder"); // Lazy loading
            }

            var findAndReplace = (FindAndReplaceControl)FindAndReplacePlaceholder.Content;

            if (findAndReplace == null) return;

            FindAndReplacePlaceholder.Height = findAndReplace.GetHeight(showReplaceBar);
            findAndReplace.ShowReplaceBar(showReplaceBar);

            if (FindAndReplacePlaceholder.Visibility == Visibility.Collapsed)
            {
                FindAndReplacePlaceholder.Show();
            }

            string searchStr = TextEditorCore.GetSearchString();
            findAndReplace.Focus(searchStr, FindAndReplaceMode.FindOnly);
            if (!string.IsNullOrEmpty(searchStr))
            {
                var ctx = new SearchContext(searchStr, false, false, false);
                (int cur, int total) = TextEditorCore.GetSearchMatchesCount(ctx);
                findAndReplace.UpdateMatchCount(cur, total);
            }
        }

        public void HideFindAndReplaceControl()
        {
            FindAndReplacePlaceholder?.Dismiss();
        }

        private void FindAndReplaceControl_OnLiveSearchTriggered(object sender, SearchContext context)
        {
            if (string.IsNullOrEmpty(context.SearchText))
            {
                FindAndReplaceControl?.UpdateMatchCount(0, 0);
                return;
            }

            TextEditorCore.TryFindNextAndSelect(context, stopAtEof: false, out bool regexError);
            (int cur, int total) = TextEditorCore.GetSearchMatchesCount(context);
            FindAndReplaceControl?.UpdateMatchCount(cur, total, regexError);
        }

        private async void FindAndReplaceControl_OnFindAndReplaceButtonClicked(object sender, FindAndReplaceEventArgs e)
        {
            TextEditorCore.Focus(FocusState.Programmatic);
            InitiateFindAndReplace(e, out bool found);

            // In case user hit "enter" key in search box instead of clicking on search button or hit F3
            // We should re-focus on FindAndReplaceControl to make the next search "flows"
            if (!(sender is Button))
            {
                if (found)
                {
                    // Wait for layout to refresh (ScrollViewer scroll to the found text) before focusing
                    await Task.Delay(10);
                }
                FindAndReplaceControl.Focus(string.Empty, e.FindAndReplaceMode);
            }
        }

        private void InitiateFindAndReplace(FindAndReplaceEventArgs findAndReplaceEventArgs, out bool found)
        {
            found = false;

            if (string.IsNullOrEmpty(findAndReplaceEventArgs.SearchContext.SearchText)) return;

            bool regexError = false;

            if (FindAndReplacePlaceholder?.Visibility == Visibility.Visible)
                _lastSearchContext = findAndReplaceEventArgs.SearchContext;

            switch (findAndReplaceEventArgs.FindAndReplaceMode)
            {
                case FindAndReplaceMode.FindOnly:
                    found = findAndReplaceEventArgs.SearchDirection == SearchDirection.Next
                        ? TextEditorCore.TryFindNextAndSelect(
                            findAndReplaceEventArgs.SearchContext,
                            stopAtEof: false,
                            out regexError)
                        : TextEditorCore.TryFindPreviousAndSelect(
                            findAndReplaceEventArgs.SearchContext,
                            stopAtBof: false,
                            out regexError);
                    break;
                case FindAndReplaceMode.Replace:
                    found = findAndReplaceEventArgs.SearchDirection == SearchDirection.Next
                        ? TextEditorCore.TryFindNextAndReplace(
                            findAndReplaceEventArgs.SearchContext,
                            findAndReplaceEventArgs.ReplaceText,
                            out regexError)
                        : TextEditorCore.TryFindPreviousAndReplace(
                            findAndReplaceEventArgs.SearchContext,
                            findAndReplaceEventArgs.ReplaceText,
                            out regexError);
                    break;
                case FindAndReplaceMode.ReplaceAll:
                    int replacedCount = 0;
                    found = TextEditorCore.TryFindAndReplaceAll(
                        findAndReplaceEventArgs.SearchContext,
                        findAndReplaceEventArgs.ReplaceText,
                        out replacedCount,
                        out regexError);

                    if (found)
                    {
                        string tmpl = _resourceLoader.GetString("FindAndReplace_NotificationMsg_ReplacedCount");
                        string msg = !string.IsNullOrEmpty(tmpl)
                            ? string.Format(tmpl, replacedCount)
                            : $"แทนที่ทั้งหมด {replacedCount} รายการสำเร็จ";
                        NotificationCenter.Instance.PostNotification(msg, 2000);
                    }
                    break;
            }

            (int cur, int total) = TextEditorCore.GetSearchMatchesCount(findAndReplaceEventArgs.SearchContext);
            FindAndReplaceControl?.UpdateMatchCount(cur, total, regexError);

            if (!found && findAndReplaceEventArgs.FindAndReplaceMode != FindAndReplaceMode.ReplaceAll)
            {
                if (findAndReplaceEventArgs.SearchContext.UseRegex && regexError)
                {
                    NotificationCenter.Instance.PostNotification(_resourceLoader.GetString("FindAndReplace_NotificationMsg_InvalidRegex"), 1500);
                }
                else
                {
                    NotificationCenter.Instance.PostNotification(_resourceLoader.GetString("FindAndReplace_NotificationMsg_NotFound"), 1500);
                }
            }
        }

        private void FindAndReplacePlaceholder_Closed(object sender, InAppNotificationClosedEventArgs e)
        {
            FindAndReplacePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void FindAndReplaceControl_OnDismissKeyDown(object sender, RoutedEventArgs e)
        {
            FindAndReplacePlaceholder?.Dismiss();
            TextEditorCore.Focus(FocusState.Programmatic);
        }

        public void ShowGoToControl()
        {
            if (!TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            FindAndReplacePlaceholder?.Dismiss();

            if (GoToPlaceholder == null)
                FindName("GoToPlaceholder"); // Lazy loading

            var goToControl = (GoToControl)GoToPlaceholder.Content;

            if (goToControl == null) return;

            GoToPlaceholder.Height = goToControl.GetHeight();

            if (GoToPlaceholder.Visibility == Visibility.Collapsed)
                GoToPlaceholder.Show();

            GetLineColumnSelection(out var startLine, out _, out _, out _, out _, out var lineCount);
            goToControl.SetLineData(startLine, lineCount);
            goToControl.Focus();
        }

        public void HideGoToControl()
        {
            GoToPlaceholder?.Dismiss();
        }

        private void GoToControl_OnGoToButtonClicked(object sender, GoToEventArgs e)
        {
            var found = false;

            if (int.TryParse(e.SearchLine, out var line))
            {
                found = TextEditorCore.GoTo(line);
            }

            if (!found)
            {
                GoToControl.Focus();
                NotificationCenter.Instance.PostNotification(_resourceLoader.GetString("FindAndReplace_NotificationMsg_NotFound"), 1500);
            }
            else
            {
                HideGoToControl();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        private void GoToPlaceholder_Closed(object sender, InAppNotificationClosedEventArgs e)
        {
            GoToPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void GoToControl_OnDismissKeyDown(object sender, RoutedEventArgs e)
        {
            GoToPlaceholder.Dismiss();
            TextEditorCore.Focus(FocusState.Programmatic);
        }

        public void Undo()
        {
            if (TextEditorCore.IsEnabled && TextEditorCore.Document.CanUndo())
            {
                TextEditorCore.Document.Undo();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Redo()
        {
            if (TextEditorCore.IsEnabled && TextEditorCore.Document.CanRedo())
            {
                TextEditorCore.Document.Redo();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Cut()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.Document.Selection.Cut();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Copy()
        {
            if (TextEditorCore.IsEnabled)
            {
                TextEditorCore.Document.Selection.Copy();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Paste()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.Document.Selection.Paste(0);
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Delete()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.Document.Selection.SetText(Windows.UI.Text.TextSetOptions.None, string.Empty);
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void SelectAll()
        {
            if (TextEditorCore.IsEnabled)
            {
                TextEditorCore.Document.Selection.SetRange(0, TextEditorCore.GetText().Length);
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void InsertDateTime()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.TryInsertNewLogEntry();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void ToggleWordWrap()
        {
            TextEditorCore.TextWrapping = (TextEditorCore.TextWrapping == TextWrapping.Wrap ? TextWrapping.NoWrap : TextWrapping.Wrap);
        }

        public bool IsWordWrap()
        {
            return TextEditorCore.TextWrapping == TextWrapping.Wrap || TextEditorCore.TextWrapping == TextWrapping.WrapWholeWords;
        }

        public void FormatBold()
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;
            ApplyCharacterFormatting("Bold");
        }

        public void FormatItalic()
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;
            ApplyCharacterFormatting("Italic");
        }

        public void FormatStrikethrough()
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;
            ApplyCharacterFormatting("Strikethrough");
        }

        public bool IsBold()
        {
            if (!_loaded || !TextEditorCore.IsEnabled) return false;
            try
            {
                return TextEditorCore.Document.Selection.CharacterFormat.Bold == Windows.UI.Text.FormatEffect.On;
            }
            catch
            {
                return false;
            }
        }

        public bool IsItalic()
        {
            if (!_loaded || !TextEditorCore.IsEnabled) return false;
            try
            {
                return TextEditorCore.Document.Selection.CharacterFormat.Italic == Windows.UI.Text.FormatEffect.On;
            }
            catch
            {
                return false;
            }
        }

        public bool IsStrikethrough()
        {
            if (!_loaded || !TextEditorCore.IsEnabled) return false;
            try
            {
                return TextEditorCore.Document.Selection.CharacterFormat.Strikethrough == Windows.UI.Text.FormatEffect.On;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyCharacterFormatting(string formatType)
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            try
            {
                var selection = TextEditorCore.Document.Selection;
                int start = selection.StartPosition;
                int end = selection.EndPosition;
                if (start > end) { int t = start; start = end; end = t; }

                var doc = TextEditorCore.GetText() ?? string.Empty;

                if (start == end)
                {
                    int wordStart = start;
                    while (wordStart > 0 && wordStart - 1 < doc.Length && Notepads.Extensions.StringExtensions.IsWordCharacter(doc[wordStart - 1]))
                    {
                        wordStart--;
                    }
                    int wordEnd = start;
                    while (wordEnd < doc.Length && Notepads.Extensions.StringExtensions.IsWordCharacter(doc[wordEnd]))
                    {
                        wordEnd++;
                    }

                    if (wordStart < wordEnd)
                    {
                        var wordRange = TextEditorCore.Document.GetRange(wordStart, wordEnd);
                        string wordText = wordRange.Text ?? string.Empty;
                        string rawMarker = formatType == "Bold" ? "**" : (formatType == "Italic" ? "*" : "~~");

                        if (wordStart >= rawMarker.Length && wordEnd + rawMarker.Length <= doc.Length &&
                            doc.Substring(wordStart - rawMarker.Length, rawMarker.Length) == rawMarker &&
                            doc.Substring(wordEnd, rawMarker.Length) == rawMarker)
                        {
                            var fullRange = TextEditorCore.Document.GetRange(wordStart - rawMarker.Length, wordEnd + rawMarker.Length);
                            fullRange.SetText(Windows.UI.Text.TextSetOptions.None, wordText);
                            wordStart -= rawMarker.Length;
                            wordEnd = wordStart + wordText.Length;
                            wordRange = TextEditorCore.Document.GetRange(wordStart, wordEnd);
                        }

                        ApplyEffectToRange(wordRange, formatType);
                        TextEditorCore.Document.Selection.SetRange(wordStart, wordEnd);
                    }
                    else
                    {
                        ApplyEffectToFormat(TextEditorCore.Document.Selection.CharacterFormat, formatType);
                    }
                }
                else
                {
                    string selectedText = selection.Text ?? string.Empty;
                    string rawMarker = formatType == "Bold" ? "**" : (formatType == "Italic" ? "*" : "~~");

                    if (selectedText.StartsWith(rawMarker) && selectedText.EndsWith(rawMarker) && selectedText.Length >= rawMarker.Length * 2)
                    {
                        string stripped = selectedText.Substring(rawMarker.Length, selectedText.Length - rawMarker.Length * 2);
                        selection.SetText(Windows.UI.Text.TextSetOptions.None, stripped);
                        end = start + stripped.Length;
                        selection.SetRange(start, end);
                    }

                    ApplyEffectToRange(selection, formatType);
                }

                TextEditorCore.Focus(FocusState.Programmatic);
            }
            catch { }
        }

        private void ApplyEffectToRange(Windows.UI.Text.ITextRange range, string formatType)
        {
            switch (formatType)
            {
                case "Bold":
                    range.CharacterFormat.Bold = range.CharacterFormat.Bold == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
                case "Italic":
                    range.CharacterFormat.Italic = range.CharacterFormat.Italic == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
                case "Strikethrough":
                    range.CharacterFormat.Strikethrough = range.CharacterFormat.Strikethrough == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
            }
        }

        private void ApplyEffectToFormat(Windows.UI.Text.ITextCharacterFormat format, string formatType)
        {
            switch (formatType)
            {
                case "Bold":
                    format.Bold = format.Bold == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
                case "Italic":
                    format.Italic = format.Italic == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
                case "Strikethrough":
                    format.Strikethrough = format.Strikethrough == Windows.UI.Text.FormatEffect.On
                        ? Windows.UI.Text.FormatEffect.Off
                        : Windows.UI.Text.FormatEffect.On;
                    break;
            }
        }

        public void WrapSelection(string prefix, string suffix)
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            if (prefix == "**" && suffix == "**")
            {
                FormatBold();
                return;
            }
            if (prefix == "*" && suffix == "*")
            {
                FormatItalic();
                return;
            }
            if (prefix == "~~" && suffix == "~~")
            {
                FormatStrikethrough();
                return;
            }

            try
            {
                var selection = TextEditorCore.Document.Selection;
                string selectedText = selection.Text ?? string.Empty;

                if (string.IsNullOrEmpty(selectedText))
                {
                    selection.SetText(Windows.UI.Text.TextSetOptions.None, prefix + suffix);
                    int newPos = selection.StartPosition + prefix.Length;
                    TextEditorCore.SetTextSelectionPosition(newPos, newPos);
                }
                else
                {
                    if (selectedText.StartsWith(prefix) && selectedText.EndsWith(suffix) && selectedText.Length >= (prefix.Length + suffix.Length))
                    {
                        string unwrapped = selectedText.Substring(prefix.Length, selectedText.Length - prefix.Length - suffix.Length);
                        selection.SetText(Windows.UI.Text.TextSetOptions.None, unwrapped);
                    }
                    else
                    {
                        selection.SetText(Windows.UI.Text.TextSetOptions.None, prefix + selectedText + suffix);
                    }
                }
                TextEditorCore.Focus(FocusState.Programmatic);
            }
            catch { }
        }

        public Notepads.Utilities.MarkdownHeadingStyle GetCurrentLineHeadingStyle()
        {
            if (!_loaded || !TextEditorCore.IsEnabled) return Notepads.Utilities.MarkdownHeadingStyle.Body;
            try
            {
                var document = TextEditorCore.GetText();
                float baseSize = (float)TextEditorCore.FontSize;

                float selSize = TextEditorCore.Document.Selection.CharacterFormat.Size;
                bool selBold = TextEditorCore.Document.Selection.CharacterFormat.Bold == Windows.UI.Text.FormatEffect.On;
                var detectedFromSel = Notepads.Utilities.MarkdownHeadingHelper.DetectStyleFromFormat(selSize, selBold, baseSize);

                if (string.IsNullOrEmpty(document))
                {
                    return detectedFromSel;
                }

                TextEditorCore.GetTextSelectionPosition(out int start, out _);
                if (start > document.Length) start = document.Length;

                int lineStart = start;
                while (lineStart > 0 && document[lineStart - 1] != '\r' && document[lineStart - 1] != '\n')
                {
                    lineStart--;
                }

                int lineEnd = start;
                while (lineEnd < document.Length && document[lineEnd] != '\r' && document[lineEnd] != '\n')
                {
                    lineEnd++;
                }

                if (lineEnd > lineStart)
                {
                    var lineRange = TextEditorCore.Document.GetRange(lineStart, Math.Min(lineStart + 1, lineEnd));
                    float lineSize = lineRange.CharacterFormat.Size;
                    bool lineBold = lineRange.CharacterFormat.Bold == Windows.UI.Text.FormatEffect.On;
                    var detectedFromLine = Notepads.Utilities.MarkdownHeadingHelper.DetectStyleFromFormat(lineSize, lineBold, baseSize);
                    if (detectedFromLine != Notepads.Utilities.MarkdownHeadingStyle.Body)
                    {
                        return detectedFromLine;
                    }

                    string currentLine = document.Substring(lineStart, lineEnd - lineStart);
                    return Notepads.Utilities.MarkdownHeadingHelper.DetectStyle(currentLine);
                }

                return detectedFromSel;
            }
            catch
            {
                return Notepads.Utilities.MarkdownHeadingStyle.Body;
            }
        }

        public void FormatHeading(string prefix)
        {
            if (!_loaded || !TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            try
            {
                var targetStyle = Notepads.Utilities.MarkdownHeadingHelper.GetStyleForPrefix(prefix);
                var doc = TextEditorCore.GetText() ?? string.Empty;
                TextEditorCore.GetTextSelectionPosition(out int start, out int end);
                if (start > doc.Length) start = doc.Length;
                if (end > doc.Length) end = doc.Length;
                if (start > end) { int t = start; start = end; end = t; }

                int lineStart = start;
                while (lineStart > 0 && doc[lineStart - 1] != '\r' && doc[lineStart - 1] != '\n')
                {
                    lineStart--;
                }

                int lineEnd = end;
                if (end > start && end > 0 && (doc[end - 1] == '\r' || doc[end - 1] == '\n') && lineEnd == end)
                {
                    lineEnd = end - 1;
                    while (lineEnd > lineStart && (doc[lineEnd] == '\r' || doc[lineEnd] == '\n'))
                    {
                        lineEnd--;
                    }
                }
                while (lineEnd < doc.Length && doc[lineEnd] != '\r' && doc[lineEnd] != '\n')
                {
                    lineEnd++;
                }
                if (lineEnd < lineStart) lineEnd = lineStart;

                string lineText = (lineEnd > lineStart) ? doc.Substring(lineStart, lineEnd - lineStart) : string.Empty;

                // Strip any leading '#' symbols from the line (e.g. "###### ฟหหก" or "### Title")
                string cleanedLineText = System.Text.RegularExpressions.Regex.Replace(
                    lineText,
                    @"^(\s*)#{1,6}\s*",
                    "$1",
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                if (cleanedLineText != lineText)
                {
                    var lineRange = TextEditorCore.Document.GetRange(lineStart, lineEnd);
                    lineRange.SetText(Windows.UI.Text.TextSetOptions.None, cleanedLineText);
                    lineEnd = lineStart + cleanedLineText.Length;
                }

                var currentStyle = GetCurrentLineHeadingStyle();
                var effectiveStyle = (targetStyle == currentStyle || targetStyle == Notepads.Utilities.MarkdownHeadingStyle.Body)
                    ? Notepads.Utilities.MarkdownHeadingStyle.Body
                    : targetStyle;

                float baseFontSize = (float)TextEditorCore.FontSize;
                float targetSize = Notepads.Utilities.MarkdownHeadingHelper.GetHeadingFontSize(effectiveStyle, baseFontSize);
                bool isBold = Notepads.Utilities.MarkdownHeadingHelper.IsHeadingBold(effectiveStyle);
                var targetBold = isBold ? Windows.UI.Text.FormatEffect.On : Windows.UI.Text.FormatEffect.Off;

                if (lineEnd > lineStart)
                {
                    var range = TextEditorCore.Document.GetRange(lineStart, lineEnd);
                    range.CharacterFormat.Size = targetSize;
                    range.CharacterFormat.Bold = targetBold;

                    if (start == end)
                    {
                        int newCursor = Math.Min(lineEnd, Math.Max(lineStart, start));
                        TextEditorCore.Document.Selection.SetRange(newCursor, newCursor);
                        TextEditorCore.Document.Selection.CharacterFormat.Size = targetSize;
                        TextEditorCore.Document.Selection.CharacterFormat.Bold = targetBold;
                    }
                    else
                    {
                        TextEditorCore.Document.Selection.SetRange(lineStart, lineEnd);
                    }
                }
                else
                {
                    TextEditorCore.Document.Selection.SetRange(lineStart, lineStart);
                    TextEditorCore.Document.Selection.CharacterFormat.Size = targetSize;
                    TextEditorCore.Document.Selection.CharacterFormat.Bold = targetBold;
                }

                TextEditorCore.Focus(FocusState.Programmatic);
            }
            catch { }
        }

        public void FormatLinePrefix(string prefix)
        {
            if (!TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            var document = TextEditorCore.GetText();
            TextEditorCore.GetTextSelectionPosition(out int start, out int end);
            if (start > document.Length) start = document.Length;
            if (end > document.Length) end = document.Length;

            int lineStart = start;
            while (lineStart > 0 && document[lineStart - 1] != '\r' && document[lineStart - 1] != '\n')
            {
                lineStart--;
            }

            int lineEnd = end;
            if (end > start && end > 0 && (document[end - 1] == '\r' || document[end - 1] == '\n') && lineEnd == end)
            {
                lineEnd = end - 1;
                while (lineEnd > lineStart && (document[lineEnd] == '\r' || document[lineEnd] == '\n'))
                {
                    lineEnd--;
                }
            }

            while (lineEnd < document.Length && document[lineEnd] != '\r' && document[lineEnd] != '\n')
            {
                lineEnd++;
            }

            if (lineEnd < lineStart) lineEnd = lineStart;
            string selectedBlock = document.Substring(lineStart, lineEnd - lineStart);
            var lines = selectedBlock.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string delimiter = selectedBlock.Contains("\r\n") ? "\r\n" : (selectedBlock.Contains("\r") ? "\r" : "\n");

            var listRegex = new System.Text.RegularExpressions.Regex(@"^(\s*)(-\s+|\*\s+|\d+\.\s+)");
            var formattedLines = new System.Collections.Generic.List<string>(lines.Length);

            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l))
                {
                    formattedLines.Add(l);
                    continue;
                }

                var match = listRegex.Match(l);
                if (match.Success)
                {
                    string indent = match.Groups[1].Value;
                    string existing = match.Groups[2].Value;
                    if (existing == prefix)
                    {
                        formattedLines.Add(indent + l.Substring(match.Length));
                    }
                    else
                    {
                        formattedLines.Add(indent + prefix + l.Substring(match.Length));
                    }
                }
                else
                {
                    string trimmed = l.TrimStart();
                    int leadingSpaces = l.Length - trimmed.Length;
                    string indent = leadingSpaces > 0 ? l.Substring(0, leadingSpaces) : string.Empty;
                    formattedLines.Add(indent + prefix + trimmed);
                }
            }

            string formattedBlock = string.Join(delimiter, formattedLines);
            TextEditorCore.Document.Selection.SetRange(lineStart, lineEnd);
            TextEditorCore.Document.Selection.SetText(Windows.UI.Text.TextSetOptions.None, formattedBlock);

            if (start == end)
            {
                int offset = formattedBlock.Length - selectedBlock.Length;
                int newCursorPos = Math.Max(lineStart, Math.Min(start + offset, lineStart + formattedBlock.Length));
                TextEditorCore.Document.Selection.SetRange(newCursorPos, newCursorPos);
            }
            else
            {
                TextEditorCore.Document.Selection.SetRange(lineStart, lineStart + formattedBlock.Length);
            }

            TextEditorCore.Focus(FocusState.Programmatic);
        }

        public void InsertTable(int rows, int cols)
        {
            if (!TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            if (rows < 1) rows = 1;
            if (cols < 1) cols = 1;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine();
            sb.Append("|");
            for (int c = 1; c <= cols; c++) sb.Append($" หัวข้อ {c} |");
            sb.AppendLine();
            sb.Append("|");
            for (int c = 1; c <= cols; c++) sb.Append(" --- |");
            sb.AppendLine();
            for (int r = 1; r <= rows; r++)
            {
                sb.Append("|");
                for (int c = 1; c <= cols; c++) sb.Append(" ข้อความ |");
                sb.AppendLine();
            }
            sb.AppendLine();

            TextEditorCore.Document.Selection.SetText(Windows.UI.Text.TextSetOptions.None, sb.ToString());
            TextEditorCore.Focus(FocusState.Programmatic);
        }

        public void ClearFormatting()
        {
            if (!TextEditorCore.IsEnabled || Mode != TextEditorMode.Editing) return;

            var selection = TextEditorCore.Document.Selection;
            string text = selection.Text;
            if (string.IsNullOrEmpty(text))
            {
                selection.Expand(Windows.UI.Text.TextRangeUnit.Paragraph);
                text = selection.Text ?? string.Empty;
            }

            string cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"(\*\*|__)(.*?)\1", "$2");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\*|_)(.*?)\1", "$2");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(~~)(.*?)\1", "$2");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(`)(.*?)\1", "$2");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(\s*)[#>\-\*\+]\s+", "$1", System.Text.RegularExpressions.RegexOptions.Multiline);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\[(.*?)\]\(.*?\)", "$1");

            selection.SetText(Windows.UI.Text.TextSetOptions.None, cleaned);
            selection.CharacterFormat.Size = (float)TextEditorCore.FontSize;
            selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Off;
            selection.CharacterFormat.Italic = Windows.UI.Text.FormatEffect.Off;
            selection.CharacterFormat.Strikethrough = Windows.UI.Text.FormatEffect.Off;
            selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.None;
            TextEditorCore.Focus(FocusState.Programmatic);
        }

        public void Indent()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.AddIndentation();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }

        public void Unindent()
        {
            if (TextEditorCore.IsEnabled && Mode == TextEditorMode.Editing)
            {
                TextEditorCore.RemoveIndentation();
                TextEditorCore.Focus(FocusState.Programmatic);
            }
        }
    }
}