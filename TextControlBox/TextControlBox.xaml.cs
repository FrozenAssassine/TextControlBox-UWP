using Collections.Pooled;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextControlBox.Extensions;
using TextControlBox.Helper;
using TextControlBox.Renderer;
using TextControlBox.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static System.Net.Mime.MediaTypeNames;
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
        private int _ZoomFactor = 101; //%
        private double _HorizontalScrollSensitivity = 1;
        private double _VerticalScrollSensitivity = 1;
        private float SingleLineHeight { get => TextFormat == null ? 0 : TextFormat.LineSpacing; }
        private float ZoomedFontSize = 0;
        private int MaxFontsize = 125;
        private int MinFontSize = 3;
        private int OldZoomFactor = 0;
        private int NumberOfStartLine = 0;
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
            Color.FromArgb(0, 0, 0, 0)
            );
        private TextControlBoxDesign DarkDesign = new TextControlBoxDesign(
            new SolidColorBrush(Color.FromArgb(0, 30, 30, 30)),
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(100, 0, 100, 255),
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(50, 100, 100, 100),
            Color.FromArgb(255, 100, 100, 100),
            Color.FromArgb(0, 0, 0, 0)
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

        CanvasTextFormat TextFormat = null;
        CanvasTextLayout DrawnTextLayout = null;
        CanvasTextFormat LineNumberTextFormat = null;

        string RenderedText = "";
        string LineNumberTextToRender = "";

        //Handle double and triple -clicks:
        int PointerClickCount = 0;
        DispatcherTimer PointerClickTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 200) };

        //CursorPosition
        CursorPosition _CursorPosition = new CursorPosition(0, 0);
        CursorPosition OldCursorPosition = null;
        Line CurrentLine = null;
        CanvasTextLayout CurrentLineTextLayout = null;
        TextSelection TextSelection = null;
        TextSelection OldTextSelection = null;
        CoreTextEditContext EditContext;

        //Store the lines in Lists
        private PooledList<Line> TotalLines = new PooledList<Line>(0);
        private List<Line> RenderedLines = new List<Line>(0);
        private int NumberOfRenderedLines = 0;
        StringBuilder LineNumberContent = new StringBuilder();
        StringBuilder TextToRender = new StringBuilder();

        //Classes
        private readonly SelectionRenderer selectionrenderer;
        private readonly UndoRedo undoRedo = new UndoRedo();
        private readonly FlyoutHelper flyoutHelper;
        private readonly TabSpaceHelper tabSpaceHelper = new TabSpaceHelper();
        private readonly StringManager stringManager;

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
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
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
                OldZoomFactor = _ZoomFactor;
                ZoomChanged?.Invoke(this, _ZoomFactor);
            }

            NeedsTextFormatUpdate = true;

            int Line = CursorPosition.LineNumber;
            //Check whether the current line is outside the bounds of the visible area
            if (Line < NumberOfStartLine || Line >= NumberOfStartLine + NumberOfRenderedLines)
            {
                VerticalScroll = (Line - NumberOfRenderedLines / 2) * SingleLineHeight;
            }
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
        private void UpdateCurrentLineTextLayout()
        {
            if (CursorPosition.LineNumber < TotalLines.Count)
                CurrentLineTextLayout = TextRenderer.CreateTextLayout(Canvas_Text, TextFormat, ListHelper.GetLine(TotalLines, CursorPosition.LineNumber).Content + "|", Canvas_Text.Size);
            else
                CurrentLineTextLayout = null;
        }
        private void UpdateCursorVariable(Point Point)
        {
            CursorPosition.LineNumber = CursorRenderer.GetCursorLineFromPoint(Point, SingleLineHeight, NumberOfRenderedLines, NumberOfStartLine);

            UpdateCurrentLineTextLayout();
            CursorPosition.CharacterPosition = CursorRenderer.GetCharacterPositionFromPoint(GetCurrentLine(), CurrentLineTextLayout, Point, (float)-HorizontalScroll);
        }
        #endregion

        #region Textediting
        private void DeleteSelection()
        {
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
            if (CurrentLine == null || IsReadonly)
                return;

            if (IgnoreSelection)
                ClearSelection();

            int SplittedTextLength = text.Contains(NewLineCharacter) ? text.NumberOfOccurences(NewLineCharacter) + 1 : 1;

            if (TextSelection == null && SplittedTextLength == 1)
            {
                undoRedo.RecordUndoAction(() =>
                {
                    var CharacterPos = GetCurPosInLine();

                    if (CharacterPos > CurrentLine.Length - 1)
                        CurrentLine.AddToEnd(text);
                    else
                        CurrentLine.AddText(text, CharacterPos);
                    CursorPosition.CharacterPosition = text.Length + CharacterPos;
                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);
            }
            else if (TextSelection == null && SplittedTextLength > 1)
            {
                undoRedo.RecordUndoAction(() =>
                {
                    CursorPosition = Selection.InsertText(TextSelection, CursorPosition, TotalLines, text, NewLineCharacter);
                }, TotalLines, CursorPosition.LineNumber, 1, SplittedTextLength, NewLineCharacter);
            }
            else if (text == "") //delete selection
            {
                DeleteSelection();
            }
            else if (TextSelection != null)
            {
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
            CurrentLine = GetCurrentLine();
            if (CurrentLine == null || IsReadonly)
                return;

            if (TextSelection == null)
            {
                var charPos = GetCurPosInLine();
                var stepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveLeft(CurrentLine, charPos) : 1;
                if (charPos - stepsToMove >= 0)
                {
                    undoRedo.RecordUndoAction(() =>
                    {
                        CurrentLine.Remove(charPos - stepsToMove, stepsToMove);
                        CursorPosition.CharacterPosition -= stepsToMove;

                    }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);
                }
                else if (charPos - stepsToMove < 0) //remove lines
                {
                    if (CursorPosition.LineNumber <= 0)
                        return;

                    undoRedo.RecordUndoAction(() =>
                    {
                        Line LineOnTop = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber - 1);
                        LineOnTop.AddToEnd(CurrentLine.Content);
                        TotalLines.Remove(CurrentLine);

                        CursorPosition.LineNumber -= 1;
                        CursorPosition.CharacterPosition = LineOnTop.Length - CurrentLine.Length;

                    }, TotalLines, CursorPosition.LineNumber, 3, 2, NewLineCharacter);
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
            if (CurrentLine == null || IsReadonly)
                return;

            if (TextSelection == null)
            {
                int CharacterPos = GetCurPosInLine();
                int StepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveRight(CurrentLine, CharacterPos) : 1;

                //delete lines if cursor in at position 0 and the line is emty OR cursor is at the end of a line and the line has content
                if (CharacterPos == CurrentLine.Length)
                {
                    Line LineToAdd = CursorPosition.LineNumber + 1 < TotalLines.Count ? ListHelper.GetLine(TotalLines, CursorPosition.LineNumber + 1) : null;
                    if (LineToAdd != null)
                    {
                        undoRedo.RecordUndoAction(() =>
                        {
                            CurrentLine.Content += LineToAdd.Content;
                            TotalLines.Remove(LineToAdd);
                        }, TotalLines, CursorPosition.LineNumber, 2, 1, NewLineCharacter);
                    }
                }
                //delete text in line
                else if (TotalLines.Count > CursorPosition.LineNumber)
                {
                    undoRedo.RecordUndoAction(() =>
                    {
                        CurrentLine.Remove(CharacterPos, StepsToMove);
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
                TotalLines.Add(new Line());
                return;
            }

            CursorPosition StartLinePos = new CursorPosition(TextSelection == null ? CursorPosition.ChangeLineNumber(CursorPosition, CursorPosition.LineNumber) : Selection.GetMin(TextSelection));

            //If the whole text is selected
            if (Selection.WholeTextSelected(TextSelection, TotalLines))
            {
                undoRedo.RecordUndoAction(() =>
                {
                    ListHelper.Clear(TotalLines, true);
                    ListHelper.Insert(TotalLines, new Line(), -1);
                    CursorPosition = new CursorPosition(0, 1);
                }, TotalLines, 0, TotalLines.Count, 2, NewLineCharacter);
                ForceClearSelection();
                UpdateAll();
                Internal_TextChanged();
                return;
            }

            if (TextSelection == null) //No selection
            {
                Line StartLine = ListHelper.GetLine(TotalLines, StartLinePos.LineNumber);
                Line EndLine = new Line();

                undoRedo.RecordUndoAction(() =>
                {
                    string[] SplittedLine = Utils.SplitAt(StartLine.Content, StartLinePos.CharacterPosition);
                    StartLine.SetText(SplittedLine[1]);
                    EndLine.SetText(SplittedLine[0]);

                    ListHelper.Insert(TotalLines, EndLine, StartLinePos.LineNumber);
                }, TotalLines, StartLinePos.LineNumber, 1, 2, NewLineCharacter);

            }
            else //Any kind of selection
            {
                undoRedo.RecordUndoAction(() =>
                {
                    CursorPosition = Selection.Replace(TextSelection, TotalLines, NewLineCharacter, NewLineCharacter);
                }, TotalLines, TextSelection, 2, NewLineCharacter);
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
                TotalLines.Add(new Line());
        }
        private Line GetCurrentLine()
        {
            return ListHelper.GetLine(TotalLines, CursorPosition.LineNumber);
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
                VerticalScrollbar.Value = (CursorPosition.LineNumber - NumberOfRenderedLines + 1) * SingleLineHeight;
            else if (NumberOfStartLine > CursorPosition.LineNumber)
                VerticalScrollbar.Value = (CursorPosition.LineNumber - 1) * SingleLineHeight;
            if (Update)
                UpdateAll();
        }
        private int GetCurPosInLine()
        {
            if (CursorPosition.CharacterPosition > CurrentLine.Length)
                return CurrentLine.Length;
            return CursorPosition.CharacterPosition;
        }
        private void ScrollIntoViewHorizontal()
        {
            float CurPosInLine = CursorRenderer.GetCursorPositionInLine(CurrentLineTextLayout, CursorPosition, 0);
            if (CurPosInLine == OldHorizontalScrollValue)
                return;

            HorizontalScroll = CurPosInLine - (Canvas_Text.ActualWidth - 10);
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
        }
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (!HasFocus)
                return;

            var ctrl = Utils.IsKeyPressed(VirtualKey.Control);
            var shift = Utils.IsKeyPressed(VirtualKey.Shift);
            if (ctrl && !shift)
            {
                switch (e.VirtualKey)
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

                if (e.VirtualKey != VirtualKey.Left && e.VirtualKey != VirtualKey.Right && e.VirtualKey != VirtualKey.Back && e.VirtualKey != VirtualKey.Delete)
                    return;
            }

            switch (e.VirtualKey)
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
                            selectionrenderer.IsSelecting = true;
                            CursorPosition = selectionrenderer.SelectionEndPosition = Cursor.MoveLeft(selectionrenderer.SelectionEndPosition, TotalLines, CurrentLine);
                            selectionrenderer.IsSelecting = false;
                        }
                        else
                        {
                            //Move the cursor to the start of the selection
                            if (selectionrenderer.HasSelection && TextSelection != null)
                                CursorPosition = Selection.GetMin(TextSelection);
                            else
                                CursorPosition = Cursor.MoveLeft(CursorPosition, TotalLines, CurrentLine);

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

                            selectionrenderer.IsSelecting = true;
                            CursorPosition = selectionrenderer.SelectionEndPosition = Cursor.MoveRight(selectionrenderer.SelectionEndPosition, TotalLines, CurrentLine);
                            selectionrenderer.IsSelecting = false;
                        }
                        else
                        {
                            //Move the cursor to the end of the selection
                            if (selectionrenderer.HasSelection && TextSelection != null)
                                CursorPosition = Selection.GetMax(TextSelection);
                            else
                                CursorPosition = Cursor.MoveRight(CursorPosition, TotalLines, GetCurrentLine());

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
                            selectionrenderer.SelectionEndPosition = Cursor.MoveToLineEnd(CursorPosition, CurrentLine);
                            UpdateSelection();
                            UpdateCursor();
                        }
                        else
                        {
                            CursorPosition = Cursor.MoveToLineEnd(CursorPosition, CurrentLine);
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
                            selectionrenderer.SelectionEndPosition = Cursor.MoveToLineStart(CursorPosition);
                            UpdateSelection();
                            UpdateCursor();
                        }
                        else
                        {
                            CursorPosition = Cursor.MoveToLineStart(CursorPosition);
                            UpdateCursor();
                            UpdateText();
                        }
                        break;
                    }
            }
        }
        //Pointer-events:
        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            var point = Utils.GetPointFromCoreWindowRelativeTo(args, Canvas_Text);
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
                selectionrenderer.HasSelection = true;

            selectionrenderer.IsSelecting = false;
        }
        private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            var point = Utils.GetPointFromCoreWindowRelativeTo(args, Canvas_Text);
            if (selectionrenderer.IsSelecting)
            {
                double CanvasWidth = Math.Round(this.ActualWidth, 2);
                double CanvasHeight = Math.Round(this.ActualHeight, 2);
                double CurPosX = Math.Round(point.X, 2);
                double CurPosY = Math.Round(point.Y, 2);

                if (CurPosY > CanvasHeight - 50)
                {
                    VerticalScroll += (CurPosY > CanvasHeight + 30 ? 20 : (CanvasHeight - CurPosY) / 150);
                    UpdateAll();
                }
                else if (CurPosY < 50)
                {
                    VerticalScroll += CurPosY < -30 ? -20 : -(50 - CurPosY) / 10;
                    UpdateAll();
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
                UpdateCursorVariable(point);
                UpdateCursor();

                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                UpdateSelection();
            }
        }
        private void Canvas_Selection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(Canvas_Selection);
            //Drag drop text -> move the cursor to get the insertion point
            if (DragDropSelection)
            {
                DragDropOverSelection(point.Position);
                UpdateCursorVariable(point.Position);
                UpdateCursor();
            }
            else if (point.Properties.IsLeftButtonPressed)
            {
                selectionrenderer.IsSelecting = true;
            }

            if (selectionrenderer.IsSelecting && !DragDropSelection)
            {
                UpdateCursorVariable(e.GetCurrentPoint(Canvas_Selection).Position);
                UpdateCursor();

                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                UpdateSelection();
            }
        }
        private void Canvas_Selection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Point PointerPosition = e.GetCurrentPoint(sender as UIElement).Position;
            bool LeftButtonPressed = e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed;
            bool RightButtonPressed = e.GetCurrentPoint(sender as UIElement).Properties.IsRightButtonPressed;

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
                VerticalScrollbar.Value -= delta * VerticalScrollSensitivity;
                //Only update when a line was scrolled
                if ((int)(VerticalScrollbar.Value / SingleLineHeight) != NumberOfStartLine)
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
        private void Canvas_LineNumber_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (selectionrenderer.IsSelecting)
            {
                var CurPoint = e.GetCurrentPoint(sender as UIElement).Position;
                CurPoint.X = 0; //Set 0 to select whole lines

                if (TextSelection == null)
                    return;

                if (TextSelection.StartPosition.LineNumber < TextSelection.EndPosition.LineNumber)
                {
                    CurPoint.Y += SingleLineHeight;
                }

                //Select the last line completely
                if (CurPoint.Y / SingleLineHeight > NumberOfRenderedLines)
                {
                    CurPoint.X = Utils.MeasureLineLenght(CanvasDevice.GetSharedDevice(), CurrentLine, TextFormat).Width + 100;
                }

                UpdateCursorVariable(CurPoint);
                UpdateCursor();

                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                UpdateSelection();
            }
        }
        private void Canvas_LineNumber_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //Select the line where the cursor is over
            SelectLine(CursorRenderer.GetCursorLineFromPoint(e.GetCurrentPoint(sender as UIElement).Position, SingleLineHeight, NumberOfRenderedLines, NumberOfStartLine));

            selectionrenderer.IsSelecting = true;
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
        private void HorizontalScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateAll();
        }
        private void VerticalScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            //only update when a line was scrolled
            if ((int)(VerticalScroll / SingleLineHeight) != NumberOfStartLine)
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
            VerticalScrollbar.Maximum = ((TotalLines.Count + 1) * SingleLineHeight - Scroll.ActualHeight);
            VerticalScrollbar.ViewportSize = sender.ActualHeight;

            //Calculate number of lines that needs to be rendered
            int NumberOfLinesToBeRendered = (int)(sender.ActualHeight / SingleLineHeight);
            NumberOfStartLine = (int)(VerticalScroll / SingleLineHeight);

            //Clear rendered lines, to fill it with new lines
            RenderedLines.Clear();

            //Get all the lines, that need to be rendered, from the list
            int count = NumberOfLinesToBeRendered + NumberOfStartLine > TotalLines.Count ? TotalLines.Count : NumberOfLinesToBeRendered + NumberOfStartLine;
            for (int i = NumberOfStartLine; i < count; i++)
            {
                if (i < TotalLines.Count)
                {
                    Line item = TotalLines[i];

                    RenderedLines.Add(item);
                    TextToRender.AppendLine(item.Content);
                    if (_ShowLineNumbers)
                        LineNumberContent.AppendLine((i + 1).ToString());
                }
            }
            NumberOfRenderedLines = RenderedLines.Count;

            if (_ShowLineNumbers)
                LineNumberTextToRender = LineNumberContent.ToString();
            RenderedText = TextToRender.ToString();

            //Clear the StringBuilder:
            TextToRender.Clear();
            LineNumberContent.Clear();

            //Get length of longest line in whole text
            Size LineLength = Utils.MeasureLineLenght(CanvasDevice.GetSharedDevice(), ListHelper.GetLine(TotalLines, Utils.GetLongestLineIndex(TotalLines)), TextFormat);
            //Measure horizontal Width of longest line and apply to scrollbar
            HorizontalScrollbar.Maximum = (LineLength.Width <= sender.ActualWidth ? 0 : LineLength.Width - sender.ActualWidth + 50);
            HorizontalScrollbar.ViewportSize = sender.ActualWidth;

            ScrollIntoViewHorizontal();

            //Create the textlayout --> apply the Syntaxhighlighting --> render it
            DrawnTextLayout = TextRenderer.CreateTextResource(sender, DrawnTextLayout, TextFormat, RenderedText, new Size { Height = sender.Size.Height, Width = this.ActualWidth }, ZoomedFontSize);
            SyntaxHighlightingRenderer.UpdateSyntaxHighlighting(DrawnTextLayout, _AppTheme, _CodeLanguage, SyntaxHighlighting, RenderedText);
            args.DrawingSession.DrawTextLayout(DrawnTextLayout, (float)-HorizontalScroll, SingleLineHeight, TextColorBrush);

            if (_ShowLineNumbers)
                Canvas_LineNumber.Invalidate();
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
                TextSelection = selectionrenderer.DrawSelection(DrawnTextLayout, RenderedLines, args, (float)-HorizontalScroll, SingleLineHeight / 4, NumberOfStartLine, NumberOfRenderedLines, ZoomedFontSize, _Design.SelectionColor);
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
            CurrentLine = GetCurrentLine();
            if (CurrentLine == null || DrawnTextLayout == null || !HasFocus)
                return;

            if (CursorPosition.LineNumber >= TotalLines.Count)
            {
                CursorPosition.LineNumber = TotalLines.Count - 1;
                CursorPosition.CharacterPosition = CurrentLine.Length;
            }

            //Calculate the distance to the top for the cursorposition and render the cursor
            float RenderPosY = (float)((CursorPosition.LineNumber - NumberOfStartLine) * SingleLineHeight) + SingleLineHeight / 4;

            //Out of display-region:
            if (RenderPosY > NumberOfRenderedLines * SingleLineHeight || RenderPosY < 0)
                return;

            UpdateCurrentLineTextLayout();

            int CharacterPos = CursorPosition.CharacterPosition;
            if (CharacterPos > CurrentLine.Length)
                CharacterPos = CurrentLine.Length;

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
            if (LineNumberTextToRender == null || LineNumberTextToRender.Length == 0)
                return;

            if (_ShowLineNumbers)
            {
                //Calculate the linenumbers             
                float LineNumberWidth = (float)Utils.MeasureTextSize(CanvasDevice.GetSharedDevice(), (TotalLines.Count).ToString(), LineNumberTextFormat).Width;
                Canvas_LineNumber.Width = LineNumberWidth + 10 + SpaceBetweenLineNumberAndText;
            }
            else
            {
                Canvas_LineNumber.Width = SpaceBetweenLineNumberAndText;
                return;
            }
            CanvasTextLayout LineNumberLayout = TextRenderer.CreateTextLayout(sender, LineNumberTextFormat, LineNumberTextToRender, (float)sender.Size.Width - SpaceBetweenLineNumberAndText, (float)sender.Size.Height);
            args.DrawingSession.DrawTextLayout(LineNumberLayout, 10, SingleLineHeight, LineNumberColorBrush);
        }
        //Internal events:
        private void Internal_TextChanged(string text = null)
        {
            TextChanged?.Invoke(this, text ?? GetText());
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
        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            //Check whether the cursor is inside the bounds of the Control
            if (Utils.GetElementRect(MainGrid).Contains(args.CurrentPoint.Position))
            {
                SetFocus();
            }
            else
            {
                RemoveFocus();
            }
        }
        private void EditContext_FocusRemoved(CoreTextEditContext sender, object args)
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
            if (selectionrenderer.IsSelecting || IsReadonly)
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

        //Trys running the code and clears the memory if OutOfMemoryException gets thrown
        private async void Safe_Paste(bool HandleException = true)
        {
            try
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    string Text = await dataPackageView.GetTextAsync();
                    if (await Utils.IsOverTextLimit(Text.Length))
                        return;

                    AddCharacter(stringManager.CleanUpString(Text));
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
                return string.Join(NewLineCharacter, TotalLines.Select(x => x.Content));
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
                Debug.WriteLine(SelectedText);
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
        private async void Safe_LoadText(string text, bool HandleException = true)
        {
            try
            {
                if (await Utils.IsOverTextLimit(text.Length))
                    return;

                ListHelper.Clear(TotalLines, text == "");
                RenderedLines.Clear();
                RenderedLines.TrimExcess();
                selectionrenderer.ClearSelection();
                undoRedo.ClearAll();

                //Get the LineEnding
                LineEnding = LineEndings.FindLineEnding(text);

                if (text == "")
                {
                    UpdateAll();
                    return;
                }

                //Split the lines using the current LineEnding
                var lines = stringManager.CleanUpString(text).Split(NewLineCharacter);
                using (PooledList<Line> LinesToAdd = new PooledList<Line>(lines.Length))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        LinesToAdd.Add(new Line(lines[i]));
                    }
                    TotalLines.AddRange(LinesToAdd);
                }

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

                string[] lines = null;
                if (text != "")
                {
                    lines = stringManager.CleanUpString(text).Split(NewLineCharacter);
                }

                undoRedo.RecordUndoAction(() =>
                {
                    //Clear the lists
                    ListHelper.Clear(TotalLines, text == "");
                    RenderedLines.Clear();
                    RenderedLines.TrimExcess();

                    if (text == "")
                    {
                        UpdateAll();
                        return;
                    }

                    for (int i = 0; i < lines.Length; i++)
                    {
                        TotalLines.Add(new Line(lines[i]));
                    }

                }, TotalLines, 0, TotalLines.Count, lines.Length, NewLineCharacter);

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
            GCSettings.LargeObjectHeapCompactionMode =
              GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #region Public functions and properties
        //Functions:
        /// <summary>
        /// Select the line specified by the index
        /// </summary>
        /// <param name="index">The index of the line to select</param>
        /// <param name="CursorAtStart">Select whether the cursor moves to start or end of the line</param>
        public void SelectLine(int index)
        {
            selectionrenderer.SetSelection(new CursorPosition(0, index), new CursorPosition(ListHelper.GetLine(TotalLines, index).Length, index));
            CursorPosition = selectionrenderer.SelectionEndPosition;

            UpdateSelection();
            UpdateCursor();
        }
        public void GoToLine(int index)
        {
            CursorPosition = selectionrenderer.SelectionEndPosition = selectionrenderer.SelectionStartPosition = new CursorPosition(0, index);

            UpdateSelection();
            UpdateCursor();
        }
        /// <summary>
        /// Load text to the textbox everything will reset. Use this to load text on application start
        /// </summary>
        /// <param name="text">The text to load</param>
        public void LoadText(string text)
        {
            Safe_LoadText(text);
        }
        /// <summary>
        /// Load new content to the textbox an undo will be recorded. Use this to change the text when the app is running
        /// </summary>
        /// <param name="text">The text to load</param>
        public void SetText(string text)
        {
            Safe_SetText(text);
        }
        public void Paste()
        {
            Safe_Paste();
        }
        public void Copy()
        {
            Safe_Copy();
        }
        public void Cut()
        {
            Safe_Cut();
        }
        public string GetText()
        {
            return Safe_Gettext();
        }
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
        public void SelectAll()
        {
            //No selection can be shown
            if (TotalLines.Count == 1 && TotalLines[0].Length == 0)
                return;
            selectionrenderer.SetSelection(new CursorPosition(0, 0), new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count - 1));
            CursorPosition = selectionrenderer.SelectionEndPosition;
            UpdateSelection();
            UpdateCursor();
        }
        public void ClearSelection()
        {
            ForceClearSelection();
        }
        public void Undo()
        {
            if (IsReadonly)
                return;

            //Do the Undo
            var sel = undoRedo.Undo(TotalLines, stringManager, NewLineCharacter);
            Internal_TextChanged();

            if (sel != null)
                selectionrenderer.SetSelection(sel.StartPosition, sel.EndPosition);
            else
                ForceClearSelection();
            UpdateAll();
        }
        public void Redo()
        {
            if (IsReadonly)
                return;

            //Do the Redo
            var sel = undoRedo.Redo(TotalLines, stringManager, NewLineCharacter);
            Internal_TextChanged();

            if (sel != null)
                selectionrenderer.SetSelection(sel);
            else
                ForceClearSelection();
            UpdateAll();
        }
        public void ScrollLineToCenter(int line)
        {
            //Check whether the current line is outside the bounds of the visible area
            if (line < NumberOfStartLine || line >= NumberOfStartLine + NumberOfRenderedLines)
            {
                ScrollLineIntoView(line);
            }
        }
        public void ScrollOneLineUp()
        {
            VerticalScroll -= SingleLineHeight;
            UpdateAll();
        }
        public void ScrollOneLineDown()
        {
            VerticalScroll += SingleLineHeight;
            UpdateAll();
        }
        public void ScrollLineIntoView(int line)
        {
            VerticalScroll = (line - NumberOfRenderedLines / 2) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollTopIntoView()
        {
            VerticalScroll = (CursorPosition.LineNumber - 1) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollBottomIntoView()
        {
            VerticalScroll = (CursorPosition.LineNumber - NumberOfRenderedLines + 1) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollPageUp()
        {
            CursorPosition.LineNumber -= NumberOfRenderedLines;
            if (CursorPosition.LineNumber < 0)
                CursorPosition.LineNumber = 0;

            VerticalScroll -= NumberOfRenderedLines * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollPageDown()
        {
            CursorPosition.LineNumber += NumberOfRenderedLines;
            if (CursorPosition.LineNumber > TotalLines.Count - 1)
                CursorPosition.LineNumber = TotalLines.Count - 1;
            VerticalScroll += NumberOfRenderedLines * SingleLineHeight;
            UpdateAll();
        }
        public string GetLineContent(int line)
        {
            return ListHelper.GetLine(TotalLines, line).Content;
        }
        public string GetLinesContent(int startLine, int count)
        {
            return ListHelper.GetLinesAsString(TotalLines, startLine, count, NewLineCharacter);
        }
        public bool SetLineContent(int line, string text)
        {
            if (line >= TotalLines.Count || line < 0)
                return false;
            undoRedo.RecordUndoAction(() =>
            {
                ListHelper.GetLine(TotalLines, line).Content = stringManager.CleanUpString(text);
            }, TotalLines, line, 1, 1, NewLineCharacter);
            UpdateText();
            return true;
        }
        public bool DeleteLine(int line)
        {
            if (line >= TotalLines.Count || line < 0)
                return false;

            undoRedo.RecordUndoAction(() =>
            {
                TotalLines.RemoveAt(line);
            }, TotalLines, line, 2, 1, NewLineCharacter);

            UpdateText();
            return true;
        }
        public bool AddLine(int position, string text)
        {
            if (position > TotalLines.Count || position < 0)
                return false;

            undoRedo.RecordUndoAction(() =>
            {
                ListHelper.Insert(TotalLines, new Line(stringManager.CleanUpString(text)), position);
            }, TotalLines, position, 1, 2, NewLineCharacter);

            UpdateText();
            return true;
        }
        public TextSelectionPosition FindInText(string pattern)
        {
            var pos = Regex.Match(GetText(), pattern);
            return new TextSelectionPosition(pos.Index, pos.Length);
        }
        public void SurroundSelectionWith(string value)
        {
            value = stringManager.CleanUpString(value);
            SurroundSelectionWith(value, value);
        }
        public void SurroundSelectionWith(string value1, string value2)
        {
            if (!SelectionIsNull())
            {
                AddCharacter(stringManager.CleanUpString(value1) + SelectedText + stringManager.CleanUpString(value2));
            }
        }
        public void DuplicateLine(int line)
        {
            undoRedo.RecordUndoAction(() =>
            {
                var content = new Line(ListHelper.GetLine(TotalLines, line).Content);
                ListHelper.Insert(TotalLines, content, line);
                CursorPosition.LineNumber += 1;
            }, TotalLines, line, 1, 2, NewLineCharacter);

            ScrollOneLineDown();
            UpdateText();
            UpdateCursor();
        }
        public bool FindInText(string Word, bool Up, bool MatchCase, bool WholeWord)
        {
            string Text = GetText();
            bool NotFound()
            {
                SetSelection(SelectionStart, 0);
                return false;
            }
            //Search down:
            if (!Up)
            {
                if (!MatchCase)
                {
                    Text = Text.ToLower();
                    Word = Word.ToLower();
                }
                if (SelectionStart == -1)
                {
                    SelectionStart = 0;
                }

                int startpos = SelectionStart;
                if (SelectionLength > 0)
                {
                    startpos = SelectionStart + SelectionLength;
                }
                if (Word.Length + startpos > Text.Length)
                {
                    return NotFound();
                }

                int index = WholeWord ? StringExtension.IndexOfWholeWord(Text, Word, startpos) : Text.IndexOf(Word, startpos);
                if (index == -1)
                {
                    return NotFound();
                }
                SetSelection(index, Word.Length);
                ScrollTopIntoView();
                return true;
            }
            else
            {
                try
                {
                    if (!MatchCase)
                    {
                        Text = Text.ToLower();
                        Word = Word.ToLower();
                    }
                    if (SelectionStart == -1)
                    {
                        SelectionStart = 0;
                    }

                    string shortedText = Text.Substring(0, SelectionStart);
                    int index = WholeWord ? StringExtension.LastIndexOfWholeWord(shortedText, Word) : shortedText.LastIndexOf(Word);
                    if (index == -1)
                    {
                        SetSelection(Text.Length, 0);
                        return NotFound();
                    }

                    SetSelection(index, Word.Length);
                    ScrollTopIntoView();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in TextControlBox --> FindInText:" + "\n" + ex.Message);
                    return false;
                }
            }
        }
        public bool ReplaceInText(string Word, string ReplaceWord, bool Up, bool MatchCase, bool WholeWord)
        {
            if (Word.Length == 0)
            {
                return false;
            }

            bool res = FindInText(Word, Up, MatchCase, WholeWord);
            if (res)
            {
                SelectedText = ReplaceWord;
            }

            return res;
        }
        public bool ReplaceAll(string Word, string ReplaceWord, bool Up, bool MatchCase, bool WholeWord)
        {

            if (Word.Length == 0)
            {
                return false;
            }

            int selstart = SelectionStart, sellenght = SelectionLength;

            if (!WholeWord)
            {
                SelectAll();
                if (MatchCase)
                {
                    SelectedText = GetText().Replace(Word, ReplaceWord);
                }
                else
                {
                    SelectedText = GetText().Replace(Word.ToLower(), ReplaceWord.ToLower());
                }

                return true;
            }

            SetSelection(SelectionStart, 0);
            bool res = true;
            while (res)
            {
                res = ReplaceInText(Word, ReplaceWord, Up, MatchCase, WholeWord);
            }

            SetSelection(selstart, sellenght);
            return true;
        }
        /// <summary>
        /// Change the current Codelanguage in the textbox to another with the given Identifier 
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public void Unload()
        {
            EditContext.NotifyFocusLeave(); //inform the IME to not send any more text
            //Unsubscribe from events:
            this.KeyDown -= TextControlBox_KeyDown;
            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            EditContext.TextUpdating -= EditContext_TextUpdating;
            EditContext.FocusRemoved -= EditContext_FocusRemoved;

            //Dispose and null larger objects
            TotalLines.Dispose();
            RenderedLines = null;
            LineNumberTextToRender = RenderedText = null;
            LineNumberContent = TextToRender = null;
            undoRedo.NullAll();
        }
        public void ClearUndoRedoHistory()
        {
            undoRedo.ClearAll();
        }

        //Properties:
        public bool SyntaxHighlighting { get; set; } = true;
        public CodeLanguage CodeLanguage
        {
            get => _CodeLanguage;
            set
            {
                _CodeLanguage = value;
                UpdateText();
            }
        }
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
        public float SpaceBetweenLineNumberAndText { get => _SpaceBetweenLineNumberAndText; set { _SpaceBetweenLineNumberAndText = value; UpdateText(); } }
        public CursorPosition CursorPosition
        {
            get => _CursorPosition;
            set { _CursorPosition = new CursorPosition(value.CharacterPosition, value.LineNumber); }
        }
        public new FontFamily FontFamily { get => _FontFamily; set { _FontFamily = value; NeedsTextFormatUpdate = true; UpdateAll(); } }
        public new int FontSize { get => _FontSize; set { _FontSize = value; UpdateZoom(); } }
        public float RenderedFontSize { get => ZoomedFontSize; }
        public string Text { get => GetText(); set { SetText(value); } }
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
                UpdateAll();
            }
        }
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

                //Debug.WriteLine(Design.TextColor + "::" + RequestedTheme + ":" + UseDefaultDesign);

                this.Background = _Design.Background;
                ColorResourcesCreated = false;
                UpdateAll();
            }
        }
        public bool ShowLineNumbers
        {
            get => _ShowLineNumbers;
            set
            {
                _ShowLineNumbers = value;
                UpdateAll();
            }
        }
        public bool ShowLineHighlighter
        {
            get => _ShowLineHighlighter;
            set { _ShowLineHighlighter = value; UpdateCursor(); }
        }
        public int ZoomFactor { get => _ZoomFactor; set { _ZoomFactor = value; UpdateZoom(); } } //%
        public bool IsReadonly { get => EditContext.IsReadOnly; set => EditContext.IsReadOnly = value; }
        [Description("Change the size of the cursor. Use null for the default size")]
        public CursorSize CursorSize { get => _CursorSize; set { _CursorSize = value; UpdateCursor(); } }
        public new MenuFlyout ContextFlyout
        {
            get { return flyoutHelper.MenuFlyout; }
            set
            {
                //Use the inbuild flyout
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
        public bool ContextFlyoutDisabled { get; set; }
        public int SelectionStart { get => selectionrenderer.SelectionStart; set { SetSelection(value, SelectionLength); } }
        public int SelectionLength { get => selectionrenderer.SelectionLength; set { SetSelection(SelectionStart, value); } }
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
        public int NumberOfLines { get => TotalLines.Count; }
        public int CurrentLineIndex { get => CursorPosition.LineNumber; }
        public ScrollBarPosition ScrollBarPosition
        {
            get => new ScrollBarPosition(HorizontalScrollbar.Value, VerticalScroll);
            set { HorizontalScrollbar.Value = value.ValueX; VerticalScroll = value.ValueY; }
        }
        public int CharacterCount { get => Utils.CountCharacters(TotalLines); }
        public double VerticalScrollSensitivity { get => _VerticalScrollSensitivity; set => _VerticalScrollSensitivity = value < 1 ? 1 : value; }
        public double HorizontalScrollSensitivity { get => _HorizontalScrollSensitivity; set => _HorizontalScrollSensitivity = value < 1 ? 1 : value; }
        public double VerticalScroll { get => VerticalScrollbar.Value; set => VerticalScrollbar.Value = value < 0 ? 0 : value; }
        public double HorizontalScroll { get => HorizontalScrollbar.Value; set => HorizontalScrollbar.Value = value < 0 ? 0 : value; }
        public new CornerRadius CornerRadius { get => MainGrid.CornerRadius; set => MainGrid.CornerRadius = value; }
        public bool UseSpacesInsteadTabs { get => tabSpaceHelper.UseSpacesInsteadTabs; set { tabSpaceHelper.UseSpacesInsteadTabs = value; tabSpaceHelper.UpdateTabs(TotalLines); } }
        public int NumberOfSpacesForTab { get => tabSpaceHelper.NumberOfSpaces; set { tabSpaceHelper.NumberOfSpaces = value; tabSpaceHelper.UpdateTabs(TotalLines); } }
        #endregion

        #region Public events
        //Events:
        public delegate void TextChangedEvent(TextControlBox sender, string Text);
        public event TextChangedEvent TextChanged;
        public delegate void SelectionChangedEvent(TextControlBox sender, Text.SelectionChangedEventHandler args);
        public event SelectionChangedEvent SelectionChanged;
        public delegate void ZoomChangedEvent(TextControlBox sender, int ZoomFactor);
        public event ZoomChangedEvent ZoomChanged;
        public delegate void GotFocusEvent(TextControlBox sender);
        public new event GotFocusEvent GotFocus;
        public delegate void LostFocusEvent(TextControlBox sender);
        public new event LostFocusEvent LostFocus;
        #endregion

        //static functions
        private static void GetLanguagesFromBuffer()
        {
            _CodeLanguages = new Dictionary<string, CodeLanguage>();

            var files = Directory.GetFiles("TextControlBox/Languages");
            for (int i = 0; i < files.Length; i++)
            {
                var codeLanguage = SyntaxHighlightingRenderer.GetCodeLanguageFromJson(File.ReadAllText(files[i]));
                _CodeLanguages.Add(codeLanguage.CodeLanguage.Name, codeLanguage.CodeLanguage);
            }
        }
        private static Dictionary<string, CodeLanguage> _CodeLanguages = null;

        public static Dictionary<string, CodeLanguage> CodeLanguages
        {
            get
            {
                if (_CodeLanguages == null)
                    GetLanguagesFromBuffer();

                return _CodeLanguages;
            }
        }
        public static CodeLanguage GetCodeLanguageFromId(string Identifier)
        {
            CodeLanguages.TryGetValue(Identifier, out CodeLanguage codelanguage);
            return codelanguage;
        }
        public JsonLoadResult GetCodeLanguageFromJson(string Json)
        {
            return SyntaxHighlightingRenderer.GetCodeLanguageFromJson(Json);
        }

    }
    public class TextControlBoxDesign
    {
        public TextControlBoxDesign()
        {

        }
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

        public TextControlBoxDesign(Brush Background, Color TextColor, Color SelectionColor, Color CursorColor, Color LineHighlighterColor, Color LineNumberColor, Color LineNumberBackground)
        {
            this.Background = Background;
            this.TextColor = TextColor;
            this.SelectionColor = SelectionColor;
            this.CursorColor = CursorColor;
            this.LineHighlighterColor = LineHighlighterColor;
            this.LineNumberColor = LineNumberColor;
            this.LineNumberBackground = LineNumberBackground;
        }
        public Brush Background { get; set; }
        public Color TextColor { get; set; }
        public Color SelectionColor { get; set; }
        public Color CursorColor { get; set; }
        public Color LineHighlighterColor { get; set; }
        public Color LineNumberColor { get; set; }
        public Color LineNumberBackground { get; set; }
    }
    public class ScrollBarPosition
    {
        public ScrollBarPosition(ScrollBarPosition ScrollBarPosition)
        {
            this.ValueX = ScrollBarPosition.ValueX;
            this.ValueY = ScrollBarPosition.ValueY;
        }
        public ScrollBarPosition(double ValueX = 0, double ValueY = 0)
        {
            this.ValueX = ValueX;
            this.ValueY = ValueY;
        }

        public double ValueX { get; set; }
        public double ValueY { get; set; }
    }
    public class CursorSize
    {
        public CursorSize(float Width = 0, float Height = 0, float OffsetX = 0, float OffsetY = 0)
        {
            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;
        }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }
    }
}