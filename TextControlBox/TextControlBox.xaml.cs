using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextControlBox.Extensions;
using TextControlBox.Helper;
using TextControlBox.Renderer;
using TextControlBox.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
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
        private Color _SelectionColor = Color.FromArgb(100, 0, 100, 255);
        private Color _TextColor = Color.FromArgb(255, 255, 255, 255);
        private Color _CursorColor = Color.FromArgb(255, 255, 255, 255);
        private Color _LineHighlighterColor = Color.FromArgb(50, 0, 0, 0);
        private Color _LineNumberColor = Color.FromArgb(255, 150, 150, 150);
        private Color _LineNumberBackground = Color.FromArgb(50, 80, 80, 80);
        private string NewLineCharacter = "\r\n";
        private string TabCharacter = "\t";
        private InputPane inputPane;
        private bool _ShowLineNumbers = true;
        private CodeLanguage _CodeLanguage = null;
        private CodeLanguages _CodeLanguages = CodeLanguages.None;
        private LineEnding _LineEnding = LineEnding.CRLF;
        private bool _ShowLineHighlighter = true;
        private int _FontSize = 18;
        private int _ZoomFactor = 101; //%

        float SingleLineHeight { get => TextFormat == null ? 0 : TextFormat.LineSpacing; }
        float ZoomedFontSize = 0;
        int MaxFontsize = 125;
        int MinFontSize = 3;
        int OldZoomFactor = 0;

        int NumberOfStartLine = 0;
        int NumberOfUnrenderedLinesToRenderStart = 0;
        float OldHorizontalScrollValue = 0;

        //Colors:
        CanvasSolidColorBrush TextColorBrush;
        CanvasSolidColorBrush CursorColorBrush;
        CanvasSolidColorBrush LineNumberColorBrush;
        CanvasSolidColorBrush LineHighlighterBrush;

        bool ColorResourcesCreated = false;
        bool NeedsTextFormatUpdate = false;
        bool GotKeyboardInput = false;
        bool DragDropSelection = false;
        bool HasFocus = false;

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
        private List<Line> TotalLines = new List<Line>();
        private List<Line> RenderedLines = new List<Line>();

        //Classes
        private readonly SelectionRenderer selectionrenderer;
        private readonly UndoRedo UndoRedo = new UndoRedo();
        private readonly FlyoutHelper FlyoutHelper;

        public TextControlBox()
        {
            this.InitializeComponent();
            CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
            EditContext = manager.CreateEditContext();
            EditContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
            EditContext.InputScope = CoreTextInputScope.Text;
            EditContext.TextRequested += delegate { };//Event only needs to be added -> No need to do something else
            EditContext.SelectionRequested += delegate { };//Event only needs to be added -> No need to do something else
            EditContext.TextUpdating += EditContext_TextUpdating;
            EditContext.FocusRemoved += EditContext_FocusRemoved;
            //Classes & Variables:
            selectionrenderer = new SelectionRenderer(SelectionColor);
            FlyoutHelper = new FlyoutHelper(this);
            inputPane = InputPane.GetForCurrentView();


            //Events:
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            InitialiseOnStart();
        }

        private void InitialiseOnStart()
        {
            UpdateZoom();
            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());
        }
        private void CreateColorResources(ICanvasResourceCreatorWithDpi resourceCreator)
        {
            if (ColorResourcesCreated)
                return;

            TextColorBrush = new CanvasSolidColorBrush(resourceCreator, TextColor);
            CursorColorBrush = new CanvasSolidColorBrush(resourceCreator, CursorColor);
            LineNumberColorBrush = new CanvasSolidColorBrush(resourceCreator, LineNumberColor);
            LineHighlighterBrush = new CanvasSolidColorBrush(resourceCreator, LineHighlighterColor);
            ColorResourcesCreated = true;
        }

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
            if (Line < NumberOfUnrenderedLinesToRenderStart || Line >= NumberOfUnrenderedLinesToRenderStart + RenderedLines.Count)
            {
                VerticalScrollbar.Value = (Line - RenderedLines.Count / 2) * SingleLineHeight;
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
            CursorPosition.LineNumber = CursorRenderer.GetCursorLineFromPoint(Point, SingleLineHeight, RenderedLines.Count, NumberOfStartLine);

            UpdateCurrentLineTextLayout();
            CursorPosition.CharacterPosition = CursorRenderer.GetCharacterPositionFromPoint(GetCurrentLine(), CurrentLineTextLayout, Point, (float)-HorizontalScrollbar.Value);
        }

        private void AddCharacter(string text, bool IgnoreSelection = false, bool ExcecutePrevUndoToo = false)
        {
            if (CurrentLine == null || IsReadonly)
                return;

            var SplittedTextLength = text.Split(NewLineCharacter).Length;

            if (IgnoreSelection)
                ClearSelection();

            //Nothing is selected
            if (TextSelection == null && SplittedTextLength == 1)
            {
                UndoRedo.RecordSingleLineUndo(CurrentLine.Content, CursorPosition.LineNumber, text == "");

                var CharacterPos = GetCurPosInLine();
                if (CharacterPos > CurrentLine.Length - 1)
                    CurrentLine.AddToEnd(text);
                else
                    CurrentLine.AddText(text, CharacterPos);
                CursorPosition.CharacterPosition = text.Length + CharacterPos;
            }
            else if (TextSelection == null && SplittedTextLength > 1)
            {
                int LineNumber = CursorPosition.LineNumber;
                string UndoText = ListHelper.GetLinesAsString(TotalLines, LineNumber, 2, NewLineCharacter);
                CursorPosition = Selection.InsertText(TextSelection, CursorPosition, TotalLines, text, NewLineCharacter);
                string RedoText = ListHelper.GetLinesAsString(TotalLines, LineNumber, SplittedTextLength, NewLineCharacter);
                UndoRedo.RecordMultiLineUndo(TotalLines, LineNumber, SplittedTextLength + 1, UndoText, RedoText, null, NewLineCharacter, false, false, ExcecutePrevUndoToo);
            }
            else
            {
                UndoRedo.RecordMultiLineUndo(TotalLines, CursorPosition.LineNumber, SplittedTextLength, text, TextSelection, NewLineCharacter, text == "");
                CursorPosition = Selection.Replace(TextSelection, TotalLines, text, NewLineCharacter);

                selectionrenderer.ClearSelection();
                TextSelection = null;
                UpdateSelection();
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
                int CharacterPos = GetCurPosInLine();
                int StepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveLeft(CurrentLine, CharacterPos) : 1;
                if (CharacterPos > 0)
                {
                    CurrentLine.Remove(CharacterPos - StepsToMove, StepsToMove);
                    CursorPosition.CharacterPosition -= StepsToMove;

                    UndoRedo.RecordSingleLineUndo(CurrentLine.Content, CursorPosition.LineNumber, true);
                }
                else if (CursorPosition.LineNumber > 0)
                {
                    UndoRedo.RecordMultiLineUndo(TotalLines, CursorPosition.LineNumber, 1, "", null, NewLineCharacter, true);
                    //Move the cursor one line up, if the beginning of the line is reached
                    Line LineOnTop = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber - 1);
                    LineOnTop.AddToEnd(CurrentLine.Content);
                    TotalLines.Remove(CurrentLine);
                    CursorPosition.LineNumber -= 1;
                    CursorPosition.CharacterPosition = LineOnTop.Length - CurrentLine.Length;
                }
            }
            else
            {
                AddCharacter(""); //Replace the selection with nothing
                ClearSelection();
                UpdateSelection();
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

                if (CharacterPos < CurrentLine.Length)
                {
                    UndoRedo.RecordSingleLineUndo(CurrentLine.Content, CursorPosition.LineNumber, true);
                    CurrentLine.Remove(CharacterPos, StepsToMove);
                }
                else if (TotalLines.Count > CursorPosition.LineNumber)
                {
                    Line LineToAdd = CursorPosition.LineNumber + 1 < TotalLines.Count ? ListHelper.GetLine(TotalLines, CursorPosition.LineNumber + 1) : null;
                    if (LineToAdd != null)
                    {
                        UndoRedo.RecordMultiLineUndo(TotalLines, CursorPosition.LineNumber, 1, "", null, NewLineCharacter, true);
                        CurrentLine.Content += LineToAdd.Content;
                        TotalLines.Remove(LineToAdd);
                    }
                }
            }
            else
            {
                AddCharacter(""); //Replace the selection with nothing
                ClearSelection();
                UpdateSelection();
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
                UndoRedo.RecordMultiLineUndo(TotalLines, StartLinePos.LineNumber, 2, "", TextSelection, NewLineCharacter, false);
                TotalLines.Clear();
                TotalLines.Add(new Line());
                CursorPosition = new CursorPosition(0, 1);

                ForceClearSelection();
                UpdateAll();
                Internal_TextChanged();
                return;
            }

            if (TextSelection == null) //No selection
            {
                Line StartLine = ListHelper.GetLine(TotalLines, StartLinePos.LineNumber);
                Line EndLine = new Line();

                //Record the state before the change
                string UndoText = StartLine.Content + NewLineCharacter + (StartLinePos.LineNumber + 1 > TotalLines.Count - 1 ? "" : ListHelper.GetLine(TotalLines, StartLinePos.LineNumber + 1).Content);

                string[] SplittedLine = Utils.SplitAt(StartLine.Content, StartLinePos.CharacterPosition);
                StartLine.SetText(SplittedLine[1]);
                EndLine.SetText(SplittedLine[0]);

                ListHelper.Insert(TotalLines, EndLine, StartLinePos.LineNumber);

                //Record the state after the change
                string RedoText = EndLine.Content + NewLineCharacter + StartLine.Content;
                UndoRedo.RecordNewLineUndo(TotalLines, StartLinePos.LineNumber, 2, UndoText, RedoText, TextSelection, NewLineCharacter);
            }
            else //Any kind of selection
            {
                AddCharacter(NewLineCharacter);
            }

            ClearSelection();
            CursorPosition.LineNumber += 1;
            CursorPosition.CharacterPosition = 0;

            if (TextSelection == null && CursorPosition.LineNumber == RenderedLines.Count + NumberOfUnrenderedLinesToRenderStart)
                ScrollOneLineDown();
            else
                UpdateScrollToShowCursor();

            UpdateAll();
            Internal_TextChanged();
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

            string TextToInsert = SelectedText;
            CursorPosition curpos = new CursorPosition(CursorPosition);

            //Delete the selection
            RemoveText();

            CursorPosition = curpos;

            AddCharacter(TextToInsert, false, true);

            Utils.ChangeCursor(CoreCursorType.IBeam);
            DragDropSelection = false;
            UpdateAll();
        }
        private void EndDragDropSelection()
        {
            DragDropSelection = false;
            ClearSelection();
            UpdateSelection();
            Utils.ChangeCursor(CoreCursorType.IBeam);
            selectionrenderer.IsSelecting = false;
            UpdateCursor();
        }
        private bool DragDropOverSelection(Point CurPos)
        {
            if (selectionrenderer.CursorIsInSelection(CursorPosition, TextSelection) ||
                selectionrenderer.PointerIsOverSelection(CurPos, TextSelection, DrawnTextLayout))
            {
                Utils.ChangeCursor(CoreCursorType.UniversalNo);
                return true;
            }
            else
            {
                Utils.ChangeCursor(CoreCursorType.IBeam);
                return false;
            }
        }
        private void UpdateScrollToShowCursor()
        {
            if (NumberOfStartLine + RenderedLines.Count <= CursorPosition.LineNumber)
                ScrollBottomIntoView();
            else if (NumberOfStartLine > CursorPosition.LineNumber)
                ScrollTopIntoView();
        }
        private async Task<bool> IsOverTextLimit(int TextLength)
        {
            if (TextLength > 100000000)
            {
                await new MessageDialog("Current textlimit is 100 million characters, but your file has " + TextLength + " characters").ShowAsync();
                return true;
            }
            return false;
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

            OldHorizontalScrollValue = CurPosInLine;
            HorizontalScrollbar.Value = CurPosInLine - (Canvas_Text.ActualWidth - 5);
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
        private void UpdateSyntaxHighlighting()
        {
            if (_CodeLanguage == null || !SyntaxHighlighting)
                return;

            var Highlights = _CodeLanguage.Highlights;
            for (int i = 0; i < Highlights.Count; i++)
            {
                var matches = Regex.Matches(RenderedText, Highlights[i].Pattern);
                for (int j = 0; j < matches.Count; j++)
                {
                    DrawnTextLayout.SetColor(matches[j].Index, matches[j].Length, Highlights[i].Color);
                }
            }
        }

        //Handle keyinputs
        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (!HasFocus)
                return;

            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
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
                            ClearSelectionIfNeeded();
                            CursorPosition = Cursor.MoveLeft(CursorPosition, TotalLines, CurrentLine);
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
                            ClearSelectionIfNeeded();
                            CursorPosition = Cursor.MoveRight(CursorPosition, TotalLines, GetCurrentLine());
                        }

                        UpdateScrollToShowCursor();
                        UpdateText();
                        UpdateCursor();
                        UpdateSelection();
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

                        UpdateScrollToShowCursor();
                        UpdateText();
                        UpdateCursor();
                        UpdateSelection();
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

                        UpdateScrollToShowCursor();
                        UpdateText();
                        UpdateCursor();
                        UpdateSelection();
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
                    CursorPosition = Cursor.MoveToLineEnd(CursorPosition, CurrentLine);
                    break;
                case VirtualKey.Home:
                    CursorPosition = Cursor.MoveToLineStart(CursorPosition);
                    break;
            }

            //Tab-key
            if (e.VirtualKey == VirtualKey.Tab)
            {
                TextSelection Selection;
                if (shift)
                    Selection = TabKey.MoveTabBack(TotalLines, TextSelection, CursorPosition, TabCharacter, NewLineCharacter, UndoRedo);
                else
                    Selection = TabKey.MoveTab(TotalLines, TextSelection, CursorPosition, TabCharacter, NewLineCharacter, UndoRedo);

                if (Selection != null)
                {
                    if (Selection.EndPosition == null)
                    {
                        CursorPosition = Selection.StartPosition;
                    }
                    else
                    {

                        selectionrenderer.SelectionStartPosition = Selection.StartPosition;
                        CursorPosition = selectionrenderer.SelectionEndPosition = Selection.EndPosition;
                        selectionrenderer.HasSelection = true;
                        selectionrenderer.IsSelecting = false;
                    }
                }
                UpdateText();
                UpdateSelection();
                UpdateCursor();
            }
        }
        private void EditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
        {
            if (IsReadonly)
                return;

            //Don't allow tab -> is handled different
            if (args.Text == "\t")
                return;

            //Prevent key-entering if control key is pressed 
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var menu = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrl && !menu || menu && !ctrl)
                return;

            if (!GotKeyboardInput)
            {
                //UndoRedo.RecordSingleLineUndo(CurrentLine, CursorPosition);
                GotKeyboardInput = true;
            }

            AddCharacter(args.Text);
        }
        //Pointer-events:
        private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            if (selectionrenderer.IsSelecting)
            {
                double CanvasWidth = Math.Round(this.ActualWidth, 2);
                double CanvasHeight = Math.Round(this.ActualHeight, 2);
                double CurPosX = Math.Round(args.CurrentPoint.Position.X, 2);
                double CurPosY = Math.Round(args.CurrentPoint.Position.Y, 2);

                if (CurPosY > CanvasHeight - 50)
                {
                    VerticalScrollbar.Value += (CurPosY > CanvasHeight + 30 ? 20 : (CanvasHeight - CurPosY) / 150);
                    UpdateAll();
                }
                else if (CurPosY < 50)
                {
                    VerticalScrollbar.Value += CurPosY < -30 ? -20 : -(50 - CurPosY) / 10;
                    UpdateAll();
                }

                //Horizontal
                if (CurPosX > CanvasWidth - 100)
                {
                    HorizontalScrollbar.Value += (CurPosX > CanvasWidth + 30 ? 20 : (CanvasWidth - CurPosX) / 150);
                    UpdateAll();
                }
                else if (CurPosX < 100)
                {
                    HorizontalScrollbar.Value += CurPosX < -30 ? -20 : -(100 - CurPosX) / 10;
                    UpdateAll();
                }
            }
        }
        private void Canvas_Selection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            //End text drag/drop -> insert text at cursorposition
            if (DragDropSelection && !DragDropOverSelection(e.GetCurrentPoint(sender as UIElement).Position))
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
        private void Canvas_Selection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //Drag drop text -> move the cursor to get the insertion point
            if (DragDropSelection)
            {
                DragDropOverSelection(e.GetCurrentPoint(sender as UIElement).Position);
                UpdateCursorVariable(e.GetCurrentPoint(Canvas_Selection).Position);
                UpdateCursor();
            }

            if (selectionrenderer.IsSelecting)
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

            if (LeftButtonPressed)
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
                    if (!ContextFlyoutDisabled && ContextFlyout != null)
                    {
                        ContextFlyout.ShowAt(sender as FrameworkElement, new FlyoutShowOptions { Position = PointerPosition });
                    }
                    else
                        return;
                }

                //Shift + click = set selection
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) && LeftButtonPressed)
                {
                    UpdateCursorVariable(PointerPosition);

                    selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                    selectionrenderer.HasSelection = true;
                    selectionrenderer.IsSelecting = false;
                    UpdateSelection();
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
                            EndDragDropSelection();
                        }
                    }

                    //Clear the selection when pressing anywhere
                    selectionrenderer.SelectionStartPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
                    if (selectionrenderer.HasSelection)
                    {
                        selectionrenderer.ClearSelection();
                        TextSelection = null;
                        UpdateSelection();
                    }
                    else
                    {
                        selectionrenderer.IsSelecting = true;
                        selectionrenderer.HasSelection = true;
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
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;

            //Zoom using mousewheel
            if (ctrl)
            {
                _ZoomFactor += delta / 20;
                UpdateZoom();
            }
            //Scroll horizontal using mousewheel
            else if (shift)
            {
                HorizontalScrollbar.Value -= delta;
            }
            //Scroll vertical using mousewheel
            else
            {
                VerticalScrollbar.Value -= delta;
            }

            if (selectionrenderer.IsSelecting)
            {
                UpdateCursorVariable(e.GetCurrentPoint(Canvas_Selection).Position);
                UpdateCursor();

                selectionrenderer.SelectionEndPosition = new CursorPosition(CursorPosition.CharacterPosition, CursorPosition.LineNumber);
            }
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
                if (CurPoint.Y / SingleLineHeight > RenderedLines.Count)
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
            selectionrenderer.IsSelecting = true;
            //Select the line where the cursor is over
            SelectLine(CursorRenderer.GetCursorLineFromPoint(e.GetCurrentPoint(sender as UIElement).Position, SingleLineHeight, RenderedLines.Count, NumberOfStartLine));
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
            UpdateAll();
        }
        //Canvas event
        private void Canvas_Text_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            //TEMPORARY:
            EditContext.NotifyFocusEnter();

            //Clear the rendered lines, to fill them with new lines
            RenderedLines.Clear();
            //Create resources and layouts:
            if (NeedsTextFormatUpdate || TextFormat == null)
            {
                if (_ShowLineNumbers)
                    LineNumberTextFormat = TextRenderer.CreateLinenumberTextFormat(ZoomedFontSize, FontFamily);
                TextFormat = TextRenderer.CreateCanvasTextFormat(ZoomedFontSize, FontFamily);
            }

            CreateColorResources(args.DrawingSession);

            //Calculate number of lines that needs to be rendered
            int NumberOfLinesToBeRendered = (int)(sender.ActualHeight / SingleLineHeight);
            NumberOfStartLine = (int)(VerticalScrollbar.Value / SingleLineHeight);

            NumberOfUnrenderedLinesToRenderStart = NumberOfStartLine;

            //Measure textposition and apply the value to the scrollbar
            VerticalScrollbar.Maximum = (TotalLines.Count + 1) * SingleLineHeight - Scroll.ActualHeight;
            VerticalScrollbar.ViewportSize = sender.ActualHeight;

            //Get all the lines, which needs to be rendered, from the list
            StringBuilder LineNumberContent = new StringBuilder();
            StringBuilder TextToRender = new StringBuilder();
            for (int i = NumberOfStartLine; i < NumberOfStartLine + NumberOfLinesToBeRendered; i++)
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

            if (_ShowLineNumbers)
                LineNumberTextToRender = LineNumberContent.ToString();

            RenderedText = TextToRender.ToString();
            //Clear the StringBuilder:
            TextToRender.Clear();

            //Get text from longest line in whole text
            Size LineLength = Utils.MeasureLineLenght(CanvasDevice.GetSharedDevice(), TotalLines[Utils.GetLongestLineIndex(TotalLines)], TextFormat);

            //Measure horizontal Width of longest line and apply to scrollbar
            HorizontalScrollbar.Maximum = LineLength.Width <= sender.ActualWidth ? 0 : LineLength.Width - sender.ActualWidth + 50;
            HorizontalScrollbar.ViewportSize = sender.ActualWidth;

            ScrollIntoViewHorizontal();

            //Create the textlayout --> apply the Syntaxhighlighting --> render it
            DrawnTextLayout = TextRenderer.CreateTextResource(sender, DrawnTextLayout, TextFormat, RenderedText, new Size { Height = sender.Size.Height, Width = this.ActualWidth }, ZoomedFontSize);
            UpdateSyntaxHighlighting();
            args.DrawingSession.DrawTextLayout(DrawnTextLayout, (float)-HorizontalScrollbar.Value, SingleLineHeight, TextColorBrush);

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
                TextSelection = selectionrenderer.DrawSelection(DrawnTextLayout, RenderedLines, args, (float)-HorizontalScrollbar.Value, SingleLineHeight / 4, NumberOfUnrenderedLinesToRenderStart, RenderedLines.Count, ZoomedFontSize);
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

            if (CursorPosition.LineNumber > TotalLines.Count)
                CursorPosition.LineNumber = TotalLines.Count - 1;

            //Calculate the distance to the top for the cursorposition and render the cursor
            float RenderPosY = (float)((CursorPosition.LineNumber - NumberOfUnrenderedLinesToRenderStart) * SingleLineHeight) + SingleLineHeight / 4;

            //Out of display-region:
            if (RenderPosY > RenderedLines.Count * SingleLineHeight || RenderPosY < 0)
                return;

            UpdateCurrentLineTextLayout();

            int CharacterPos = CursorPosition.CharacterPosition;
            if (CharacterPos > CurrentLine.Length)
                CharacterPos = CurrentLine.Length;

            CursorRenderer.RenderCursor(
                CurrentLineTextLayout,
                CharacterPos,
                (float)-HorizontalScrollbar.Value,
                RenderPosY, ZoomedFontSize,
                CursorSize,
                args,
                CursorColorBrush);

            if (_ShowLineHighlighter && SelectionIsNull())
            {
                LineHighlighter.Render((float)sender.ActualWidth, CurrentLineTextLayout, (float)-HorizontalScrollbar.Value, RenderPosY, ZoomedFontSize, args, LineHighlighterBrush);
            }

            if (!Cursor.Equals(CursorPosition, OldCursorPosition))
            {
                OldCursorPosition = new CursorPosition(CursorPosition);
                Internal_CursorChanged();
            }
        }
        private void Canvas_LineNumber_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (LineNumberTextToRender.Length == 0)
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
            args.DrawingSession.FillRectangle(0, 0, (float)sender.ActualWidth, (float)sender.ActualHeight, _LineNumberBackground);
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
            Rect contentRect = Utils.GetElementRect(MainGrid);
            if (contentRect.Contains(args.CurrentPoint.Position))
            {
                SetFocus();
                Focus(FocusState.Programmatic);
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
        private void ScrollbarPointerExited(object sender, PointerRoutedEventArgs e)
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
                string text = await e.DataView.GetTextAsync();
                text = LineEndings.ChangeLineEndings(text, _LineEnding);
                AddCharacter(text, true);
                UpdateText();
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


        //Functions:
        /// <summary>
        /// Select the line specified by the index
        /// </summary>
        /// <param name="index">The index of the line to select</param>
        /// <param name="CursorAtStart">Select whether the cursor moves to start or end of the line</param>
        public void SelectLine(int index)
        {
            selectionrenderer.SelectionStartPosition = new CursorPosition(0, index);
            CursorPosition = selectionrenderer.SelectionEndPosition = new CursorPosition(ListHelper.GetLine(TotalLines, index).Length, index);

            UpdateSelection();
            UpdateCursor();
        }
        public void GoToLine(int index)
        {
            CursorPosition = selectionrenderer.SelectionEndPosition = selectionrenderer.SelectionStartPosition = new CursorPosition(0, index);

            UpdateSelection();
            UpdateCursor();
        }
        public async void SetText(string text)
        {
            if (await IsOverTextLimit(text.Length))
                return;

            TotalLines.Clear();
            RenderedLines.Clear();
            UndoRedo.ClearAll();
            selectionrenderer.ClearSelection();

            //Get the LineEnding
            LineEnding = LineEndings.FindLineEnding(text);

            //Split the lines by the current LineEnding
            var lines = text.Split(NewLineCharacter);
            for (int i = 0; i < lines.Length; i++)
            {
                TotalLines.Add(new Line(lines[i]));
            }

            Debug.WriteLine("Loaded " + lines.Length + " lines with the lineending " + LineEnding.ToString());
            Internal_TextChanged(text);
            UpdateText();
            UpdateSelection();
        }
        public async void Paste()
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string Text = LineEndings.CleanLineEndings(await dataPackageView.GetTextAsync(), LineEnding);
                if (await IsOverTextLimit(Text.Length))
                    return;

                AddCharacter(Text);
                ClearSelection();
                UpdateCursor();
            }
        }
        public void Copy()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(LineEndings.ChangeLineEndings(SelectedText, LineEnding));
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(dataPackage);
        }
        public void Cut()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(LineEndings.ChangeLineEndings(SelectedText, LineEnding));
            DeleteText(); //Delete the selected text
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            Clipboard.SetContent(dataPackage);
            ClearSelection();
        }
        public string GetText()
        {
            return String.Join(NewLineCharacter, TotalLines.Select(item => item.Content));
        }
        public void SetSelection(int start, int length)
        {
            var result = Selection.GetSelectionFromPosition(TotalLines, start, length, CharacterCount);
            if (result != null)
            {
                selectionrenderer.SelectionStartPosition = result.StartPosition;
                selectionrenderer.SelectionEndPosition = result.EndPosition;
                if (result.EndPosition != null)
                    CursorPosition = result.EndPosition;

                selectionrenderer.HasSelection = true;
                selectionrenderer.IsSelecting = false;
            }

            UpdateSelection();
            UpdateCursor();
        }
        public void SelectAll()
        {
            //No selection can be shown
            if (TotalLines.Count == 1 && TotalLines[0].Length == 0)
                return;

            selectionrenderer.SelectionStartPosition = new CursorPosition(0, 0);
            CursorPosition = selectionrenderer.SelectionEndPosition = new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count - 1);
            selectionrenderer.HasSelection = true;
            Canvas_Selection.Invalidate();
        }
        public void ClearSelection()
        {
            if (selectionrenderer.HasSelection)
            {
                ForceClearSelection();
            }
        }
        public void Undo()
        {
            if (IsReadonly)
                return;

            //Do the Undo
            var sel = UndoRedo.Undo(TotalLines, NewLineCharacter);
            Internal_TextChanged();

            if (sel != null)
            {
                selectionrenderer.SelectionStartPosition = sel.StartPosition;
                selectionrenderer.SelectionEndPosition = sel.EndPosition;
                selectionrenderer.HasSelection = true;
                selectionrenderer.IsSelecting = false;
            }
            else
                ForceClearSelection();
            UpdateAll();
        }
        public void Redo()
        {
            if (IsReadonly)
                return;

            //Do the Redo
            var sel = UndoRedo.Redo(TotalLines, NewLineCharacter);
            Internal_TextChanged();

            if (sel != null)
            {
                selectionrenderer.SelectionStartPosition = sel.StartPosition;
                selectionrenderer.SelectionEndPosition = sel.EndPosition;
                selectionrenderer.HasSelection = true;
                selectionrenderer.IsSelecting = false;
            }
            else
                ForceClearSelection();
            UpdateAll();
        }
        public void ScrollLineToCenter(int line)
        {
            //Check whether the current line is outside the bounds of the visible area
            if (line < NumberOfUnrenderedLinesToRenderStart || line >= NumberOfUnrenderedLinesToRenderStart + RenderedLines.Count)
            {
                ScrollLineIntoView(line);
            }
        }
        public void ScrollOneLineUp()
        {
            VerticalScrollbar.Value -= SingleLineHeight;
            UpdateAll();
        }
        public void ScrollOneLineDown()
        {
            VerticalScrollbar.Value += SingleLineHeight;
            UpdateAll();
        }
        public void ScrollLineIntoView(int line)
        {
            VerticalScrollbar.Value = (line - RenderedLines.Count / 2) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollTopIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber - 1) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollBottomIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber - RenderedLines.Count + 1) * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollPageUp()
        {
            CursorPosition.LineNumber -= RenderedLines.Count;
            if (CursorPosition.LineNumber < 0)
                CursorPosition.LineNumber = 0;

            VerticalScrollbar.Value -= RenderedLines.Count * SingleLineHeight;
            UpdateAll();
        }
        public void ScrollPageDown()
        {
            CursorPosition.LineNumber += RenderedLines.Count;
            if (CursorPosition.LineNumber > TotalLines.Count - 1)
                CursorPosition.LineNumber = TotalLines.Count - 1;
            VerticalScrollbar.Value += RenderedLines.Count * SingleLineHeight;
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
        public void SetLineContent(int line, string text)
        {
            ListHelper.GetLine(TotalLines, line).Content = text;
            UpdateText();
        }
        public void DeleteLine(int line)
        {
            TotalLines.RemoveAt(line);
            UpdateText();
        }
        public void AddLine(int position, string text)
        {
            ListHelper.Insert(TotalLines, new Line(text), position);
            UpdateText();
        }
        public TextSelectionPosition FindInText(string pattern)
        {
            var pos = Regex.Match(GetText(), pattern);
            return new TextSelectionPosition(pos.Index, pos.Length);
        }
        public void SourroundSelectionWith(string value)
        {
            SourroundSelectionWith(value, value);
        }
        public void SourroundSelectionWith(string value1, string value2)
        {
            if (!SelectionIsNull())
            {
                AddCharacter(value1 + SelectedText + value2);
            }
        }
        public void DuplicateLine(int line)
        {
            var content = new Line(ListHelper.GetLine(TotalLines, line).Content);
            ListHelper.Insert(TotalLines, content, line);
            CursorPosition.LineNumber += 1;
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

        //Properties:
        public bool SyntaxHighlighting { get; set; } = true;
        public CodeLanguage CustomCodeLanguage
        {
            get => _CodeLanguage;
            set
            {
                _CodeLanguage = value;
                UpdateSyntaxHighlighting();
                UpdateText();
            }
        }
        public CodeLanguages CodeLanguage
        {
            get => _CodeLanguages;
            set
            {
                _CodeLanguage = CodeLanguageHelper.GetCodeLanguage(value);
                UpdateSyntaxHighlighting();
                UpdateText();
            }
        }
        public LineEnding LineEnding
        {
            get => _LineEnding;
            set
            {
                NewLineCharacter = LineEndings.LineEndingToString(value);
                _LineEnding = value;
            }
        }
        private float _SpaceBetweenLineNumberAndText = 30;
        public float SpaceBetweenLineNumberAndText { get => _SpaceBetweenLineNumberAndText; set { _SpaceBetweenLineNumberAndText = value; UpdateText(); } }
        public CursorPosition CursorPosition
        {
            get => _CursorPosition;
            set { _CursorPosition = new CursorPosition(value.CharacterPosition, value.LineNumber); UpdateCursor(); }
        }
        public new FontFamily FontFamily { get => _FontFamily; set { _FontFamily = value; NeedsTextFormatUpdate = true; UpdateAll(); } }
        public new int FontSize { get => _FontSize; set { _FontSize = value; UpdateZoom(); } }
        public float RenderedFontSize { get => ZoomedFontSize; }
        public string Text { get => GetText(); set { SetText(value); } }
        public Color TextColor { get => _TextColor; set { _TextColor = value; ColorResourcesCreated = false; UpdateAll(); } }
        public Color SelectionColor { get => _SelectionColor; set { _SelectionColor = value; selectionrenderer.SelectionColor = value; UpdateAll(); } }
        public Color CursorColor { get => _CursorColor; set { _CursorColor = value; ColorResourcesCreated = false; UpdateAll(); } }
        public Color LineNumberColor { get => _LineNumberColor; set { _LineNumberColor = value; ColorResourcesCreated = false; UpdateAll(); } }
        public Color LineHighlighterColor { get => _LineHighlighterColor; set { _LineHighlighterColor = value; ColorResourcesCreated = false; UpdateAll(); } }
        public Color LineNumberBackground { get => _LineNumberBackground; set { _LineNumberBackground = value; ColorResourcesCreated = false; UpdateAll(); } }
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
        public bool IsReadonly { get; set; } = false;
        [Description("Change the size of the cursor. Use null for the default size")]
        public CursorSize CursorSize { get => _CursorSize; set { _CursorSize = value; UpdateCursor(); } }
        public new MenuFlyout ContextFlyout
        {
            get { return FlyoutHelper.MenuFlyout; }
            set
            {
                //Use the inbuild flyout
                if (value == null)
                {
                    FlyoutHelper.CreateFlyout(this);
                }
                else //Use a custom flyout
                {
                    FlyoutHelper.MenuFlyout = value;
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
                    return GetText();

                return Selection.GetSelectedText(TotalLines, TextSelection, CursorPosition.LineNumber, NewLineCharacter);
            }
            set
            {
                AddCharacter(value);
            }
        }
        public int NumberOfLines { get => TotalLines.Count; }
        public int CurrentLineIndex { get => CursorPosition.LineNumber; }
        public ScrollBarPosition ScrollBarPosition
        {
            get => new ScrollBarPosition(HorizontalScrollbar.Value, VerticalScrollbar.Value);
            set { HorizontalScrollbar.Value = value.ValueX; VerticalScrollbar.Value = value.ValueY; }
        }
        public int CharacterCount { get => Utils.CountCharacters(TotalLines); }

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
    public class SyntaxHighlights
    {
        public SyntaxHighlights(string Pattern, Windows.UI.Color Color)
        {
            this.Pattern = Pattern;
            this.Color = Color;
        }

        public string Pattern { get; set; }
        public Color Color { get; set; }
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
