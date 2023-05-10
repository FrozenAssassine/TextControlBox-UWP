using Collections.Pooled;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TextControlBox.Extensions;
using TextControlBox.Helper;
using TextControlBox.Languages;
using TextControlBox.Renderer;
using TextControlBox.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Text.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Color = Windows.UI.Color;

namespace TextControlBox
{
    public partial class TextControlBox : UserControl
    {
        private CursorSize _CursorSize = null;
        private FontFamily _FontFamily = new FontFamily("Consolas");
        private bool UseDefaultDesign = true;
        private TextControlBoxDesign _Design { get; set; }
        private string NewLineCharacter = "\r\n";
        private InputPane inputPane;
        private bool _ShowLineNumbers = true;
        private CodeLanguage _CodeLanguage = null;
        private LineEnding _LineEnding = LineEnding.CRLF;
        private bool _ShowLineHighlighter = true;
        private int _FontSize = 18;
        private int _ZoomFactor = 100; //%
        private double _HorizontalScrollSensitivity = 1;
        private double _VerticalScrollSensitivity = 1;
        private int DefaultVerticalScrollSensitivity = 4;
        private float SingleLineHeight { get => TextFormat == null ? 0 : TextFormat.LineSpacing; }
        private float ZoomedFontSize = 0;
        private int MaxFontsize = 125;
        private int MinFontSize = 3;
        private int OldZoomFactor = 0;
        private int NumberOfStartLine = 0;
        private int LongestLineLength = 0;
        private int LongestLineIndex = 0;
        private float OldHorizontalScrollValue = 0;
        private float _SpaceBetweenLineNumberAndText = 30;
        private ElementTheme _RequestedTheme = ElementTheme.Default;
        private ApplicationTheme _AppTheme = ApplicationTheme.Light;
        private TextControlBoxDesign LightDesign = new TextControlBoxDesign(
            new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)),
            Color.FromArgb(255, 50, 50, 50),
            Color.FromArgb(100, 0, 100, 255),
            Color.FromArgb(255, 0, 0, 0),
            Color.FromArgb(50, 200, 200, 200),
            Color.FromArgb(255, 180, 180, 180),
            Color.FromArgb(0, 0, 0, 0),
            Color.FromArgb(100, 200, 120, 0)
            );
        private TextControlBoxDesign DarkDesign = new TextControlBoxDesign(
            new SolidColorBrush(Color.FromArgb(0, 30, 30, 30)),
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(100, 0, 100, 255),
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(50, 100, 100, 100),
            Color.FromArgb(255, 100, 100, 100),
            Color.FromArgb(0, 0, 0, 0),
            Color.FromArgb(100, 160, 80, 0)
            );

        //Colors:
        CanvasSolidColorBrush TextColorBrush;
        CanvasSolidColorBrush CursorColorBrush;
        CanvasSolidColorBrush LineNumberColorBrush;
        CanvasSolidColorBrush LineHighlighterBrush;

        bool ColorResourcesCreated = false;
        bool NeedsTextFormatUpdate = false;
        bool DragDropSelection = false;
        bool HasFocus = true;
        bool NeedsUpdateTextLayout = false;
        bool NeedsRecalculateLongestLineIndex = true;

        CanvasTextFormat TextFormat = null;
        CanvasTextLayout DrawnTextLayout = null;
        CanvasTextFormat LineNumberTextFormat = null;

        string RenderedText = "";
        string LineNumberTextToRender = "";
        string OldRenderedText = null;
        string OldLineNumberTextToRender = "";
        //Handle double and triple -clicks:
        int PointerClickCount = 0;
        DispatcherTimer PointerClickTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 200) };

        Point? OldTouchPosition = null;

        //CursorPosition
        CursorPosition _CursorPosition = new CursorPosition(0, 0);
        CursorPosition OldCursorPosition = null;
        CanvasTextLayout CurrentLineTextLayout = null;
        TextSelection TextSelection = null;
        TextSelection OldTextSelection = null;
        CoreTextEditContext EditContext;

        //Store the lines in Lists
        private PooledList<string> TotalLines = new PooledList<string>(0);
        private IEnumerable<string> RenderedLines;
        private int NumberOfRenderedLines = 0;

        private string CurrentLine { get => TotalLines.GetCurrentLineText(); set => TotalLines.SetCurrentLineText(value); }
        StringBuilder LineNumberContent = new StringBuilder();

        //Classes
        private readonly SelectionRenderer selectionrenderer;
        private readonly UndoRedo undoRedo = new UndoRedo();
        private readonly FlyoutHelper flyoutHelper;
        private readonly TabSpaceHelper tabSpaceHelper = new TabSpaceHelper();
        private readonly StringManager stringManager;
        private readonly SearchHelper searchHelper = new SearchHelper();

        /// <summary>
        /// Creates a new instance of the TextControlBox
        /// </summary>
        public TextControlBox()
        {
            this.InitializeComponent();

            InitialiseTextService();

            //Classes & Variables:
            selectionrenderer = new SelectionRenderer();
            flyoutHelper = new FlyoutHelper(this);
            inputPane = InputPane.GetForCurrentView();
            stringManager = new StringManager(tabSpaceHelper);

            //Events:
            this.KeyDown += TextControlBox_KeyDown;
            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;

            //set default values
            RequestedTheme = ElementTheme.Default;
            LineEnding = LineEnding.CRLF;

            InitialiseOnStart();
            SetFocus();
        }

        #region Update functions
        private void UpdateAll()
        {
            UpdateText();
            UpdateSelection();
            UpdateCursor();
        }
        private void UpdateZoom()
        {
            ZoomedFontSize = _FontSize * (float)_ZoomFactor / 100;
            if (ZoomedFontSize < MinFontSize)
                ZoomedFontSize = MinFontSize;
            if (ZoomedFontSize > MaxFontsize)
                ZoomedFontSize = MaxFontsize;

            if (_ZoomFactor > 400)
                _ZoomFactor = 400;
            if (_ZoomFactor < 4)
                _ZoomFactor = 4;

            if (_ZoomFactor != OldZoomFactor)
            {
                NeedsUpdateTextLayout = true;
                OldZoomFactor = _ZoomFactor;
                ZoomChanged?.Invoke(this, _ZoomFactor);
            }

            NeedsTextFormatUpdate = true;

            ScrollLineIntoView(CursorPosition.LineNumber);
            NeedsUpdateLineNumbers();
            UpdateAll();
        }
        private void UpdateCursor()
        {
            Canvas_Cursor.Invalidate();
        }
        private void UpdateText()
        {
            Utils.ChangeCursor(CoreCursorType.IBeam);
            Canvas_Text.Invalidate();
        }
        private void UpdateSelection()
        {
            Canvas_Selection.Invalidate();
        }
        private void NeedsUpdateLineNumbers()
        {
            OldLineNumberTextToRender = "";
        }
        private void UpdateCurrentLineTextLayout()
        {
            if (CursorPosition.LineNumber < TotalLines.Count)
                CurrentLineTextLayout = TextRenderer.CreateTextLayout(Canvas_Text, TextFormat, TotalLines.GetLineText(CursorPosition.LineNumber) + "|", Canvas_Text.Size);
            else
                CurrentLineTextLayout = null;
        }
        private void UpdateCursorVariable(Point point)
        {
            //Apply an offset to the cursorposition
            point = point.Subtract(-10, +5);

            CursorPosition.LineNumber = CursorRenderer.GetCursorLineFromPoint(point, SingleLineHeight, NumberOfRenderedLines, NumberOfStartLine);

            UpdateCurrentLineTextLayout();
            CursorPosition.CharacterPosition = CursorRenderer.GetCharacterPositionFromPoint(TotalLines.GetCurrentLineText(), CurrentLineTextLayout, point, (float)-HorizontalScroll);
        }
        private void UpdateCurrentLine()
        {
            TotalLines.UpdateCurrentLine(CursorPosition.LineNumber);
        }
        private void CheckRecalculateLongestLine(string text)
        {
            if (Utils.GetLongestLineLength(text) > LongestLineLength)
            {
                NeedsRecalculateLongestLineIndex = true;
            }
        }
        #endregion

        #region Textediting
        private void DeleteSelection()
        {
            if (TextSelection == null)
                return;

            //line gets deleted -> recalculate
            if (TextSelection.IsLineInSelection(LongestLineIndex))
            {
                NeedsRecalculateLongestLineIndex = true;
            }

            undoRedo.RecordUndoAction(() =>
            {
                CursorPosition = Selection.Remove(TextSelection, TotalLines);
                ClearSelection();

            }, TotalLines, TextSelection, 0, NewLineCharacter);
            UpdateSelection();
            UpdateCursor();
        }
        private void AddCharacter(string text, bool IgnoreSelection = false)
        {
            if (IsReadonly)
                return;

            if (IgnoreSelection)
                ClearSelection();

            int SplittedTextLength = text.Contains(NewLineCharacter) ? Utils.CountLines(text, NewLineCharacter) : 1;

            if (TextSelection == null && SplittedTextLength == 1)
            {
                undoRedo.RecordUndoAction(() =>
                {
                    var CharacterPos = GetCurPosInLine();

                    if (CharacterPos > TotalLines.CurrentLineLength() - 1)
                        CurrentLine = CurrentLine.AddToEnd(text);
                    else
                        CurrentLine = CurrentLine.AddText(text, CharacterPos);
                    CursorPosition.CharacterPosition = text.Length + CharacterPos;

                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);

                if (TotalLines.GetCurrentLineText().Length > LongestLineLength)
                {
                    LongestLineIndex = CursorPosition.LineNumber;
                }
            }
            else if (TextSelection == null && SplittedTextLength > 1)
            {
                CheckRecalculateLongestLine(text);
                undoRedo.RecordUndoAction(() =>
                {
                    CursorPosition = Selection.InsertText(TextSelection, CursorPosition, TotalLines, text, NewLineCharacter);
                }, TotalLines, CursorPosition.LineNumber, 1, SplittedTextLength, NewLineCharacter);
            }
            else if (text.Length == 0) //delete selection
            {
                DeleteSelection();
            }
            else if (TextSelection != null)
            {
                CheckRecalculateLongestLine(text);
                undoRedo.RecordUndoAction(() =>
                {
                    CursorPosition = Selection.Replace(TextSelection, TotalLines, text, NewLineCharacter);

                    ClearSelection();
                    UpdateSelection();
                }, TotalLines, TextSelection, SplittedTextLength, NewLineCharacter);
            }

            ScrollLineToCenter(CursorPosition.LineNumber);
            UpdateText();
            UpdateCursor();
            Internal_TextChanged();
        }
        private void RemoveText(bool ControlIsPressed = false)
        {
            UpdateCurrentLine();

            if (IsReadonly)
                return;

            if (TextSelection == null)
            {
                string curLine = CurrentLine;
                var charPos = GetCurPosInLine();
                var stepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveLeft(CurrentLine, charPos) : 1;
                if (charPos - stepsToMove >= 0)
                {
                    if (CursorPosition.LineNumber == LongestLineIndex)
                        NeedsRecalculateLongestLineIndex = true;

                    undoRedo.RecordUndoAction(() =>
                    {
                        CurrentLine = curLine.SafeRemove(charPos - stepsToMove, stepsToMove);
                        CursorPosition.CharacterPosition -= stepsToMove;

                    }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);
                }
                else if (charPos - stepsToMove < 0) //remove lines
                {
                    if (CursorPosition.LineNumber <= 0)
                        return;

                    if (CursorPosition.LineNumber == LongestLineIndex)
                        NeedsRecalculateLongestLineIndex = true;

                    undoRedo.RecordUndoAction(() =>
                    {
                        int curpos = TotalLines.GetLineLength(CursorPosition.LineNumber - 1);

                        //line still has text:
                        if (curLine.Length > 0)
                            TotalLines.String_AddToEnd(CursorPosition.LineNumber - 1, curLine);

                        TotalLines.DeleteAt(CursorPosition.LineNumber);

                        //update the cursorposition
                        CursorPosition.LineNumber -= 1;
                        CursorPosition.CharacterPosition = curpos;

                    }, TotalLines, CursorPosition.LineNumber - 1, 3, 2, NewLineCharacter);
                }
            }
            else
            {
                DeleteSelection();
            }

            UpdateScrollToShowCursor();
            UpdateText();
            UpdateCursor();
            Internal_TextChanged();
        }
        private void DeleteText(bool ControlIsPressed = false)
        {
            UpdateCurrentLine();
            string curLine = CurrentLine;

            if (IsReadonly)
                return;

            if (TextSelection == null)
            {
                int CharacterPos = GetCurPosInLine();
                //delete lines if cursor is at position 0 and the line is emty OR cursor is at the end of a line and the line has content
                if (CharacterPos == curLine.Length)
                {
                    string LineToAdd = CursorPosition.LineNumber + 1 < TotalLines.Count ? TotalLines.GetLineText(CursorPosition.LineNumber + 1) : null;
                    if (LineToAdd != null)
                    {
                        if (CursorPosition.LineNumber == LongestLineIndex)
                            NeedsRecalculateLongestLineIndex = true;

                        undoRedo.RecordUndoAction(() =>
                        {
                            int curpos = TotalLines.GetLineLength(CursorPosition.LineNumber);
                            CurrentLine += LineToAdd;
                            TotalLines.DeleteAt(CursorPosition.LineNumber + 1);

                            //update the cursorposition
                            CursorPosition.CharacterPosition = curpos;

                        }, TotalLines, CursorPosition.LineNumber, 2, 1, NewLineCharacter);
                    }
                }
                //delete text in line
                else if (TotalLines.Count > CursorPosition.LineNumber)
                {
                    int StepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveRight(curLine, CharacterPos) : 1;

                    if (CursorPosition.LineNumber == LongestLineIndex)
                        NeedsRecalculateLongestLineIndex = true;

                    undoRedo.RecordUndoAction(() =>
                    {
                        CurrentLine = curLine.SafeRemove(CharacterPos, StepsToMove);
                    }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);
                }
            }
            else
            {
                DeleteSelection();
            }

            UpdateScrollToShowCursor();
            UpdateText();
            UpdateCursor();
            Internal_TextChanged();
        }
        private void AddNewLine()
        {
            if (IsReadonly)
                return;

            if (TotalLines.Count == 0)
            {
                TotalLines.AddLine();
                return;
            }

            CursorPosition StartLinePos = new CursorPosition(TextSelection == null ? CursorPosition.ChangeLineNumber(CursorPosition, CursorPosition.LineNumber) : Selection.GetMin(TextSelection));

            //If the whole text is selected
            if (Selection.WholeTextSelected(TextSelection, TotalLines))
            {
                undoRedo.RecordUndoAction(() =>
                {
                    ListHelper.Clear(TotalLines, true);
                    TotalLines.InsertNewLine(-1);
                    CursorPosition = new CursorPosition(0, 1);
                }, TotalLines, 0, TotalLines.Count, 2, NewLineCharacter);
                ForceClearSelection();
                UpdateAll();
                Internal_TextChanged();
                return;
            }

            if (TextSelection == null) //No selection
            {
                string StartLine = TotalLines.GetLineText(StartLinePos.LineNumber);

                undoRedo.RecordUndoAction(() =>
                {
                    string[] SplittedLine = Utils.SplitAt(TotalLines.GetLineText(StartLinePos.LineNumber), StartLinePos.CharacterPosition);

                    TotalLines.SetLineText(StartLinePos.LineNumber, SplittedLine[1]);
                    TotalLines.InsertOrAdd(StartLinePos.LineNumber, SplittedLine[0]);

                }, TotalLines, StartLinePos.LineNumber, 1, 2, NewLineCharacter);

            }
            else //Any kind of selection
            {
                int remove = 2;
                if (TextSelection.StartPosition.LineNumber == TextSelection.EndPosition.LineNumber)
                {
                    //line is selected completely: remove = 1
                    if (Selection.GetMax(TextSelection.StartPosition, TextSelection.EndPosition).CharacterPosition == TotalLines.GetLineLength(CursorPosition.LineNumber) &&
                        Selection.GetMin(TextSelection.StartPosition, TextSelection.EndPosition).CharacterPosition == 0)
                    {
                        remove = 1;
                    }
                }

                undoRedo.RecordUndoAction(() =>
                {
                    CursorPosition = Selection.Replace(TextSelection, TotalLines, NewLineCharacter, NewLineCharacter);
                }, TotalLines, TextSelection, remove, NewLineCharacter);
            }

            ClearSelection();
            CursorPosition.LineNumber += 1;
            CursorPosition.CharacterPosition = 0;

            if (TextSelection == null && CursorPosition.LineNumber == NumberOfRenderedLines + NumberOfStartLine)
                ScrollOneLineDown();
            else
                UpdateScrollToShowCursor();

            UpdateAll();
            Internal_TextChanged();
        }
        #endregion

        #region Random functions
        private void InitialiseOnStart()
        {
            UpdateZoom();
            if (TotalLines.Count == 0)
            {
                TotalLines.AddLine();
            }
        }
        private void ClearSelectionIfNeeded()
        {
            //If the selection is visible, but is not getting set, clear the selection
            if (selectionrenderer.HasSelection && !selectionrenderer.IsSelecting)
            {
                ForceClearSelection();
            }
        }
        private void ForceClearSelection()
        {
            selectionrenderer.ClearSelection();
            TextSelection = null;
            UpdateSelection();
        }
        private void StartSelectionIfNeeded()
        {
            if (SelectionIsNull())
            {
                selectionrenderer.SelectionStartPosition = selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                TextSelection = new TextSelection(selectionrenderer.SelectionStartPosition, selectionrenderer.SelectionEndPosition);
            }
        }
        private bool SelectionIsNull()
        {
            if (TextSelection == null)
                return true;
            return selectionrenderer.SelectionStartPosition == null || selectionrenderer.SelectionEndPosition == null;
        }
        private void SelectSingleWord(CursorPosition CursorPosition)
        {
            int Characterpos = CursorPosition.CharacterPosition;
            //Update variables
            selectionrenderer.SelectionStartPosition =
                new CursorPosition(Characterpos - Cursor.CalculateStepsToMoveLeft2(CurrentLine, Characterpos), CursorPosition.LineNumber);

            selectionrenderer.SelectionEndPosition =
                new CursorPosition(Characterpos + Cursor.CalculateStepsToMoveRight2(CurrentLine, Characterpos), CursorPosition.LineNumber);

            CursorPosition.CharacterPosition = selectionrenderer.SelectionEndPosition.CharacterPosition;
            selectionrenderer.HasSelection = true;

            //Render it
            UpdateSelection();
            UpdateCursor();
        }
        private void DoDragDropSelection()
        {
            if (TextSelection == null || IsReadonly)
                return;

            //Position to insert is selection start or selection end -> no need to drag
            if (Cursor.Equals(TextSelection.StartPosition, CursorPosition) || Cursor.Equals(TextSelection.EndPosition, CursorPosition))
            {
                Utils.ChangeCursor(CoreCursorType.IBeam);
                DragDropSelection = false;
                return;
            }

            string TextToInsert = SelectedText;
            CursorPosition curpos = new CursorPosition(CursorPosition);

            //Delete the selection
            RemoveText();

            CursorPosition = curpos;

            AddCharacter(TextToInsert, false);

            Utils.ChangeCursor(CoreCursorType.IBeam);
            DragDropSelection = false;
            UpdateAll();
        }
        private void EndDragDropSelection(bool ClearSelectedText = true)
        {
            DragDropSelection = false;
            if (ClearSelectedText)
                ClearSelection();

            Utils.ChangeCursor(CoreCursorType.IBeam);
            selectionrenderer.IsSelecting = false;
            UpdateCursor();
        }
        private bool DragDropOverSelection(Point CurPos)
        {
            bool res = selectionrenderer.CursorIsInSelection(CursorPosition, TextSelection) ||
                selectionrenderer.PointerIsOverSelection(CurPos, TextSelection, DrawnTextLayout);

            Utils.ChangeCursor(res ? CoreCursorType.UniversalNo : CoreCursorType.IBeam);

            return res;
        }
        private void UpdateScrollToShowCursor(bool Update = true)
        {
            if (NumberOfStartLine + NumberOfRenderedLines <= CursorPosition.LineNumber)
                VerticalScrollbar.Value = (CursorPosition.LineNumber - NumberOfRenderedLines + 1) * SingleLineHeight / DefaultVerticalScrollSensitivity;
            else if (NumberOfStartLine > CursorPosition.LineNumber)
                VerticalScrollbar.Value = (CursorPosition.LineNumber - 1) * SingleLineHeight / DefaultVerticalScrollSensitivity;

            if (Update)
                UpdateAll();
        }
        private int GetCurPosInLine()
        {
            int curLineLength = CurrentLine.Length;

            if (CursorPosition.CharacterPosition > curLineLength)
                return curLineLength;
            return CursorPosition.CharacterPosition;
        }
        private void ScrollIntoViewHorizontal()
        {
            float CurPosInLine = CursorRenderer.GetCursorPositionInLine(CurrentLineTextLayout, CursorPosition, 0);
            if (CurPosInLine == OldHorizontalScrollValue)
                return;

            HorizontalScrollbar.Value = CurPosInLine - (Canvas_Text.ActualWidth - 10);
            OldHorizontalScrollValue = CurPosInLine;
        }
        private void SetFocus()
        {
            if (!HasFocus)
                GotFocus?.Invoke(this);
            HasFocus = true;
            EditContext.NotifyFocusEnter();
            inputPane.TryShow();
            Utils.ChangeCursor(CoreCursorType.IBeam);

            UpdateCursor();
        }
        private void RemoveFocus()
        {
            if (HasFocus)
                LostFocus?.Invoke(this);
            UpdateCursor();

            HasFocus = false;
            EditContext.NotifyFocusLeave();
            inputPane.TryHide();
            Utils.ChangeCursor(CoreCursorType.Arrow);
        }
        private void CreateColorResources(ICanvasResourceCreatorWithDpi resourceCreator)
        {
            if (ColorResourcesCreated)
                return;

            Canvas_LineNumber.ClearColor = _Design.LineNumberBackground;
            MainGrid.Background = _Design.Background;
            TextColorBrush = new CanvasSolidColorBrush(resourceCreator, _Design.TextColor);
            CursorColorBrush = new CanvasSolidColorBrush(resourceCreator, _Design.CursorColor);
            LineNumberColorBrush = new CanvasSolidColorBrush(resourceCreator, _Design.LineNumberColor);
            LineHighlighterBrush = new CanvasSolidColorBrush(resourceCreator, _Design.LineHighlighterColor);
            ColorResourcesCreated = true;
        }
        private void InitialiseTextService()
        {

            CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
            EditContext = manager.CreateEditContext();
            EditContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
            EditContext.InputScope = CoreTextInputScope.Text;
            EditContext.TextRequested += delegate { };//Event only needs to be added -> No need to do something else
            EditContext.SelectionRequested += delegate { };//Event only needs to be added -> No need to do something else
            EditContext.TextUpdating += EditContext_TextUpdating;
            EditContext.FocusRemoved += EditContext_FocusRemoved;
        }
        private void UpdateWhenScrolled()
        {
            //only update when a line was scrolled
            if ((int)(VerticalScrollbar.Value / SingleLineHeight) != NumberOfStartLine)
            {
                UpdateAll();
            }
        }

        private bool OutOfRenderedArea(int line)
        {
            //Check whether the current line is outside the bounds of the visible area
            return line < NumberOfStartLine || line >= NumberOfStartLine + NumberOfRenderedLines;
        }
        //Trys running the code and clears the memory if OutOfMemoryException gets thrown
        private async void Safe_Paste(bool HandleException = true)
        {
            try
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    string text = null;
                    try
                    {
                        text = await dataPackageView.GetTextAsync();
                    }
                    catch (Exception ex) //When longer holding Ctrl + V the clipboard may throw an exception:
                    {
                        Debug.WriteLine("Clipboard exception: " + ex.Message);
                        return;
                    }

                    if (await Utils.IsOverTextLimit(text.Length))
                        return;

                    AddCharacter(stringManager.CleanUpString(text));
                }
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_Paste(false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private string Safe_Gettext(bool HandleException = true)
        {
            try
            {
                return TotalLines.GetString(NewLineCharacter);
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    return Safe_Gettext(false);
                }
                throw new OutOfMemoryException();
            }
        }
        private void Safe_Cut(bool HandleException = true)
        {
            try
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(SelectedText);
                if (TextSelection == null)
                    DeleteLine(CursorPosition.LineNumber); //Delete the line
                else
                    DeleteText(); //Delete the selected text

                dataPackage.RequestedOperation = DataPackageOperation.Move;
                Clipboard.SetContent(dataPackage);
                ClearSelection();
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_Cut(false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private void Safe_Copy(bool HandleException = true)
        {
            try
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(SelectedText);
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(dataPackage);
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_Copy(false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private void Safe_LoadLines(IEnumerable<string> lines, LineEnding LineEnding = LineEnding.CRLF, bool HandleException = true)
        {
            try
            {
                selectionrenderer.ClearSelection();
                undoRedo.ClearAll();

                ListHelper.Clear(TotalLines);
                TotalLines.AddRange(lines);

                this.LineEnding = LineEnding;

                NeedsRecalculateLongestLineIndex = true;
                UpdateAll();
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_LoadLines(lines, LineEnding, false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private async void Safe_LoadText(string text, bool HandleException = true)
        {
            try
            {
                if (await Utils.IsOverTextLimit(text.Length))
                    return;

                //Get the LineEnding
                LineEnding = LineEndings.FindLineEnding(text);

                selectionrenderer.ClearSelection();
                undoRedo.ClearAll();

                NeedsRecalculateLongestLineIndex = true;

                if (text.Length == 0)
                    ListHelper.Clear(TotalLines, true);
                else
                    Selection.ReplaceLines(TotalLines, 0, TotalLines.Count, stringManager.CleanUpString(text).Split(NewLineCharacter));

                UpdateAll();
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_LoadText(text, false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private async void Safe_SetText(string text, bool HandleException = true)
        {
            try
            {
                if (await Utils.IsOverTextLimit(text.Length))
                    return;

                selectionrenderer.ClearSelection();
                NeedsRecalculateLongestLineIndex = true;
                undoRedo.RecordUndoAction(() =>
                {
                    Selection.ReplaceLines(TotalLines, 0, TotalLines.Count, stringManager.CleanUpString(text).Split(NewLineCharacter));
                    if (text.Length == 0) //Create a new line when the text gets cleared
                    {
                        TotalLines.AddLine();
                    }
                }, TotalLines, 0, TotalLines.Count, Utils.CountLines(text, NewLineCharacter), NewLineCharacter);

                UpdateAll();
            }
            catch (OutOfMemoryException)
            {
                if (HandleException)
                {
                    CleanUp();
                    Safe_SetText(text, false);
                    return;
                }
                throw new OutOfMemoryException();
            }
        }
        private void CleanUp()
        {
            Debug.WriteLine("Collect GC");
            ListHelper.GCList(TotalLines);
        }
        private void PointerReleasedAction(Point point)
        {
            OldTouchPosition = null;
            selectionrenderer.IsSelectingOverLinenumbers = false;

            //End text drag/drop -> insert text at cursorposition
            if (DragDropSelection && !DragDropOverSelection(point))
            {
                DoDragDropSelection();
            }
            else if (DragDropSelection)
            {
                EndDragDropSelection();
            }

            if (selectionrenderer.IsSelecting)
            {
                this.Focus(FocusState.Programmatic);
                selectionrenderer.HasSelection = true;
            }

            selectionrenderer.IsSelecting = false;
        }
        private void PointerMovedAction(Point point)
        {
            if (selectionrenderer.IsSelecting)
            {
                double CanvasWidth = Math.Round(this.ActualWidth, 2);
                double CanvasHeight = Math.Round(this.ActualHeight, 2);
                double CurPosX = Math.Round(point.X, 2);
                double CurPosY = Math.Round(point.Y, 2);

                if (CurPosY > CanvasHeight - 50)
                {
                    VerticalScrollbar.Value += (CurPosY > CanvasHeight + 30 ? 20 : (CanvasHeight - CurPosY) / 180);
                    UpdateWhenScrolled();
                }
                else if (CurPosY < 50)
                {
                    VerticalScrollbar.Value += CurPosY < -30 ? -20 : -(50 - CurPosY) / 20;
                    UpdateWhenScrolled();
                }

                //Horizontal
                if (CurPosX > CanvasWidth - 100)
                {
                    ScrollIntoViewHorizontal();
                    UpdateAll();
                }
                else if (CurPosX < 100)
                {
                    ScrollIntoViewHorizontal();
                    UpdateAll();
                }
            }

            //Drag drop text -> move the cursor to get the insertion point
            if (DragDropSelection)
            {
                DragDropOverSelection(point);
                UpdateCursorVariable(point);
                UpdateCursor();
            }
            if (selectionrenderer.IsSelecting && !DragDropSelection)
            {
                //selection started over the linenumbers:
                if (selectionrenderer.IsSelectingOverLinenumbers)
                {
                    Point pointerPos = point;
                    pointerPos.Y += SingleLineHeight; //add one more line

                    //When the selection reaches the end of the textbox select the last line completely
                    if (CursorPosition.LineNumber == TotalLines.Count - 1)
                    {
                        pointerPos.Y -= SingleLineHeight; //add one more line
                        pointerPos.X = Utils.MeasureLineLenght(CanvasDevice.GetSharedDevice(), TotalLines.GetLineText(-1), TextFormat).Width + 10;
                    }
                    UpdateCursorVariable(pointerPos);
                }
                else //Default selection
                    UpdateCursorVariable(point);

                //Update:
                UpdateCursor();
                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                UpdateSelection();
            }
        }
        private bool CheckTouchInput(PointerPoint point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Touch || point.PointerDevice.PointerDeviceType == PointerDeviceType.Pen)
            {
                //Get the touch start position:
                if (!OldTouchPosition.HasValue)
                    return true;

                //GEt the dragged offset:
                double scrollX = OldTouchPosition.Value.X - point.Position.X;
                double scrollY = OldTouchPosition.Value.Y - point.Position.Y;
                VerticalScrollbar.Value += scrollY > 2 ? 2 : scrollY < -2 ? -2 : scrollY;
                HorizontalScrollbar.Value += scrollX > 2 ? 2 : scrollX < -2 ? -2 : scrollX;
                UpdateAll();
                return true;
            }
            return false;
        }
        private bool CheckTouchInput_Click(PointerPoint point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Touch || point.PointerDevice.PointerDeviceType == PointerDeviceType.Pen)
            {
                OldTouchPosition = point.Position;
                return true;
            }
            return false;
        }


        #endregion

        #region Events
        //Handle keyinputs
        private void EditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
        {
            if (IsReadonly || args.Text == "\t")
                return;

            //Prevent key-entering if control key is pressed 
            var ctrl = Utils.IsKeyPressed(VirtualKey.Control);
            var menu = Utils.IsKeyPressed(VirtualKey.Menu);
            if (ctrl && !menu || menu && !ctrl)
                return;

            AddCharacter(args.Text);
        }
        private void TextControlBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                TextSelection Selection;
                if (Utils.IsKeyPressed(VirtualKey.Shift))
                    Selection = TabKey.MoveTabBack(TotalLines, TextSelection, CursorPosition, tabSpaceHelper.TabCharacter, NewLineCharacter, undoRedo);
                else
                    Selection = TabKey.MoveTab(TotalLines, TextSelection, CursorPosition, tabSpaceHelper.TabCharacter, NewLineCharacter, undoRedo);

                if (Selection != null)
                {
                    if (Selection.EndPosition == null)
                    {
                        CursorPosition = Selection.StartPosition;
                    }
                    else
                    {
                        selectionrenderer.SetSelection(Selection);
                        CursorPosition = Selection.EndPosition;
                    }
                }
                UpdateAll();

                //mark as handled to not change focus
                e.Handled = true;
            }
            if (!HasFocus)
                return;

            var ctrl = Utils.IsKeyPressed(VirtualKey.Control);
            var shift = Utils.IsKeyPressed(VirtualKey.Shift);
            var menu = Utils.IsKeyPressed(VirtualKey.Menu);
            if (ctrl && !shift && !menu)
            {
                switch (e.Key)
                {
                    case VirtualKey.Up:
                        ScrollOneLineUp();
                        break;
                    case VirtualKey.Down:
                        ScrollOneLineDown();
                        break;
                    case VirtualKey.V:
                        Paste();
                        break;
                    case VirtualKey.Z:
                        Undo();
                        break;
                    case VirtualKey.Y:
                        Redo();
                        break;
                    case VirtualKey.C:
                        Copy();
                        break;
                    case VirtualKey.X:
                        Cut();
                        break;
                    case VirtualKey.A:
                        SelectAll();
                        break;
                    case VirtualKey.W:
                        SelectSingleWord(CursorPosition);
                        break;
                }

                if (e.Key != VirtualKey.Left && e.Key != VirtualKey.Right && e.Key != VirtualKey.Back && e.Key != VirtualKey.Delete)
                    return;
            }

            if (menu)
            {
                if (e.Key == VirtualKey.Down || e.Key == VirtualKey.Up)
                {
                    var selection =
                        MoveLine.Move(
                            TotalLines,
                            TextSelection,
                            CursorPosition,
                            undoRedo,
                            NewLineCharacter,
                            e.Key == VirtualKey.Down ? MoveDirection.Down : MoveDirection.Up
                            );

                    if (e.Key == VirtualKey.Down)
                        ScrollOneLineDown();
                    else if (e.Key == VirtualKey.Up)
                        ScrollOneLineUp();

                    if (selection == null)
                        ForceClearSelection();
                    else
                    {
                        selectionrenderer.SetSelection(selection);
                    }
                    UpdateAll();
                    return;
                }
            }

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    AddNewLine();
                    break;
                case VirtualKey.Back:
                    RemoveText(ctrl);
                    break;
                case VirtualKey.Delete:
                    DeleteText(ctrl);
                    break;
                case VirtualKey.Left:
                    {
                        if (shift)
                        {
                            StartSelectionIfNeeded();
                            Cursor.MoveLeft(CursorPosition, TotalLines, CurrentLine);
                            selectionrenderer.SetSelectionEnd(CursorPosition);
                        }
                        else
                        {
                            //Move the cursor to the start of the selection
                            if (selectionrenderer.HasSelection && TextSelection != null)
                                CursorPosition = Selection.GetMin(TextSelection);
                            else
                                Cursor.MoveLeft(CursorPosition, TotalLines, CurrentLine);

                            ClearSelectionIfNeeded();
                        }

                        UpdateScrollToShowCursor();
                        UpdateText();
                        UpdateCursor();
                        UpdateSelection();
                        break;
                    }
                case VirtualKey.Right:
                    {
                        if (shift)
                        {
                            StartSelectionIfNeeded();
                            Cursor.MoveRight(CursorPosition, TotalLines, CurrentLine);
                            selectionrenderer.SetSelectionEnd(CursorPosition);
                        }
                        else
                        {
                            //Move the cursor to the end of the selection
                            if (selectionrenderer.HasSelection && TextSelection != null)
                                CursorPosition = Selection.GetMax(TextSelection);
                            else
                                Cursor.MoveRight(CursorPosition, TotalLines, TotalLines.GetCurrentLineText());

                            ClearSelectionIfNeeded();
                        }

                        UpdateScrollToShowCursor(false);
                        UpdateAll();
                        break;
                    }
                case VirtualKey.Down:
                    {
                        if (shift)
                        {
                            StartSelectionIfNeeded();
                            selectionrenderer.IsSelecting = true;
                            CursorPosition = selectionrenderer.SelectionEndPosition = Cursor.MoveDown(selectionrenderer.SelectionEndPosition, TotalLines.Count);
                            selectionrenderer.IsSelecting = false;
                        }
                        else
                        {
                            ClearSelectionIfNeeded();
                            CursorPosition = Cursor.MoveDown(CursorPosition, TotalLines.Count);
                        }

                        UpdateScrollToShowCursor(false);
                        UpdateAll();
                        break;
                    }
                case VirtualKey.Up:
                    {
                        if (shift)
                        {
                            StartSelectionIfNeeded();
                            selectionrenderer.IsSelecting = true;
                            CursorPosition = selectionrenderer.SelectionEndPosition = Cursor.MoveUp(selectionrenderer.SelectionEndPosition);
                            selectionrenderer.IsSelecting = false;
                        }
                        else
                        {
                            ClearSelectionIfNeeded();
                            CursorPosition = Cursor.MoveUp(CursorPosition);
                        }

                        UpdateScrollToShowCursor(false);
                        UpdateAll();
                        break;
                    }
                case VirtualKey.Escape:
                    {
                        EndDragDropSelection();
                        ClearSelection();
                        break;
                    }
                case VirtualKey.PageUp:
                    ScrollPageUp();
                    break;
                case VirtualKey.PageDown:
                    ScrollPageDown();
                    break;
                case VirtualKey.End:
                    {
                        if (shift)
                        {
                            selectionrenderer.HasSelection = true;

                            if (selectionrenderer.SelectionStartPosition == null)
                                selectionrenderer.SelectionStartPosition = new CursorPosition(CursorPosition);

                            Cursor.MoveToLineEnd(CursorPosition, CurrentLine);
                            selectionrenderer.SelectionEndPosition = CursorPosition;
                            UpdateSelection();
                            UpdateCursor();
                        }
                        else
                        {
                            Cursor.MoveToLineEnd(CursorPosition, CurrentLine);
                            UpdateCursor();
                            UpdateText();
                        }
                        break;
                    }
                case VirtualKey.Home:
                    {
                        if (shift)
                        {
                            selectionrenderer.HasSelection = true;

                            if (selectionrenderer.SelectionStartPosition == null)
                                selectionrenderer.SelectionStartPosition = new CursorPosition(CursorPosition);
                            Cursor.MoveToLineStart(CursorPosition);
                            selectionrenderer.SelectionEndPosition = CursorPosition;

                            UpdateSelection();
                            UpdateCursor();
                        }
                        else
                        {
                            Cursor.MoveToLineStart(CursorPosition);
                            UpdateCursor();
                            UpdateText();
                        }
                        break;
                    }
            }
        }
        //Pointer-events:

        //Need both the Canvas event and the CoreWindow event, because:
        //AppWindows does not handle CoreWindow events
        //Without coreWindow the selection outside of the window would not work
        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {

            var point = Utils.GetPointFromCoreWindowRelativeTo(args, Canvas_Text);
            PointerReleasedAction(point);
        }
        private void Canvas_Selection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            PointerReleasedAction(e.GetCurrentPoint(sender as UIElement).Position);
        }
        private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            if (CheckTouchInput(args.CurrentPoint))
                return;

            PointerMovedAction(Utils.GetPointFromCoreWindowRelativeTo(args, Canvas_Text));
        }
        private void Canvas_Selection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!HasFocus)
                return;

            var point = e.GetCurrentPoint(Canvas_Selection);

            if (CheckTouchInput(point))
                return;

            if (point.Properties.IsLeftButtonPressed)
            {
                selectionrenderer.IsSelecting = true;
            }
            PointerMovedAction(point.Position);

        }

        private void Canvas_Selection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            selectionrenderer.IsSelectingOverLinenumbers = false;

            var point = e.GetCurrentPoint(sender as UIElement);
            if (CheckTouchInput_Click(point))
                return;

            Point PointerPosition = point.Position;
            bool LeftButtonPressed = point.Properties.IsLeftButtonPressed;
            bool RightButtonPressed = point.Properties.IsRightButtonPressed;

            if (LeftButtonPressed && !Utils.IsKeyPressed(VirtualKey.Shift))
                PointerClickCount++;

            if (PointerClickCount == 3)
            {
                SelectLine(CursorPosition.LineNumber);
                PointerClickCount = 0;
                return;
            }
            else if (PointerClickCount == 2)
            {
                UpdateCursorVariable(PointerPosition);
                SelectSingleWord(CursorPosition);
            }
            else
            {
                //Show the onscreenkeyboard if no physical keyboard is attached
                inputPane.TryShow();

                //Show the contextflyout
                if (RightButtonPressed)
                {
                    if (!selectionrenderer.PointerIsOverSelection(PointerPosition, TextSelection, DrawnTextLayout))
                    {
                        ForceClearSelection();
                        UpdateCursorVariable(PointerPosition);
                    }

                    if (!ContextFlyoutDisabled && ContextFlyout != null)
                    {
                        ContextFlyout.ShowAt(sender as FrameworkElement, new FlyoutShowOptions { Position = PointerPosition });
                    }
                }

                //Shift + click = set selection
                if (Utils.IsKeyPressed(VirtualKey.Shift) && LeftButtonPressed)
                {
                    UpdateCursorVariable(PointerPosition);

                    selectionrenderer.SetSelectionEnd(new CursorPosition(CursorPosition));
                    UpdateSelection();
                    UpdateCursor();
                    return;
                }

                if (LeftButtonPressed)
                {
                    UpdateCursorVariable(PointerPosition);

                    //Text drag/drop
                    if (TextSelection != null)
                    {
                        if (selectionrenderer.PointerIsOverSelection(PointerPosition, TextSelection, DrawnTextLayout) && !DragDropSelection)
                        {
                            PointerClickCount = 0;
                            DragDropSelection = true;
                            Utils.ChangeCursor(CoreCursorType.UniversalNo);
                            return;
                        }
                        //End the selection by pressing on it
                        if (DragDropSelection && DragDropOverSelection(PointerPosition))
                        {
                            EndDragDropSelection(true);
                        }
                    }

                    //Clear the selection when pressing anywhere
                    if (selectionrenderer.HasSelection)
                    {
                        ForceClearSelection();
                        selectionrenderer.SelectionStartPosition = new CursorPosition(CursorPosition);
                    }
                    else
                    {
                        selectionrenderer.SetSelectionStart(new CursorPosition(CursorPosition));
                        selectionrenderer.IsSelecting = true;
                    }
                }
                UpdateCursor();
            }

            PointerClickTimer.Start();
            PointerClickTimer.Tick += (s, t) =>
            {
                PointerClickTimer.Stop();
                PointerClickCount = 0;
            };
        }
        private void Canvas_Selection_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            bool NeedsUpdate = false;
            //Zoom using mousewheel
            if (Utils.IsKeyPressed(VirtualKey.Control))
            {
                _ZoomFactor += delta / 20;
                UpdateZoom();
                return;
            }
            //Scroll horizontal using mousewheel
            else if (Utils.IsKeyPressed(VirtualKey.Shift))
            {
                HorizontalScrollbar.Value -= delta * HorizontalScrollSensitivity;
                NeedsUpdate = true;
            }
            //Scroll horizontal using touchpad
            else if (e.GetCurrentPoint(this).Properties.IsHorizontalMouseWheel)
            {
                HorizontalScrollbar.Value += delta * HorizontalScrollSensitivity;
                NeedsUpdate = true;
            }
            //Scroll vertical using mousewheel
            else
            {
                VerticalScrollbar.Value -= (delta * VerticalScrollSensitivity) / DefaultVerticalScrollSensitivity;
                //Only update when a line was scrolled
                if ((int)(VerticalScrollbar.Value / SingleLineHeight * DefaultVerticalScrollSensitivity) != NumberOfStartLine)
                {
                    NeedsUpdate = true;
                }
            }

            if (selectionrenderer.IsSelecting)
            {
                UpdateCursorVariable(e.GetCurrentPoint(Canvas_Selection).Position);
                UpdateCursor();

                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                NeedsUpdate = true;
            }
            if (NeedsUpdate)
                UpdateAll();
        }
        private void Canvas_LineNumber_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as UIElement);
            if (CheckTouchInput_Click(point))
                return;

            //Select the line where the cursor is over
            SelectLine(CursorRenderer.GetCursorLineFromPoint(point.Position, SingleLineHeight, NumberOfRenderedLines, NumberOfStartLine));

            selectionrenderer.IsSelecting = true;
            selectionrenderer.IsSelectingOverLinenumbers = true;
        }
        //Change the cursor when entering/leaving the control
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.IBeam);
        }
        private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.Arrow);
        }
        //Scrolling
        private void VerticalScrollbar_Loaded(object sender, RoutedEventArgs e)
        {
            VerticalScrollbar.Maximum = ((TotalLines.Count + 1) * SingleLineHeight - Scroll.ActualHeight) / DefaultVerticalScrollSensitivity;
            VerticalScrollbar.ViewportSize = this.ActualHeight;
        }
        private void HorizontalScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateAll();
        }
        private void VerticalScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            //only update when a line was scrolled
            if ((int)(VerticalScrollbar.Value / SingleLineHeight * DefaultVerticalScrollSensitivity) != NumberOfStartLine)
            {
                UpdateAll();
            }
        }
        //Canvas event
        private void Canvas_Text_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            //Create resources and layouts:
            if (NeedsTextFormatUpdate || TextFormat == null || LineNumberTextFormat == null)
            {
                if (_ShowLineNumbers)
                    LineNumberTextFormat = TextRenderer.CreateLinenumberTextFormat(ZoomedFontSize, FontFamily);
                TextFormat = TextRenderer.CreateCanvasTextFormat(ZoomedFontSize, FontFamily);
            }

            CreateColorResources(args.DrawingSession);

            //Measure textposition and apply the value to the scrollbar
            VerticalScrollbar.Maximum = ((TotalLines.Count + 1) * SingleLineHeight - Scroll.ActualHeight) / DefaultVerticalScrollSensitivity;
            VerticalScrollbar.ViewportSize = sender.ActualHeight;

            //Calculate number of lines that needs to be rendered
            NumberOfRenderedLines = (int)(sender.ActualHeight / SingleLineHeight);
            NumberOfStartLine = (int)((VerticalScrollbar.Value * DefaultVerticalScrollSensitivity) / SingleLineHeight);

            //Get all the lines, that need to be rendered, from the list
            NumberOfRenderedLines = NumberOfRenderedLines + NumberOfStartLine > TotalLines.Count ? TotalLines.Count : NumberOfRenderedLines;

            RenderedLines = TotalLines.GetLines_Small(NumberOfStartLine, NumberOfRenderedLines);
            RenderedText = RenderedLines.GetString("\n");

            if (_ShowLineNumbers)
            {
                for (int i = 0; i < NumberOfRenderedLines; i++)
                {
                    LineNumberContent.AppendLine((i + 1 + NumberOfStartLine).ToString());
                }
                LineNumberTextToRender = LineNumberContent.ToString();
                LineNumberContent.Clear();
            }

            //Get the longest line in the text:
            if (NeedsRecalculateLongestLineIndex)
            {
                NeedsRecalculateLongestLineIndex = false;
                Debug.WriteLine("recalculate...");
                LongestLineIndex = Utils.GetLongestLineIndex(TotalLines);
            }

            string longestLineText = TotalLines.GetLineText(LongestLineIndex);
            LongestLineLength = longestLineText.Length;
            Size LineLength = Utils.MeasureLineLenght(CanvasDevice.GetSharedDevice(), longestLineText, TextFormat);

            //Measure horizontal Width of longest line and apply to scrollbar
            HorizontalScrollbar.Maximum = (LineLength.Width <= sender.ActualWidth ? 0 : LineLength.Width - sender.ActualWidth + 50);
            HorizontalScrollbar.ViewportSize = sender.ActualWidth;
            ScrollIntoViewHorizontal();

            //Create the textlayout --> apply the Syntaxhighlighting --> render it:

            //Only update the textformat when the text changes:
            if (OldRenderedText != RenderedText || NeedsUpdateTextLayout)
            {
                NeedsUpdateTextLayout = false;
                OldRenderedText = RenderedText;

                DrawnTextLayout = TextRenderer.CreateTextResource(sender, DrawnTextLayout, TextFormat, RenderedText, new Size { Height = sender.Size.Height, Width = this.ActualWidth }, ZoomedFontSize);
                SyntaxHighlightingRenderer.UpdateSyntaxHighlighting(DrawnTextLayout, _AppTheme, _CodeLanguage, SyntaxHighlighting, RenderedText);
            }

            //render the search highlights
            if (SearchIsOpen)
                SearchHighlightsRenderer.RenderHighlights(args, DrawnTextLayout, RenderedText, searchHelper.MatchingSearchLines, searchHelper.SearchParameter.SearchExpression, (float)-HorizontalScroll, SingleLineHeight / DefaultVerticalScrollSensitivity, _Design.SearchHighlightColor);

            args.DrawingSession.DrawTextLayout(DrawnTextLayout, (float)-HorizontalScroll, SingleLineHeight, TextColorBrush);

            //Only update when old text != new text, to reduce updates when scrolling
            if (!OldLineNumberTextToRender.Equals(LineNumberTextToRender, StringComparison.OrdinalIgnoreCase))
            {
                OldLineNumberTextToRender = LineNumberTextToRender;
                Canvas_LineNumber.Invalidate();
            }
        }
        private void Canvas_Selection_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (selectionrenderer.SelectionStartPosition != null && selectionrenderer.SelectionEndPosition != null)
            {
                selectionrenderer.HasSelection =
                    !(selectionrenderer.SelectionStartPosition.LineNumber == selectionrenderer.SelectionEndPosition.LineNumber &&
                    selectionrenderer.SelectionStartPosition.CharacterPosition == selectionrenderer.SelectionEndPosition.CharacterPosition);
            }
            else
                selectionrenderer.HasSelection = false;

            if (selectionrenderer.HasSelection)
            {
                TextSelection = selectionrenderer.DrawSelection(DrawnTextLayout, RenderedLines, args, (float)-HorizontalScroll, SingleLineHeight / DefaultVerticalScrollSensitivity, NumberOfStartLine, NumberOfRenderedLines, ZoomedFontSize, _Design.SelectionColor);
            }

            if (TextSelection != null && !Selection.Equals(OldTextSelection, TextSelection))
            {
                //Update the variables
                OldTextSelection = new TextSelection(TextSelection);
                Internal_CursorChanged();
            }
        }
        private void Canvas_Cursor_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            UpdateCurrentLine();
            if (DrawnTextLayout == null || !HasFocus)
                return;

            int CurrentLineLength = CurrentLine.Length;

            if (CursorPosition.LineNumber >= TotalLines.Count)
            {
                CursorPosition.LineNumber = TotalLines.Count - 1;
                CursorPosition.CharacterPosition = CurrentLineLength;
            }

            //Calculate the distance to the top for the cursorposition and render the cursor
            float RenderPosY = (float)((CursorPosition.LineNumber - NumberOfStartLine) * SingleLineHeight) + SingleLineHeight / DefaultVerticalScrollSensitivity;

            //Out of display-region:
            if (RenderPosY > NumberOfRenderedLines * SingleLineHeight || RenderPosY < 0)
                return;

            UpdateCurrentLineTextLayout();

            int CharacterPos = CursorPosition.CharacterPosition;
            if (CharacterPos > CurrentLineLength)
                CharacterPos = CurrentLineLength;

            CursorRenderer.RenderCursor(
                CurrentLineTextLayout,
                CharacterPos,
                (float)-HorizontalScroll,
                RenderPosY, ZoomedFontSize,
                CursorSize,
                args,
                CursorColorBrush);


            if (_ShowLineHighlighter && SelectionIsNull())
            {
                LineHighlighter.Render((float)sender.ActualWidth, CurrentLineTextLayout, (float)-HorizontalScroll, RenderPosY, ZoomedFontSize, args, LineHighlighterBrush);
            }

            if (!Cursor.Equals(CursorPosition, OldCursorPosition))
            {
                OldCursorPosition = new CursorPosition(CursorPosition);
                Internal_CursorChanged();
            }
        }
        private void Canvas_LineNumber_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_ShowLineNumbers)
            {
                if (LineNumberTextToRender == null || LineNumberTextToRender.Length == 0)
                    return;

                //Calculate the linenumbers             
                float LineNumberWidth = (float)Utils.MeasureTextSize(CanvasDevice.GetSharedDevice(), (TotalLines.Count).ToString(), LineNumberTextFormat).Width;
                Canvas_LineNumber.Width = LineNumberWidth + 10 + SpaceBetweenLineNumberAndText;
            }
            else
            {
                Canvas_LineNumber.Width = SpaceBetweenLineNumberAndText;
                LineNumberTextToRender = null;
                return;
            }

            float posX = (float)sender.Size.Width - SpaceBetweenLineNumberAndText;
            if (posX < 0)
                posX = 0;

            CanvasTextLayout LineNumberLayout = TextRenderer.CreateTextLayout(sender, LineNumberTextFormat, LineNumberTextToRender, posX, (float)sender.Size.Height);
            args.DrawingSession.DrawTextLayout(LineNumberLayout, 10, SingleLineHeight, LineNumberColorBrush);
        }
        //Internal events:
        private void Internal_TextChanged()
        {
            //update the possible lines if the search is open
            if (searchHelper.IsSearchOpen)
                searchHelper.UpdateSearchLines(TotalLines);

            TextChanged?.Invoke(this);
        }
        private void Internal_CursorChanged()
        {
            Text.SelectionChangedEventHandler args = new Text.SelectionChangedEventHandler
            {
                CharacterPositionInLine = GetCurPosInLine() + 1,
                LineNumber = CursorPosition.LineNumber,
            };
            if (selectionrenderer.SelectionStartPosition != null && selectionrenderer.SelectionEndPosition != null)
            {
                var Sel = Selection.GetIndexOfSelection(TotalLines, new TextSelection(selectionrenderer.SelectionStartPosition, selectionrenderer.SelectionEndPosition));
                args.SelectionLength = Sel.Length;
                args.SelectionStartIndex = Sel.Index;
            }
            else
            {
                args.SelectionLength = 0;
                args.SelectionStartIndex = Cursor.CursorPositionToIndex(TotalLines, new CursorPosition { CharacterPosition = GetCurPosInLine(), LineNumber = CursorPosition.LineNumber });
            }
            SelectionChanged?.Invoke(this, args);
        }
        //Focus:
        private void UserControl_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            //Prevent the focus switching to the RootScrollViewer when double clicking.
            //It was the only way, I could think of.
            //https://stackoverflow.com/questions/74802534/double-tap-on-uwp-usercontrol-removes-focus
            if (args.NewFocusedElement is ScrollViewer sv && sv.Content is Border brd)
            {
                args.TryCancel();
            }
        }
        private void EditContext_FocusRemoved(CoreTextEditContext sender, object args)
        {
            RemoveFocus();
        }
        private void UserControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Programmatic);
        }
        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            SetFocus();
        }
        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveFocus();
        }
        //Cursor:
        private void Canvas_LineNumber_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.Arrow);
        }
        private void Canvas_LineNumber_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.IBeam);
        }
        private void Scrollbar_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.IBeam);
        }
        private void Scrollbar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Utils.ChangeCursor(CoreCursorType.Arrow);
        }
        //Drag Drop text
        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (IsReadonly)
                return;

            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                AddCharacter(stringManager.CleanUpString(await e.DataView.GetTextAsync()), true);
            }
        }
        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            if (selectionrenderer.IsSelecting || IsReadonly || !e.DataView.Contains(StandardDataFormats.Text))
                return;

            var deferral = e.GetDeferral();

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.IsContentVisible = false;
            deferral.Complete();

            UpdateCursorVariable(e.GetPosition(Canvas_Text));
            UpdateCursor();
        }
        #endregion

        #region Public functions and properties

        /// <summary>
        /// Selects a line specified by the index
        /// </summary>
        /// <param name="index">The index of the line to select</param>
        public void SelectLine(int index)
        {
            selectionrenderer.SetSelection(new CursorPosition(0, index), new CursorPosition(TotalLines.GetLineLength(index), index));
            CursorPosition = selectionrenderer.SelectionEndPosition;

            UpdateSelection();
            UpdateCursor();
        }

        /// <summary>
        /// Moves the cursor to the beginning of the line specified by the index
        /// </summary>
        /// <param name="index">The index of the line to go to</param>
        public void GoToLine(int index)
        {
            if (index >= TotalLines.Count || index < 0)
                return;

            selectionrenderer.SelectionEndPosition = null;
            CursorPosition = selectionrenderer.SelectionStartPosition = new CursorPosition(0, index);

            ScrollLineIntoView(index);
            this.Focus(FocusState.Programmatic);

            UpdateAll();
        }

        /// <summary>
        /// Load text to the textbox everything will reset. Use this to load text on application start
        /// Use "" to emty the textbox
        /// </summary>
        /// <param name="text">The text to load</param>
        public void LoadText(string text)
        {
            Safe_LoadText(text);
        }

        /// <summary>
        /// Load new text to the textbox an undo will be recorded. Use this to change the text when the app is running
        /// Use "" to emty the textbox
        /// </summary>
        /// <param name="text">The text to set</param>
        public void SetText(string text)
        {
            Safe_SetText(text);
        }

        /// <summary>
        /// Load lines to the textbox everything will reset. Use this to load text on application start
        /// Use this in combination with the FileIO.ReadLinesAsync function or similar
        /// This will drastically improve loading time and ram usage
        /// </summary>
        /// <param name="lines">A string array containing the lines</param>
        public void LoadLines(IEnumerable<string> lines, LineEnding lineEnding = LineEnding.CRLF)
        {
            Safe_LoadLines(lines, lineEnding);
        }

        /// <summary>
        /// Past the text from the clipboard to the current cursorposition
        /// </summary>
        public void Paste()
        {
            Safe_Paste();
        }

        /// <summary>
        /// Copies the selected text to the clipboard
        /// </summary>
        public void Copy()
        {
            Safe_Copy();
        }

        /// <summary>
        /// Deletes the selected text and copies it to the clipboard
        /// </summary>
        public void Cut()
        {
            Safe_Cut();
        }

        /// <summary>
        /// Gets the text from the textbox
        /// </summary>
        /// <returns>The text of the textbox</returns>
        public string GetText()
        {
            return Safe_Gettext();
        }

        /// <summary>
        /// Sets a selection
        /// </summary>
        /// <param name="start">The index to start the selection</param>
        /// <param name="length">The length of the selection</param>
        public void SetSelection(int start, int length)
        {
            var result = Selection.GetSelectionFromPosition(TotalLines, start, length, CharacterCount);
            if (result != null)
            {
                selectionrenderer.SetSelection(result.StartPosition, result.EndPosition);
                if (result.EndPosition != null)
                    CursorPosition = result.EndPosition;
            }

            UpdateSelection();
            UpdateCursor();
        }

        /// <summary>
        /// Selects the whole text
        /// </summary>
        public void SelectAll()
        {
            //No selection can be shown
            if (TotalLines.Count == 1 && TotalLines[0].Length == 0)
                return;
            selectionrenderer.SetSelection(new CursorPosition(0, 0), new CursorPosition(TotalLines.GetLineLength(-1), TotalLines.Count - 1));
            CursorPosition = selectionrenderer.SelectionEndPosition;
            UpdateSelection();
            UpdateCursor();
        }

        /// <summary>
        /// Clears the selection
        /// </summary>
        public void ClearSelection()
        {
            ForceClearSelection();
        }

        /// <summary>
        /// Undoes the last undo record
        /// </summary>
        public void Undo()
        {
            if (IsReadonly || !undoRedo.CanUndo)
                return;

            //Do the Undo
            Utils.ChangeCursor(CoreCursorType.Wait);
            var sel = undoRedo.Undo(TotalLines, stringManager, NewLineCharacter);
            Internal_TextChanged();
            Utils.ChangeCursor(CoreCursorType.IBeam);

            NeedsRecalculateLongestLineIndex = true;

            if (sel != null)
            {
                //only set cursorposition
                if (sel.StartPosition != null && sel.EndPosition == null)
                {
                    CursorPosition = sel.StartPosition;
                    UpdateAll();
                    return;
                }

                selectionrenderer.SetSelection(sel);
                CursorPosition = sel.EndPosition;
            }
            else
                ForceClearSelection();
            UpdateAll();
        }

        /// <summary>
        /// Redoes the last redo record
        /// </summary>
        public void Redo()
        {
            if (IsReadonly || !undoRedo.CanRedo)
                return;

            //Do the Redo
            Utils.ChangeCursor(CoreCursorType.Wait);
            var sel = undoRedo.Redo(TotalLines, stringManager, NewLineCharacter);
            Internal_TextChanged();
            Utils.ChangeCursor(CoreCursorType.IBeam);

            NeedsRecalculateLongestLineIndex = true;

            if (sel != null)
            {
                //only set cursorposition
                if (sel.StartPosition != null && sel.EndPosition == null)
                {
                    CursorPosition = sel.StartPosition;
                    UpdateAll();
                    return;
                }

                selectionrenderer.SetSelection(sel);
                CursorPosition = sel.EndPosition;
            }
            else
                ForceClearSelection();
            UpdateAll();
        }

        /// <summary>
        /// Scrolls the line to the center of the textbox if the line is out of rendered region
        /// </summary>
        /// <param name="index">The line to center</param>
        public void ScrollLineToCenter(int index)
        {
            if (OutOfRenderedArea(index))
                ScrollLineIntoView(index);
        }

        /// <summary>
        /// Scrolls one line up
        /// </summary>
        public void ScrollOneLineUp()
        {
            VerticalScrollbar.Value -= SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Scrolls one line down
        /// </summary>
        public void ScrollOneLineDown()
        {
            VerticalScrollbar.Value += SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Forces the line to scroll to center
        /// </summary>
        /// <param name="index">The line to center</param>
        public void ScrollLineIntoView(int index)
        {
            VerticalScrollbar.Value = (index - NumberOfRenderedLines / 2) * SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Scrolls the first line into view
        /// </summary>
        public void ScrollTopIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber - 1) * SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Scrolls the bottom line into view
        /// </summary>
        public void ScrollBottomIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber - NumberOfRenderedLines + 1) * SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Scrolls one page up (same as page up-key)
        /// </summary>
        public void ScrollPageUp()
        {
            CursorPosition.LineNumber -= NumberOfRenderedLines;
            if (CursorPosition.LineNumber < 0)
                CursorPosition.LineNumber = 0;

            VerticalScrollbar.Value -= NumberOfRenderedLines * SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Scrolls one page up (same as page down-key)
        /// </summary>
        public void ScrollPageDown()
        {
            CursorPosition.LineNumber += NumberOfRenderedLines;
            if (CursorPosition.LineNumber > TotalLines.Count - 1)
                CursorPosition.LineNumber = TotalLines.Count - 1;
            VerticalScrollbar.Value += NumberOfRenderedLines * SingleLineHeight / DefaultVerticalScrollSensitivity;
            UpdateAll();
        }

        /// <summary>
        /// Gets the content of the line specified by the index
        /// </summary>
        /// <param name="index">The index to get the content from</param>
        /// <returns>The text from the line specified by the index</returns>
        public string GetLineText(int index)
        {
            return TotalLines.GetLineText(index);
        }

        /// <summary>
        /// Gets the text of multiple lines, starting by start
        /// </summary>
        /// <param name="start">The line to start with</param>
        /// <param name="length">The number of lines to get</param>
        /// <returns>The text from the lines specified by index and count</returns>
        public string GetLinesText(int start, int length)
        {
            if (start + length >= TotalLines.Count)
                return TotalLines.GetString(NewLineCharacter);

            if (length > 200)
                return TotalLines.GetLines_Large(start, length).GetString(NewLineCharacter);
            return TotalLines.GetLines_Small(start, length).GetString(NewLineCharacter);
        }

        /// <summary>
        /// Sets the content of the line specified by the index. First line has the index 0
        /// </summary>
        /// <param name="index">The index of the line to change the content</param>
        /// <param name="text">The text to set to the line</param>
        /// <returns>Returns true if the text was changed successfully and false if the index was out of range</returns>
        public bool SetLineText(int index, string text)
        {
            if (index >= TotalLines.Count || index < 0)
                return false;

            if (text.Length > LongestLineLength)
                LongestLineIndex = index;

            undoRedo.RecordUndoAction(() =>
            {
                TotalLines.SetLineText(index, stringManager.CleanUpString(text));
            }, TotalLines, index, 1, 1, NewLineCharacter);
            UpdateText();
            return true;
        }

        /// <summary>
        /// Deletes the line from the textbox
        /// </summary>
        /// <param name="index">The line to delete</param>
        /// <returns>Returns true if the line was deleted successfully and false if the index is out of range</returns>
        public bool DeleteLine(int index)
        {
            if (index >= TotalLines.Count || index < 0)
                return false;

            if (index == LongestLineIndex)
                NeedsRecalculateLongestLineIndex = true;

            undoRedo.RecordUndoAction(() =>
            {
                TotalLines.RemoveAt(index);
            }, TotalLines, index, 2, 1, NewLineCharacter);

            UpdateText();
            return true;
        }

        /// <summary>
        /// Adds a new line with the text specified
        /// </summary>
        /// <param name="index">The position to insert the line to</param>
        /// <param name="text">The text to put in the new line</param>
        /// <returns>Returns true if the line was deleted successfully and false if the index was out of range</returns>
        public bool AddLine(int index, string text)
        {
            if (index > TotalLines.Count || index < 0)
                return false;

            if (text.Length > LongestLineLength)
                LongestLineIndex = index;

            undoRedo.RecordUndoAction(() =>
            {
                TotalLines.InsertOrAdd(index, stringManager.CleanUpString(text));

            }, TotalLines, index, 1, 2, NewLineCharacter);

            UpdateText();
            return true;
        }

        /// <summary>
        /// Surrounds the selection with the text specified by the value
        /// </summary>
        /// <param name="text">The text to surround the selection with</param>
        public void SurroundSelectionWith(string text)
        {
            text = stringManager.CleanUpString(text);
            SurroundSelectionWith(text, text);
        }

        /// <summary>
        /// Surround the selection with individual text for the left and right side.
        /// </summary>
        /// <param name="text1">The text for the left side</param>
        /// <param name="text2">The text for the right side</param>
        public void SurroundSelectionWith(string text1, string text2)
        {
            if (!SelectionIsNull())
            {
                AddCharacter(stringManager.CleanUpString(text1) + SelectedText + stringManager.CleanUpString(text2));
            }
        }

        /// <summary>
        /// Duplicates the line specified by the index into the next line
        /// </summary>
        /// <param name="index">The index of the line to duplicate</param>
        public void DuplicateLine(int index)
        {
            undoRedo.RecordUndoAction(() =>
            {
                TotalLines.InsertOrAdd(index, TotalLines.GetLineText(index));
                CursorPosition.LineNumber += 1;
            }, TotalLines, index, 1, 2, NewLineCharacter);

            if (OutOfRenderedArea(index))
                ScrollBottomIntoView();

            UpdateText();
            UpdateCursor();
        }
        /// <summary>
        /// Duplicates the line at the current cursor position
        /// </summary>
        public void DuplicateLine()
        {
            DuplicateLine(CursorPosition.LineNumber);
        }

        /// <summary>
        /// Replaces all occurences in the text with another word
        /// </summary>
        /// <param name="word">The word to search for</param>
        /// <param name="replaceWord">The word to replace with</param>
        /// <param name="matchCase">Search with case sensitivity</param>
        /// <param name="wholeWord">Search for whole words</param>
        /// <returns></returns>
        public SearchResult ReplaceAll(string word, string replaceWord, bool matchCase, bool wholeWord)
        {
            if (word.Length == 0 || replaceWord.Length == 0)
                return SearchResult.InvalidInput;

            SearchParameter searchParameter = new SearchParameter(word, wholeWord, matchCase);

            undoRedo.RecordUndoAction(() =>
            {
                for (int i = 0; i < TotalLines.Count; i++)
                {
                    if (TotalLines[i].Contains(searchParameter))
                    {
                        SetLineText(i, Regex.Replace(TotalLines[i], searchParameter.SearchExpression, replaceWord));
                    }
                }
            }, TotalLines, 0, TotalLines.Count, TotalLines.Count, NewLineCharacter);
            UpdateText();
            return SearchResult.ReachedEnd;
        }

        /// <summary>
        /// Searches for the next occurence. Call this after BeginSearch
        /// </summary>
        /// <returns>SearchResult.Found if the word was found</returns>
        public SearchResult FindNext()
        {
            if (!searchHelper.IsSearchOpen)
                return SearchResult.SearchNotOpened;

            var res = searchHelper.FindNext(TotalLines, CursorPosition);
            if (res.selection != null)
            {
                selectionrenderer.SetSelection(res.selection);
                ScrollLineIntoView(CursorPosition.LineNumber);
                UpdateText();
                UpdateSelection();
            }
            return res.result;
        }

        /// <summary>
        /// Searches for the previous occurence. Call this after BeginSearch
        /// </summary>
        /// <returns>SearchResult.Found if the word was found</returns>
        public SearchResult FindPrevious()
        {
            if (!searchHelper.IsSearchOpen)
                return SearchResult.SearchNotOpened;

            var res = searchHelper.FindPrevious(TotalLines, CursorPosition);
            if (res.selection != null)
            {
                selectionrenderer.SetSelection(res.selection);
                ScrollLineIntoView(CursorPosition.LineNumber);
                UpdateText();
                UpdateSelection();
            }
            return res.result;
        }

        /// <summary>
        /// Begins the search and highlights all occurences
        /// </summary>
        /// <param name="word">The word to search for</param>
        /// <param name="wholeWord">Search for a whole word</param>
        /// <param name="matchCase">Search for a case sensitive word</param>
        /// <returns></returns>
        public SearchResult BeginSearch(string word, bool wholeWord, bool matchCase)
        {
            var res = searchHelper.BeginSearch(TotalLines, word, wholeWord, matchCase);
            UpdateText();
            return res;
        }

        /// <summary>
        /// Ends the search removes the highlights
        /// </summary>
        public void EndSearch()
        {
            searchHelper.EndSearch();
            UpdateText();
        }

        /// <summary>
        /// Unloads the textbox and releases all resources
        /// Don't use it afterwards
        /// </summary>
        public void Unload()
        {
            EditContext.NotifyFocusLeave(); //inform the IME to not send any more text
            //Unsubscribe from events:
            this.KeyDown -= TextControlBox_KeyDown;
            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            EditContext.TextUpdating -= EditContext_TextUpdating;
            EditContext.FocusRemoved -= EditContext_FocusRemoved;

            //Dispose and null larger objects
            TotalLines.Dispose();
            RenderedLines = null;
            LineNumberTextToRender = RenderedText = null;
            LineNumberContent = null;
            undoRedo.NullAll();
        }

        /// <summary>
        /// Clears the undo and redo history
        /// </summary>
        public void ClearUndoRedoHistory()
        {
            undoRedo.ClearAll();
        }

        /// <summary>
        /// Get the cursorposition in pixels relative to the textbox
        /// </summary>
        /// <returns>The cursorposition in pixels</returns>
        public Point GetCursorPosition()
        {
            return new Point
            {
                Y = (float)((CursorPosition.LineNumber - NumberOfStartLine) * SingleLineHeight) + SingleLineHeight / DefaultVerticalScrollSensitivity,
                X = CursorRenderer.GetCursorPositionInLine(CurrentLineTextLayout, CursorPosition, 0)
            };
        }

        /// <summary>
        /// True to enable Syntaxhighlighting and false to disable it
        /// </summary>
        public bool SyntaxHighlighting { get; set; } = true;

        /// <summary>
        /// The Codelanguage to use for syntaxhighlighting
        /// </summary>
        public CodeLanguage CodeLanguage
        {
            get => _CodeLanguage;
            set
            {
                _CodeLanguage = value;
                NeedsUpdateTextLayout = true; //set to true to force update the textlayout
                UpdateText();
            }
        }

        /// <summary>
        /// The lineending to use for the text.
        /// It is detected automatically when loading text to the textbox and can be read from the variable. It can also be changed
        /// 
        /// </summary>
        public LineEnding LineEnding
        {
            get => _LineEnding;
            set
            {
                NewLineCharacter = LineEndings.LineEndingToString(value);
                stringManager.lineEnding = value;
                _LineEnding = value;
            }
        }

        /// <summary>
        /// The space between the linenumbers and the start of the text
        /// </summary>
        public float SpaceBetweenLineNumberAndText { get => _SpaceBetweenLineNumberAndText; set { _SpaceBetweenLineNumberAndText = value; NeedsUpdateLineNumbers(); UpdateAll(); } }

        /// <summary>
        /// Get or set the current position of the cursor
        /// </summary>
        public CursorPosition CursorPosition
        {
            get => _CursorPosition;
            set { _CursorPosition = new CursorPosition(value.CharacterPosition, value.LineNumber); UpdateCursor(); }
        }

        /// <summary>
        /// The fontfamily of the textbox
        /// </summary>
        public new FontFamily FontFamily { get => _FontFamily; set { _FontFamily = value; NeedsTextFormatUpdate = true; UpdateAll(); } }

        /// <summary>
        /// The fontsize of the textbox
        /// </summary>
        public new int FontSize { get => _FontSize; set { _FontSize = value; UpdateZoom(); } }

        /// <summary>
        /// Get the actual renderd size of the fontsize in pixels
        /// </summary>
        public float RenderedFontSize { get => ZoomedFontSize; }

        /// <summary>
        /// Get or set the text of the textbox
        /// </summary>
        public string Text { get => GetText(); set { SetText(value); } }

        /// <summary>
        /// Get or set the theme of the textbox
        /// </summary>
        public new ElementTheme RequestedTheme
        {
            get => _RequestedTheme;
            set
            {
                _RequestedTheme = value;
                _AppTheme = Utils.ConvertTheme(value);
                if (UseDefaultDesign)
                {
                    _Design = _AppTheme == ApplicationTheme.Light ? LightDesign : DarkDesign;
                }

                this.Background = _Design.Background;
                ColorResourcesCreated = false;
                NeedsUpdateTextLayout = true;
                UpdateAll();
            }
        }

        /// <summary>
        /// Get or set the design of the textbox returns null if the default design is used
        /// </summary>
        public TextControlBoxDesign Design
        {
            get => UseDefaultDesign ? null : _Design;
            set
            {
                if (value != null)
                {
                    UseDefaultDesign = false;
                    _Design = value;
                }
                else
                {
                    UseDefaultDesign = true;
                    _Design = _AppTheme == ApplicationTheme.Dark ? DarkDesign : LightDesign;
                }

                this.Background = _Design.Background;
                ColorResourcesCreated = false;
                UpdateAll();
            }
        }

        /// <summary>
        /// True to show, false to hide the linenumbers
        /// </summary>
        public bool ShowLineNumbers
        {
            get => _ShowLineNumbers;
            set
            {
                _ShowLineNumbers = value;
                NeedsUpdateTextLayout = true;
                NeedsUpdateLineNumbers();
                UpdateAll();
            }
        }

        /// <summary>
        /// True to show, false to hide the linehighlighter
        /// </summary>
        public bool ShowLineHighlighter
        {
            get => _ShowLineHighlighter;
            set { _ShowLineHighlighter = value; UpdateCursor(); }
        }

        /// <summary>
        /// Define the zoomfactor in percent; The default is 100%
        /// </summary>
        public int ZoomFactor { get => _ZoomFactor; set { _ZoomFactor = value; UpdateZoom(); } } //%

        /// <summary>
        /// False to allow changes to the textbox, true to lock the editing
        /// </summary>
        public bool IsReadonly { get => EditContext.IsReadOnly; set => EditContext.IsReadOnly = value; }

        /// <summary>
        /// Change the size and offset of the cursor. Use null for the default
        /// </summary>
        public CursorSize CursorSize { get => _CursorSize; set { _CursorSize = value; UpdateCursor(); } }

        /// <summary>
        /// Change the contextflyout of the textbox
        /// </summary>
        public new MenuFlyout ContextFlyout
        {
            get { return flyoutHelper.MenuFlyout; }
            set
            {
                //Use the builtin flyout
                if (value == null)
                {
                    flyoutHelper.CreateFlyout(this);
                }
                else //Use a custom flyout
                {
                    flyoutHelper.MenuFlyout = value;
                }
            }
        }

        /// <summary>
        /// True to disable the ContextFlyout, false to enable it
        /// </summary>
        public bool ContextFlyoutDisabled { get; set; }

        /// <summary>
        /// The characterposition of the selection start
        /// </summary>
        public int SelectionStart { get => selectionrenderer.SelectionStart; set { SetSelection(value, SelectionLength); } }

        /// <summary>
        /// The characterposition of the selection length
        /// </summary>
        public int SelectionLength { get => selectionrenderer.SelectionLength; set { SetSelection(SelectionStart, value); } }

        /// <summary>
        /// Get or set the selected text
        /// </summary>
        public string SelectedText
        {
            get
            {
                if (TextSelection != null && Selection.WholeTextSelected(TextSelection, TotalLines))
                {
                    return GetText();
                }

                return Selection.GetSelectedText(TotalLines, TextSelection, CursorPosition.LineNumber, NewLineCharacter);
            }
            set
            {
                AddCharacter(stringManager.CleanUpString(value));
            }
        }

        /// <summary>
        /// Get the number of lines in the textbox
        /// </summary>
        public int NumberOfLines { get => TotalLines.Count; }

        /// <summary>
        /// Get the line index of the cursor
        /// </summary>
        public int CurrentLineIndex { get => CursorPosition.LineNumber; }

        /// <summary>
        /// Get or set the scrollbar position
        /// </summary>
        public ScrollBarPosition ScrollBarPosition
        {
            get => new ScrollBarPosition(HorizontalScrollbar.Value, VerticalScroll);
            set { HorizontalScrollbar.Value = value.ValueX; VerticalScroll = value.ValueY; }
        }

        /// <summary>
        /// Get the number of characters in the textbox
        /// </summary>
        public int CharacterCount { get => Utils.CountCharacters(TotalLines); }

        /// <summary>
        /// Get or set the scroll sensitivity of the vertical scrollbar
        /// </summary>
        public double VerticalScrollSensitivity { get => _VerticalScrollSensitivity; set => _VerticalScrollSensitivity = value < 1 ? 1 : value; }

        /// <summary>
        /// Get or set the scroll sensitivity of the horizontal scrollbar
        /// </summary>
        public double HorizontalScrollSensitivity { get => _HorizontalScrollSensitivity; set => _HorizontalScrollSensitivity = value < 1 ? 1 : value; }

        /// <summary>
        /// Get the vertical scrollposition
        /// </summary>
        public double VerticalScroll { get => VerticalScrollbar.Value; set { VerticalScrollbar.Value = value < 0 ? 0 : value; UpdateAll(); } }

        /// <summary>
        /// Get the horizontal scrollposition
        /// </summary>
        public double HorizontalScroll { get => HorizontalScrollbar.Value; set { HorizontalScrollbar.Value = value < 0 ? 0 : value; UpdateAll(); } }

        /// <summary>
        /// The radius of the borders around the control
        /// </summary>
        public new CornerRadius CornerRadius { get => MainGrid.CornerRadius; set => MainGrid.CornerRadius = value; }

        /// <summary>
        /// Indicates whether to use spaces or tabs
        /// </summary>
        public bool UseSpacesInsteadTabs { get => tabSpaceHelper.UseSpacesInsteadTabs; set { tabSpaceHelper.UseSpacesInsteadTabs = value; tabSpaceHelper.UpdateTabs(TotalLines); UpdateAll(); } }

        /// <summary>
        /// The number of spaces to use instead of one tab
        /// </summary>
        public int NumberOfSpacesForTab { get => tabSpaceHelper.NumberOfSpaces; set { tabSpaceHelper.NumberOfSpaces = value; tabSpaceHelper.UpdateNumberOfSpaces(TotalLines); UpdateAll(); } }

        /// <summary>
        /// Get whether the search is currently active
        /// </summary>
        public bool SearchIsOpen { get => searchHelper.IsSearchOpen; }

        /// <summary>
        /// A reference to all the lines in the textbox.
        /// Use this to save the lines to a file using the FileIO.WriteLinesAsync function or similar
        /// This will drastically improve ram usage when saving
        /// </summary>
        public IEnumerable<string> Lines { get => TotalLines; }

        #endregion

        #region Public events

        /// <summary>
        /// Invokes when the text has changed
        /// </summary>
        /// <param name="sender">The textbox in which the text was changed</param>
        /// <param name="text">The text of the textbox</param>
        public delegate void TextChangedEvent(TextControlBox sender);
        public event TextChangedEvent TextChanged;

        /// <summary>
        /// Invokes when the selection has changed
        /// </summary>
        /// <param name="sender">The textbox in which the selection was changed</param>
        /// <param name="args">The new position of the cursor</param>
        public delegate void SelectionChangedEvent(TextControlBox sender, Text.SelectionChangedEventHandler args);
        public event SelectionChangedEvent SelectionChanged;

        /// <summary>
        /// Invokes when the zoom has changed
        /// </summary>
        /// <param name="sender">The textbox in which the zoom was changed</param>
        /// <param name="zoomFactor">The factor of the current zoom</param>
        public delegate void ZoomChangedEvent(TextControlBox sender, int zoomFactor);
        public event ZoomChangedEvent ZoomChanged;

        /// <summary>
        /// Invokes when the textbox received focus
        /// </summary>
        /// <param name="sender">The textbox that received focus</param>
        public delegate void GotFocusEvent(TextControlBox sender);
        public new event GotFocusEvent GotFocus;

        /// <summary>
        /// Invokes when the textbox lost focus
        /// </summary>
        /// <param name="sender">The textbox that lost focus</param>
        public delegate void LostFocusEvent(TextControlBox sender);
        public new event LostFocusEvent LostFocus;
        #endregion

        #region Static functions
        //static functions
        /// <summary>
        /// Get all the builtin code languages for syntaxhighlighting
        /// </summary>
        public static Dictionary<string, CodeLanguage> CodeLanguages => new Dictionary<string, CodeLanguage>(StringComparer.OrdinalIgnoreCase)
        {
            { "Batch", new Batch() },
            { "ConfigFile", new ConfigFile() },
            { "C++", new Cpp() },
            { "C#", new CSharp() },
            { "GCode", new GCode() },
            { "HexFile", new HexFile() },
            { "Html", new Html() },
            { "Java", new Java() },
            { "Javascript", new Javascript() },
            { "Json", new Json() },
            { "PHP", new PHP() },
            { "QSharp", new QSharp() },
            { "XML", new XML() },
        };

        /// <summary>
        /// Get a CodeLanguage by the identifier from the list
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns>When found the Codelanguage. Otherwise null</returns>
        public static CodeLanguage GetCodeLanguageFromId(string Identifier)
        {
            if (CodeLanguages.TryGetValue(Identifier, out CodeLanguage codelanguage))
                return codelanguage;
            return null;
        }

        /// <summary>
        /// Get a CodeLanguage class from the Json data
        /// </summary>
        /// <param name="Json">The data to build the class from</param>
        /// <returns>An instance of the JsonLoadResult with the CodeLanguage and a bool indicating whether the method succeed</returns>
        public static JsonLoadResult GetCodeLanguageFromJson(string Json)
        {
            return SyntaxHighlightingRenderer.GetCodeLanguageFromJson(Json);
        }

        #endregion
    }
    public class TextControlBoxDesign
    {
        public TextControlBoxDesign(TextControlBoxDesign Design)
        {
            this.Background = Design.Background;
            this.TextColor = Design.TextColor;
            this.SelectionColor = Design.SelectionColor;
            this.CursorColor = Design.CursorColor;
            this.LineHighlighterColor = Design.LineHighlighterColor;
            this.LineNumberColor = Design.LineNumberColor;
            this.LineNumberBackground = Design.LineNumberBackground;
        }

        /// <summary>
        /// Create a new instance of the TextControlBoxDesign class
        /// </summary>
        /// <param name="Background">The background color of the textbox</param>
        /// <param name="TextColor">The color of the text</param>
        /// <param name="SelectionColor">The color of the selection</param>
        /// <param name="CursorColor">The color of the cursor</param>
        /// <param name="LineHighlighterColor">The color of the linehighlighter</param>
        /// <param name="LineNumberColor">The color of the linenumber</param>
        /// <param name="LineNumberBackground">The background color of the linenumbers</param>
        public TextControlBoxDesign(Brush Background, Color TextColor, Color SelectionColor, Color CursorColor, Color LineHighlighterColor, Color LineNumberColor, Color LineNumberBackground, Color SearchHighlightColor)
        {
            this.Background = Background;
            this.TextColor = TextColor;
            this.SelectionColor = SelectionColor;
            this.CursorColor = CursorColor;
            this.LineHighlighterColor = LineHighlighterColor;
            this.LineNumberColor = LineNumberColor;
            this.LineNumberBackground = LineNumberBackground;
            this.SearchHighlightColor = SearchHighlightColor;
        }

        public Brush Background { get; set; }
        public Color TextColor { get; set; }
        public Color SelectionColor { get; set; }
        public Color CursorColor { get; set; }
        public Color LineHighlighterColor { get; set; }
        public Color LineNumberColor { get; set; }
        public Color LineNumberBackground { get; set; }
        public Color SearchHighlightColor { get; set; }
    }
    public class ScrollBarPosition
    {
        public ScrollBarPosition(ScrollBarPosition ScrollBarPosition)
        {
            this.ValueX = ScrollBarPosition.ValueX;
            this.ValueY = ScrollBarPosition.ValueY;
        }

        /// <summary>
        /// Creates a new instance of the ScrollBarPosition class
        /// </summary>
        /// <param name="ValueX">The horizontal amount scrolled</param>
        /// <param name="ValueY">The vertical amount scrolled</param>
        public ScrollBarPosition(double ValueX = 0, double ValueY = 0)
        {
            this.ValueX = ValueX;
            this.ValueY = ValueY;
        }

        /// <summary>
        /// The amount scrolled horizontally
        /// </summary>
        public double ValueX { get; set; }

        /// <summary>
        /// The amount scrolled vertically
        /// </summary>
        public double ValueY { get; set; }
    }
    public class CursorSize
    {
        /// <summary>
        /// Creates a new instance of the CursorSize class
        /// </summary>
        /// <param name="Width">The width of the cursor</param>
        /// <param name="Height">The height of the cursor</param>
        /// <param name="OffsetX">The x-offset from the actual cursor position</param>
        /// <param name="OffsetY">The y-offset from the actual cursor position</param>
        public CursorSize(float Width = 0, float Height = 0, float OffsetX = 0, float OffsetY = 0)
        {
            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;
        }

        /// <summary>
        /// The width of the cursor
        /// </summary>
        public float Width { get; private set; }

        /// <summary>
        /// The height of the cursor
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// The left/right offset from the actual cursor position
        /// </summary>
        public float OffsetX { get; private set; }

        /// <summary>
        /// The top/bottom offset from the actual cursor position
        /// </summary>
        public float OffsetY { get; private set; }
    }
}