using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextControlBox_TestApp.TextControlBox.Helper;
using TextControlBox_TestApp.TextControlBox.Languages;
using TextControlBox_TestApp.TextControlBox.Renderer;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;
using Color = Windows.UI.Color;
using Size = Windows.Foundation.Size;

namespace TextControlBox_TestApp.TextControlBox
{
    public partial class TextControlBox : UserControl
    {
        private string NewLineCharacter = "\r\n";
        private string TabCharacter = "\t";
        private InputPane inputPane;
        private bool _ShowLineNumbers = true;
        private CodeLanguage _CodeLanguage = null;
        private CodeLanguages _CodeLanguages = CodeLanguages.None;
        private LineEnding _LineEnding = LineEnding.CRLF;
        private ObservableCollection<TextHighlight> _TextHighlights = new ObservableCollection<TextHighlight>();
        private bool _ShowLineHighlighter = true;
        private int _FontSize = 18;
        private int _ZoomFactor = 100; //%

        float SingleLineHeight { get => TextFormat.LineSpacing; }
        float ZoomedFontSize = 0;
        int MaxFontsize = 125;
        int MinFontSize = 3;
        int OldZoomFactor = 0;

        int NumberOfStartLine = 0;
        int NumberOfUnrenderedLinesToRenderStart = 0;

        //Colors:
        CanvasSolidColorBrush TextColorBrush;
        CanvasSolidColorBrush CursorColorBrush;
        CanvasSolidColorBrush LineNumberColorBrush;
        CanvasSolidColorBrush LineHighlighterBrush;

        bool ColorResourcesCreated = false;
        bool NeedsTextFormatUpdate = false;
        bool GotKeyboardInput = false;
        bool DragDropSelection = false;

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
            EditContext.NotifyFocusEnter();

            //Classes & Variables:
            selectionrenderer = new SelectionRenderer(SelectionColor);
            inputPane = InputPane.GetForCurrentView();

            //Events:
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            _TextHighlights.CollectionChanged += _TextHighlights_CollectionChanged;
            InitialiseOnStart();
        }

        private void InitialiseOnStart()
        {
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

        private void UpdateZoom()
        {
            ZoomedFontSize = (float)_FontSize * (float)_ZoomFactor / 100;
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
            UpdateScrollToShowCursor();
            UpdateText();
            Canvas_Selection.Invalidate();
        }
        private void UpdateCursor()
        {
            Canvas_Cursor.Invalidate();
        }
        private void UpdateText()
        {
            ChangeCursor(CoreCursorType.IBeam);
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
            CursorPosition.LineNumber = CursorRenderer.GetCursorLineFromPoint(Point, SingleLineHeight, RenderedLines.Count, NumberOfStartLine, NumberOfUnrenderedLinesToRenderStart);

            UpdateCurrentLineTextLayout();
            CursorPosition.CharacterPosition = CursorRenderer.GetCharacterPositionFromPoint(GetCurrentLine(), CurrentLineTextLayout, Point, (float)-HorizontalScrollbar.Value);
        }

        private void AddCharacter(string text, bool ExcecuteNextUndoToo = false, bool IgnoreSelection = false)
        {
            if (CurrentLine == null || IsReadonly)
                return;

            var SplittedText = text.Split(NewLineCharacter);

            if (IgnoreSelection)
                ClearSelection();

            //Nothing is selected
            if (TextSelection == null && SplittedText.Length == 1)
            {
                if (text.Length == 1)
                    UndoRedo.EnteringText += text;
                else
                {
                    UndoRedo.EnteringText = "";
                    UndoRedo.RecordSingleLineUndo(CurrentLine, CursorPosition);
                }

                if (CursorPosition.CharacterPosition > GetLineContentWidth(CurrentLine) - 1)
                    CurrentLine.AddToEnd(text);
                else
                    CurrentLine.AddText(text, CursorPosition.CharacterPosition);
                CursorPosition.CharacterPosition += text.Length;
            }
            else if (TextSelection == null && SplittedText.Length > 1)
            {
                UndoRedo.RecordMultiLineUndo(CursorPosition.LineNumber, ListHelper.GetLine(TotalLines, CursorPosition.LineNumber).Content, SplittedText.Length);
                CursorPosition = Selection.InsertText(TextSelection, CursorPosition, TotalLines, text, NewLineCharacter);
            }
            else
            {
                string SelectedLines = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, NewLineCharacter);
                //Check whether the startline and endline are completely selected to calculate the number of lines to delete
                CursorPosition StartLine = Selection.GetMin(TextSelection.StartPosition, TextSelection.EndPosition);
                int DeleteCount = StartLine.CharacterPosition == 0 ? 0 : 1;
                if (DeleteCount == 0)
                {
                    CursorPosition EndLine = Selection.GetMax(TextSelection.StartPosition, TextSelection.EndPosition);
                    DeleteCount = EndLine.CharacterPosition == ListHelper.GetLine(TotalLines, EndLine.LineNumber).Length ? 0 : 1;
                }

                UndoRedo.RecordMultiLineUndo(StartLine.LineNumber + 1, SelectedLines, text.Length == 0 ? DeleteCount : SplittedText.Length, TextSelection, ExcecuteNextUndoToo);
                CursorPosition = Selection.Replace(TextSelection, TotalLines, text, NewLineCharacter);

                selectionrenderer.ClearSelection();
                TextSelection = null;
                UpdateSelection();
            }

            UpdateScrollToShowCursor();
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
                int StepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveLeft(CurrentLine, CursorPosition.CharacterPosition) : 1;
                if (CursorPosition.CharacterPosition > 0)
                {
                    UndoRedo.RecordSingleLineUndo(CurrentLine, CursorPosition);
                    CurrentLine.Remove(CursorPosition.CharacterPosition - StepsToMove, StepsToMove);
                    CursorPosition.CharacterPosition -= StepsToMove;
                }
                else if (CursorPosition.LineNumber > 0)
                {
                    UndoRedo.RecordMultiLineUndo(CursorPosition.LineNumber, ListHelper.GetLine(TotalLines, CursorPosition.LineNumber).Content, 0);

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
                AddCharacter(""); //Replace the selection by nothing
                selectionrenderer.ClearSelection();
                TextSelection = null;
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
                int StepsToMove = ControlIsPressed ? Cursor.CalculateStepsToMoveRight(CurrentLine, CursorPosition.CharacterPosition) : 1;

                if (CursorPosition.CharacterPosition < CurrentLine.Length)
                {
                    UndoRedo.RecordSingleLineUndo(CurrentLine, CursorPosition);
                    CurrentLine.Remove(CursorPosition.CharacterPosition, StepsToMove);
                }
                else if (TotalLines.Count > CursorPosition.LineNumber)
                {
                    Line LineToAdd = CursorPosition.LineNumber + 1< TotalLines.Count ? ListHelper.GetLine(TotalLines, CursorPosition.LineNumber + 1) : null;
                    if (LineToAdd != null)
                    {
                        UndoRedo.RecordMultiLineUndo(CursorPosition.LineNumber, CurrentLine.Content + LineToAdd.Content, 1);
                        CurrentLine.Content += LineToAdd.Content;
                        TotalLines.Remove(LineToAdd);
                    }
                }
            }
            else
            {
                AddCharacter(""); //Replace the selection by nothing
                selectionrenderer.ClearSelection();
                TextSelection = null;
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

            CursorPosition NormalCurPos = CursorPosition.ChangeLineNumber(CursorPosition, CursorPosition.LineNumber);
            CursorPosition StartLinePos = new CursorPosition(TextSelection == null ? NormalCurPos : Selection.GetMin(TextSelection));
            CursorPosition EndLinePos = new CursorPosition(TextSelection == null ? NormalCurPos : Selection.GetMax(TextSelection));

            int Index = StartLinePos.LineNumber;
            if (Index >= TotalLines.Count)
                Index = TotalLines.Count - 1;
            if (Index < 0)
                Index = 0;

            Line EndLine = new Line();
            Line StartLine = ListHelper.GetLine(TotalLines, Index);

            //If the whole text is selected
            if (Selection.WholeTextSelected(TextSelection, TotalLines))
            {
                UndoRedo.RecordNewLineUndo(GetText(), 2, StartLinePos.LineNumber, TextSelection);

                TotalLines.Clear();
                TotalLines.Add(EndLine);
                CursorPosition = new CursorPosition(0, 1);

                ClearSelection();
                UpdateText();
                UpdateSelection();
                UpdateCursor();
                Internal_TextChanged();
                return;
            }

            //Undo
            string Lines;
            if (TextSelection == null)
                Lines = StartLine.Content;
            else
                Lines = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, NewLineCharacter);

            int UndoDeleteCount = 2;
            //Whole lines are selected
            if (TextSelection != null && StartLinePos.CharacterPosition == 0 && EndLinePos.CharacterPosition == ListHelper.GetLine(TotalLines, EndLinePos.LineNumber).Length)
            {
                UndoDeleteCount = 1;
            }

            UndoRedo.RecordNewLineUndo(Lines, UndoDeleteCount, StartLinePos.LineNumber, TextSelection);

            if (TextSelection != null)
            {
                CursorPosition = Selection.Remove(TextSelection, TotalLines, NewLineCharacter);
                ClearSelection();
                //Inline selection
                if (StartLinePos.LineNumber == EndLinePos.LineNumber)
                {
                    StartLinePos.CharacterPosition = CursorPosition.CharacterPosition;
                }
            }

            string[] SplittedLine = Utils.SplitAt(StartLine.Content, StartLinePos.CharacterPosition);
            StartLine.SetText(SplittedLine[1]);
            EndLine.SetText(SplittedLine[0]);

            //Add the line to the collection
            ListHelper.Insert(TotalLines, EndLine, StartLinePos.LineNumber);

            CursorPosition.LineNumber += 1;
            CursorPosition.CharacterPosition = 0;

            if (TextSelection == null && CursorPosition.LineNumber == RenderedLines.Count + NumberOfUnrenderedLinesToRenderStart)
                ScrollOneLineDown();
            else
                UpdateScrollToShowCursor();

            UpdateText();
            UpdateSelection();
            UpdateCursor();
            Internal_TextChanged();
        }

        private int GetLineContentWidth(Line line)
        {
            if (line == null || line.Content == null)
                return -1;
            return line.Length;
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
                selectionrenderer.ClearSelection();
                TextSelection = null;
                UpdateSelection();
            }
        }
        private void ClearSelection()
        {
            if (selectionrenderer.HasSelection)
            {
                selectionrenderer.ClearSelection();
                TextSelection = null;
                UpdateSelection();
            }
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
        public void SelectSingleWord(CursorPosition CursorPosition)
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

            string TextToInsert = SelectedText();

            CursorPosition StartLine = Selection.GetMin(TextSelection.StartPosition, TextSelection.EndPosition);
            int DeleteCount = StartLine.CharacterPosition == 0 ? 0 : 1;
            if (DeleteCount == 0)
            {
                CursorPosition EndLine = Selection.GetMax(TextSelection.StartPosition, TextSelection.EndPosition);
                DeleteCount = EndLine.CharacterPosition == ListHelper.GetLine(TotalLines, EndLine.LineNumber).Length ? 0 : 1;
            }
            
            UndoRedo.RecordMultiLineUndo(
                StartLine.LineNumber + 1, 
                Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, 
                NewLineCharacter), 
                DeleteCount, 
                TextSelection
                );
            Selection.Remove(TextSelection, TotalLines, NewLineCharacter);

            ClearSelection();

            CursorPosition.ChangeLineNumber(CursorPosition.LineNumber - TextToInsert.Split(NewLineCharacter).Length);

            UndoRedo.RecordMultiLineUndo(
                CursorPosition.LineNumber, 
                ListHelper.GetLine(TotalLines, CursorPosition.LineNumber).Content, 
                TextToInsert.Split(NewLineCharacter).Length,
                null, true);

            CursorPosition = Selection.InsertText(TextSelection, CursorPosition, TotalLines, TextToInsert, NewLineCharacter);

            ChangeCursor(CoreCursorType.IBeam);
            DragDropSelection = false;
            UpdateText();
            UpdateCursor();
            UpdateSelection();
        }
        private void EndDragDropSelection()
        {
            ClearSelection();
            UpdateSelection();
            DragDropSelection = false;
            ChangeCursor(CoreCursorType.IBeam);
            selectionrenderer.IsSelecting = false;
            UpdateCursor();
        }
        private void UpdateScrollToShowCursor()
        {
            if (NumberOfStartLine + RenderedLines.Count <= CursorPosition.LineNumber)
                ScrollBottomIntoView();
            else if (NumberOfStartLine > CursorPosition.LineNumber)
                ScrollTopIntoView();
        }

        //Syntaxhighlighting
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
        private void UpdateTextHighlights(CanvasDrawingSession DrawingSession)
        {
            /*for (int i = 0; i < _TextHighlights.Count; i++)
            {
                TextHighlight th = _TextHighlights[i];
                int Start = 0;
                int End = 0;
                if (th.StartIndex < RenderStartIndex && th.EndIndex > RenderStartIndex)
                {
                    End = th.EndIndex;
                    Start = 0;
                }
                if(th.StartIndex > RenderStartIndex && th.StartIndex > RenderStartIndex)

                var regions = DrawnTextLayout.GetCharacterRegions(th.StartIndex - RenderStartIndex, th.EndIndex - th.StartIndex - RenderStartIndex);
                for(int j = 0; j< regions.Length; j++)
                {
                    DrawingSession.FillRectangle(new Windows.Foundation.Rect
                    {
                        Height = regions[j].LayoutBounds.Height,
                        Width = regions[j].LayoutBounds.Width,
                        X = regions[j].LayoutBounds.X,
                        Y = regions[j].LayoutBounds.Y
                    }, th.HighlightColor);
                }
            }*/
        }

        //Handle keyinputs
        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
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
                    case VirtualKey.Z:
                        Undo();
                        break;
                    case VirtualKey.V:
                        Paste();
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
                case VirtualKey.Space:
                    UndoRedo.RecordUndoOnPress(CurrentLine, CursorPosition);
                    break;
                case VirtualKey.Enter:
                    UndoRedo.RecordUndoOnPress(CurrentLine, CursorPosition);
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
                        Cursor.RelativeToAbsolute(CursorPosition, NumberOfUnrenderedLinesToRenderStart);
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
                        Cursor.RelativeToAbsolute(CursorPosition, NumberOfUnrenderedLinesToRenderStart);

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
                        Cursor.RelativeToAbsolute(CursorPosition, NumberOfUnrenderedLinesToRenderStart);

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
                        Cursor.RelativeToAbsolute(CursorPosition, NumberOfUnrenderedLinesToRenderStart);

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
                UndoRedo.RecordSingleLineUndo(CurrentLine, CursorPosition);
                GotKeyboardInput = true;
            }


            AddCharacter(args.Text);
        }

        //Pointer-events:
        private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            Point PointerPosition = args.CurrentPoint.Position;

            if (selectionrenderer.IsSelecting)
            {
                double CanvasWidth = Math.Round(this.ActualWidth, 2);
                double CurPosX = Math.Round(PointerPosition.X, 2);
                if (CurPosX > CanvasWidth - 100)
                {
                    HorizontalScrollbar.Value -= (CurPosX > CanvasWidth + 30 ? -20 : (-CanvasWidth - CurPosX) / 150);
                }
                else if (CurPosX < 100)
                {
                    HorizontalScrollbar.Value += CurPosX < -30 ? -20 : -(100 - CurPosX) / 10;
                }
            }
        }
        private void Canvas_Selection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            //End text drag/drop -> insert text at cursorposition
            if (DragDropSelection && Window.Current.CoreWindow.PointerCursor.Type != CoreCursorType.UniversalNo)
            {
                DoDragDropSelection();
            }
            else if(DragDropSelection)
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
                if (selectionrenderer.CursorIsInSelection(CursorPosition, TextSelection) || 
                    selectionrenderer.PointerIsOverSelection(e.GetCurrentPoint(sender as UIElement).Position, TextSelection, DrawnTextLayout))
                {
                    ChangeCursor(CoreCursorType.UniversalNo);
                }
                else
                    ChangeCursor(CoreCursorType.Arrow);

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
                bool IsShiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                bool LeftButtonPressed = e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed;

                //Show the onscreenkeyboard if no physical keyboard is attached
                inputPane.TryShow();

                //Shift + click = set selection
                if (IsShiftPressed && LeftButtonPressed)
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
                    
                    //Text dragging/dropping
                    if(TextSelection != null)
                    {
                        if (selectionrenderer.PointerIsOverSelection(PointerPosition, TextSelection, DrawnTextLayout) && !DragDropSelection)
                        {
                            PointerClickCount = 0;
                            ChangeCursor(CoreCursorType.UniversalNo);
                            DragDropSelection = true;
                            return;
                        }
                        if (DragDropSelection)
                        {
                            EndDragDropSelection();
                        }
                    }
                    
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
                e.Handled = true;
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
        }

        //Scrolling and Zooming
        private void VerticalScrollbar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Canvas_Text.Invalidate();
            Canvas_Selection.Invalidate();
            Canvas_Cursor.Invalidate();
        }
        private void HorizontalScrollbar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Canvas_Text.Invalidate();
            Canvas_Selection.Invalidate();
            Canvas_Cursor.Invalidate();
        }

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
                    LineNumberTextFormat = TextRenderer.CreateLinenumberTextFormat(ZoomedFontSize);
                TextFormat = TextRenderer.CreateCanvasTextFormat(ZoomedFontSize);
            }

            CreateColorResources(args.DrawingSession);

            //Calculate number of lines that needs to be rendered
            int NumberOfLinesToBeRendered = (int)(sender.ActualHeight / SingleLineHeight);
            NumberOfStartLine = (int)(VerticalScrollbar.Value / SingleLineHeight);

            NumberOfUnrenderedLinesToRenderStart = NumberOfStartLine;

            //Measure textposition and apply the value to the scrollbar
            VerticalScrollbar.Maximum = (TotalLines.Count + 1) * SingleLineHeight - Scroll.ActualHeight;
            VerticalScrollbar.ViewportSize = sender.ActualHeight;

            StringBuilder LineNumberContent = new StringBuilder();
            //Get all the lines, which needs to be rendered, from the array with all lines
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

            //Create the textlayout --> apply the Syntaxhighlighting --> render it
            DrawnTextLayout = TextRenderer.CreateTextResource(sender, DrawnTextLayout, TextFormat, RenderedText, new Size { Height = sender.Size.Height, Width = this.ActualWidth });
            UpdateSyntaxHighlighting();
            args.DrawingSession.DrawTextLayout(DrawnTextLayout, (float)-HorizontalScrollbar.Value, SingleLineHeight, TextColorBrush);

            //UpdateTextHighlights(args.DrawingSession);
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
                TextSelection = selectionrenderer.DrawSelection(DrawnTextLayout, RenderedLines, args, (float)-HorizontalScrollbar.Value, SingleLineHeight / 4, NumberOfUnrenderedLinesToRenderStart, RenderedLines.Count, new ScrollBarPosition(HorizontalScrollbar.Value, VerticalScrollbar.Value));
            }

            if (TextSelection != null && !Selection.Equals(OldTextSelection, TextSelection))
            {
                OldTextSelection = new TextSelection(TextSelection);
                Internal_CursorChanged();
            }
        }
        private void Canvas_Cursor_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            CurrentLine = GetCurrentLine();
            if (CurrentLine == null || DrawnTextLayout == null)
                return;

            if(CursorPosition.LineNumber > TotalLines.Count)
                CursorPosition.LineNumber = TotalLines.Count;

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
            if (LineNumberTextToRender.Length == 0 || !_ShowLineNumbers)
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
            args.DrawingSession.DrawTextLayout(LineNumberLayout, 0, SingleLineHeight, LineNumberColorBrush);
        }

        //Internal events:
        private void Internal_TextChanged(string text = null)
        {
            TextChanged?.Invoke(this, text == null ? GetText() : text);
        }
        private void Internal_CursorChanged()
        {
            SelectionChangedEventHandler args = new SelectionChangedEventHandler
            {
                CharacterPositionInLine = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
            };
            if (selectionrenderer.SelectionStartPosition != null && selectionrenderer.SelectionEndPosition != null)
            {
                var Sel = Selection.GetIndexOfSelection(TotalLines, new TextSelection(selectionrenderer.SelectionStartPosition, selectionrenderer.SelectionEndPosition));
                args.SelectionLength = Sel.Length;
                args.SelectionStartIndex = 1 + Sel.Index;
            }
            else
            {
                args.SelectionLength = 0;
                args.SelectionStartIndex = 1 + Cursor.CursorPositionToIndex(TotalLines, CursorPosition);
            }
            SelectionChanged?.Invoke(this, args);
        }
        
        private void UserControl_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            inputPane.TryShow();
            ChangeCursor(CoreCursorType.IBeam);
        }
        private void UserControl_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        {
            inputPane.TryHide();
            ChangeCursor(CoreCursorType.Arrow);
        }
        private void _TextHighlights_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Nothing has changed
            if (e.NewItems.Count == 0 && e.OldItems.Count == 0)
                return;

            UpdateText();
        }
        
        //Cursor:
        private void ChangeCursor(CoreCursorType CursorType)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CursorType, 0);
        }
        private void Canvas_LineNumber_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ChangeCursor(CoreCursorType.Arrow);
        }
        private void Canvas_LineNumber_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ChangeCursor(CoreCursorType.IBeam);
        }
        private void ScrollbarPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ChangeCursor(CoreCursorType.IBeam);
        }
        private void Scrollbar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ChangeCursor(CoreCursorType.Arrow);
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
                AddCharacter(text, false, true);
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
        public void SelectLine(int index, bool CursorAtStart = false)
        {
            selectionrenderer.SelectionStartPosition = new CursorPosition(0, index);
            CursorPosition pos = selectionrenderer.SelectionEndPosition = new CursorPosition(ListHelper.GetLine(TotalLines, index - 1).Length, index - 1);
            CursorPosition.LineNumber = index;
            CursorPosition.CharacterPosition = CursorAtStart ? 0 : pos.CharacterPosition;

            UpdateSelection();
            UpdateCursor();
        }
        public void SetText(string text)
        {
            TotalLines.Clear();
            RenderedLines.Clear();
            UndoRedo.ClearStacks();
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
                string Text = LineEndings.ChangeLineEndings(await dataPackageView.GetTextAsync(), LineEnding);
                AddCharacter(Text);
                ClearSelection();
                UpdateCursor();
            }
        }
        public void Copy()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(LineEndings.ChangeLineEndings(SelectedText(), LineEnding));
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(dataPackage);
        }
        public void Cut()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(LineEndings.ChangeLineEndings(SelectedText(), LineEnding));
            DeleteText(); //Delete the selected text
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            Clipboard.SetContent(dataPackage);
            ClearSelection();
        }
        public string SelectedText()
        {
            if (TextSelection != null && Selection.WholeTextSelected(TextSelection, TotalLines))
                return GetText();

            return Selection.GetSelectedText(TotalLines, TextSelection, CursorPosition.LineNumber, NewLineCharacter);
        }
        public string GetText()
        {
            return String.Join(NewLineCharacter, TotalLines.Select(item => item.Content));
        }
        public void SetSelection(CursorPosition StartPosition, CursorPosition EndPosition = null)
        {
            //Nothing gets selected:
            if (EndPosition == null || (StartPosition.LineNumber == EndPosition.LineNumber && StartPosition.CharacterPosition == EndPosition.CharacterPosition))
            {
                CursorPosition = new CursorPosition(StartPosition.CharacterPosition, StartPosition.LineNumber - 1);
            }
            else
            {
                selectionrenderer.SelectionStartPosition = new CursorPosition(StartPosition.CharacterPosition, StartPosition.LineNumber - 1);
                selectionrenderer.SelectionEndPosition = new CursorPosition(EndPosition.CharacterPosition, StartPosition.LineNumber - 1);
                selectionrenderer.HasSelection = true;
                Canvas_Selection.Invalidate();
            }
            UpdateCursor();
        }
        public void SelectAll()
        {
            selectionrenderer.SelectionStartPosition = new CursorPosition(0, 0);
            CursorPosition = selectionrenderer.SelectionEndPosition = new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count);
            selectionrenderer.HasSelection = true;
            Canvas_Selection.Invalidate();
        }
        public void Undo()
        {
            if (IsReadonly)
                return;

            var sel = UndoRedo.Undo(TotalLines, this, NewLineCharacter);
            Internal_TextChanged();
            if (sel == null)
                return;
            
            selectionrenderer.SelectionStartPosition = sel.StartPosition;
            selectionrenderer.SelectionEndPosition = sel.EndPosition;
            selectionrenderer.HasSelection = true;
            selectionrenderer.IsSelecting = false;
            UpdateText();
            UpdateSelection();
        }
        public void Redo()
        {
            if (IsReadonly)
                return;

            UndoRedo.Redo(TotalLines, this);
            Internal_TextChanged();
        }
        public void ScrollOneLineUp()
        {
            VerticalScrollbar.Value -= SingleLineHeight;
        }
        public void ScrollOneLineDown()
        {
            VerticalScrollbar.Value += SingleLineHeight;
            //UpdateText();
        }
        public void ScrollLineIntoView(int Line)
        {
            VerticalScrollbar.Value = Line * SingleLineHeight;
        }
        public void ScrollTopIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber-1) * SingleLineHeight;
        }
        public void ScrollBottomIntoView()
        {
            VerticalScrollbar.Value = (CursorPosition.LineNumber - RenderedLines.Count + 1) * SingleLineHeight;
        }
        public void ScrollPageUp()
        {
            CursorPosition.LineNumber -= RenderedLines.Count;
            if (CursorPosition.LineNumber < 0)
                CursorPosition.LineNumber = 0;

            VerticalScrollbar.Value -= RenderedLines.Count * SingleLineHeight;
        }
        public void ScrollPageDown()
        {
            CursorPosition.LineNumber += RenderedLines.Count;
            if (CursorPosition.LineNumber > TotalLines.Count - 1)
                CursorPosition.LineNumber = TotalLines.Count - 1;
            VerticalScrollbar.Value += RenderedLines.Count * SingleLineHeight;
        }

        //Properties:
        public ObservableCollection<TextHighlight> TextHighlights
        {
            get => _TextHighlights;
        }
        public bool SyntaxHighlighting { get; set; } = true;
        public CodeLanguage CustomCodeLanguage
        {
            get => _CodeLanguage;
            set
            {
                _CodeLanguage = value;
                UpdateSyntaxHighlighting();
            }
        }
        public CodeLanguages CodeLanguage
        {
            get => _CodeLanguages;
            set
            {
                _CodeLanguage = CodeLanguageHelper.GetCodeLanguage(value);
                UpdateSyntaxHighlighting();
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
        public float SpaceBetweenLineNumberAndText = 30;
        public CursorPosition CursorPosition
        {
            get => _CursorPosition;
            set { _CursorPosition = new CursorPosition(value.CharacterPosition, value.LineNumber + (int)(VerticalScrollbar.Value / SingleLineHeight)); UpdateCursor(); }
        }
        public new int FontSize { get => _FontSize; set { _FontSize = value; UpdateZoom(); } }
        public string Text { get => GetText(); set { SetText(value);} }
        public Color TextColor = Color.FromArgb(255, 255, 255, 255);
        public Color SelectionColor = Color.FromArgb(100, 0, 100, 255);
        public Color CursorColor = Color.FromArgb(255, 255, 255, 255);
        public Color LineNumberColor = Color.FromArgb(255, 150, 150, 150);
        public Color LineHighlighterColor = Color.FromArgb(50, 0, 0, 0);
        public bool ShowLineNumbers
        {
            get
            {
                return _ShowLineNumbers;
            }
            set
            {
                _ShowLineNumbers = value;
                UpdateText();
            }
        }
        public bool ShowLineHighlighter
        {
            get => _ShowLineHighlighter;
            set { _ShowLineHighlighter = value; UpdateCursor(); }
        }
        public int ZoomFactor { get => _ZoomFactor; set { _ZoomFactor = value; UpdateZoom(); } } //%
        public bool IsReadonly { get; set; } = false; 

        //Events:
        public delegate void TextChangedEvent(TextControlBox sender, string Text);
        public event TextChangedEvent TextChanged;
        public delegate void SelectionChangedEvent(TextControlBox sender, SelectionChangedEventHandler args);
        public event SelectionChangedEvent SelectionChanged;
        public delegate void ZoomChangedEvent(TextControlBox sender, int ZoomFactor);
        public event ZoomChangedEvent ZoomChanged;
    }

    public class Line
    {
        private string _Content = "";
        public string Content { get => _Content; set { _Content = value; this.Length = value.Length; } }
        public int Length { get; private set; }

        public Line(string Content = "")
        {
            this.Content = Content;
        }
        public void SetText(string Value)
        {
            Content = Value;
        }
        public void AddText(string Value, int Position)
        {
            if (Position < 0)
                Position = 0;

            if (Position >= Content.Length)
                Content += Value;
            else if (Length <= 0)
                AddToEnd(Value);
            else
                Content = Content.Insert(Position, Value);
        }
        public void AddToEnd(string Value)
        {
            Content += Value;
        }
        public void AddToStart(string Value)
        {
            Content = Content.Insert(0, Value);
        }
        public string Remove(int Index, int Count = -1)
        {
            if (Index >= Length || Index < 0)
                return Content;

            try
            {
                if (Count == -1)
                    Content = Content.Remove(Index);
                else
                    Content = Content.Remove(Index, Count);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            return Content;
        }
        public string Substring(int Index, int Count = -1)
        {
            if (Index >= Length)
                Content = "";
            else if (Count == -1)
                Content = Content.Substring(Index);
            else
                Content = Content.Substring(Index, Count);
            return Content;
        }
        public void ReplaceText(int Start, int End, string Text)
        {
            int end = Math.Max(Start, End);
            int start = Math.Min(Start, End);

            if (start == 0 && end >= Length)
                Content = "";
            else
            {
                Content = Content.Remove(End) + Text + Content.Remove(0, start);
            }
        }
    }
    public enum CodeLanguages
    {
        Csharp, Gcode, Html, None
    }
    public class TextHighlight
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public Color HighlightColor { get; }

        public TextHighlight(int StartIndex, int EndIndex, Color HighlightColor)
        {
            this.StartIndex = StartIndex;
            this.EndIndex = EndIndex;
            this.HighlightColor = HighlightColor;
        }
    }
    public class CodeLanguage
    {
        public string Name { get; set; }
        public List<SyntaxHighlights> Highlights = new List<SyntaxHighlights>();
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
    public enum LineEnding
    {
        LF, CRLF, CR
    }

    public class SelectionChangedEventHandler
    {
        public int CharacterPositionInLine;
        public int LineNumber;
        public int SelectionStartIndex;
        public int SelectionLength;
    }
}
public static class Extensions
{
    public static string RemoveFirstOccurence(this string value, string removeString)
    {
        int index = value.IndexOf(removeString, StringComparison.Ordinal);
        return index < 0 ? value : value.Remove(index, removeString.Length);
    }
    public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter, Func<T, string> func)
    {
        return String.Join(delimiter, source.Select(func).ToArray());
    }
}