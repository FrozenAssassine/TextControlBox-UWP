using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using TextControlBox_TestApp.TextControlBox.Helper;
using Windows.Foundation;
using Windows.UI;

namespace TextControlBox_TestApp.TextControlBox.Renderer
{
    public class SelectionRenderer
    {
        public bool HasSelection = false;
        public bool IsSelecting = false;
        public CursorPosition SelectionStartPosition = null;
        public CursorPosition SelectionEndPosition = null;
        public Color SelectionColor;
        public int SelectionLength = 0;
        public int SelectionStart = 0;

        public SelectionRenderer(Color SelectionColor)
        {
            this.SelectionColor = SelectionColor;
        }

        //Get the point, where the selection starts, It is always the higest cursorposition
        public CursorPosition GetSelectionEndPoint()
        {
            if (SelectionStartPosition == null || SelectionEndPosition == null)
                return null;

            int EndLine = Math.Max(SelectionStartPosition.LineNumber, SelectionEndPosition.LineNumber);
            int EndCharacterPos;

            if (EndLine == SelectionEndPosition.LineNumber)
            {
                EndCharacterPos = SelectionEndPosition.CharacterPosition;
            }
            else
            {
                EndCharacterPos = SelectionStartPosition.CharacterPosition;
            }
            return new CursorPosition(EndCharacterPos, EndLine);
        }
        //Get the point, where the selection starts, It is always the lowest cursorposition
        public CursorPosition GetSelectionStartPoint()
        {
            int StartLine = Math.Min(SelectionStartPosition.LineNumber, SelectionEndPosition.LineNumber);
            int StartCharacterPos;

            if (StartLine == SelectionStartPosition.LineNumber)
            {
                StartCharacterPos = SelectionStartPosition.CharacterPosition;
            }
            else
            {
                StartCharacterPos = SelectionEndPosition.CharacterPosition;
            }
            return new CursorPosition(StartCharacterPos, StartLine);
        }

        //Create the rect, to render
        public Rect CreateRect(Rect r, float MarginLeft = 0, float MarginTop = 0)
        {
            return new Rect(
                new Point(
                    Math.Floor(r.Left + MarginLeft),//X
                    Math.Floor(r.Top + MarginTop)), //Y
                new Point(
                    Math.Ceiling(r.Right + MarginLeft), //Width
                    Math.Ceiling(r.Bottom + MarginTop))); //Height
        }

        //Draw the actual selection and return the cursorposition. Return -1 if no selection was drawn
        public TextSelection DrawSelection(CanvasTextLayout TextLayout, List<Line> RenderedLines, CanvasDrawEventArgs args, float MarginLeft, float MarginTop, int UnrenderedLinesToRenderStart, int NumberOfRenderedLines)
        {
            if (HasSelection && SelectionEndPosition != null && SelectionStartPosition != null)
            {
                int SelStartIndex = 0;
                int SelEndIndex = 0;
                int CharacterPosStart = SelectionStartPosition.CharacterPosition;
                int CharacterPosEnd = SelectionEndPosition.CharacterPosition;

                //Render the selection on position 0 if the user scrolled the start away
                if (SelectionEndPosition.LineNumber < SelectionStartPosition.LineNumber)
                {
                    if (SelectionEndPosition.LineNumber < UnrenderedLinesToRenderStart)
                        CharacterPosEnd = 0;
                    if (SelectionStartPosition.LineNumber < UnrenderedLinesToRenderStart + 1)
                        CharacterPosStart = 0;
                }
                else if (SelectionEndPosition.LineNumber == SelectionStartPosition.LineNumber)
                {
                    if (SelectionStartPosition.LineNumber < UnrenderedLinesToRenderStart)
                        CharacterPosStart = 0;
                    if (SelectionEndPosition.LineNumber < UnrenderedLinesToRenderStart)
                        CharacterPosEnd = 0;
                }
                else
                {
                    if (SelectionStartPosition.LineNumber < UnrenderedLinesToRenderStart)
                        CharacterPosStart = 0;
                    if (SelectionEndPosition.LineNumber < UnrenderedLinesToRenderStart + 1)
                        CharacterPosEnd = 0;
                }

                if (SelectionStartPosition.LineNumber == SelectionEndPosition.LineNumber)
                {
                    int LenghtToLine = 0;
                    for (int i = 0; i < SelectionStartPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        if (i < RenderedLines.Count)
                        {
                            LenghtToLine += RenderedLines[i].Length + 2;
                        }
                    }


                    SelStartIndex = CharacterPosStart + LenghtToLine;
                    SelEndIndex = CharacterPosEnd + LenghtToLine;
                }
                else
                {
                    for (int i = 0; i < SelectionStartPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        if (i >= NumberOfRenderedLines) //Out of range of the List (do nothing)
                            break;
                        SelStartIndex += RenderedLines[i].Length + 2;
                    }
                    SelStartIndex += CharacterPosStart;

                    for (int i = 0; i < SelectionEndPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        if (i >= NumberOfRenderedLines) //Out of range of the List (do nothing)
                            break;
                        SelEndIndex += RenderedLines[i].Length + 2;
                    }
                    SelEndIndex += CharacterPosEnd;
                }

                SelectionStart = Math.Min(SelStartIndex, SelEndIndex);

                if (SelEndIndex > SelStartIndex)
                    SelectionLength = SelEndIndex - SelStartIndex;
                else
                    SelectionLength = SelStartIndex - SelEndIndex;

                CanvasTextLayoutRegion[] descriptions = TextLayout.GetCharacterRegions(SelectionStart, SelectionLength);
                for (int i = 0; i < descriptions.Length; i++)
                {
                    args.DrawingSession.FillRectangle(CreateRect(descriptions[i].LayoutBounds, MarginLeft, MarginTop), SelectionColor);
                }
                return new TextSelection(SelectionStart, SelectionLength, new CursorPosition(SelectionStartPosition), new CursorPosition(SelectionEndPosition));
            }
            return null;
        }

        //returns whether the pointer is over a selection
        public bool PointerIsOverSelection(Point PointerPosition, TextSelection Selection, CanvasTextLayout TextLayout)
        {
            if (TextLayout == null || Selection == null)
                return false;

            CanvasTextLayoutRegion[] regions = TextLayout.GetCharacterRegions(Selection.Index, Selection.Length);
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].LayoutBounds.Contains(PointerPosition))
                    return true;
            }
            return false;
        }

        public bool CursorIsInSelection(CursorPosition CursorPosition, TextSelection TextSelection)
        {
            if (TextSelection == null)
                return false;
            TextSelection = Selection.OrderTextSelection(TextSelection);

            //Cursorposition is smaller than the start of selection
            if (TextSelection.StartPosition.LineNumber > CursorPosition.LineNumber)
            {
                return false;
            }
            else
            {
                //Selectionend is smaller than Cursorposition -> not in selection
                if (TextSelection.EndPosition.LineNumber < CursorPosition.LineNumber)
                {
                    return false;
                }
                else
                {
                    //Selection-start line equals Cursor line:
                    if (CursorPosition.LineNumber == TextSelection.StartPosition.LineNumber)
                    {
                        return CursorPosition.CharacterPosition > TextSelection.StartPosition.CharacterPosition;
                    }
                    //Selection-end line equals Cursor line
                    else if (CursorPosition.LineNumber == TextSelection.EndPosition.LineNumber)
                    {
                        return CursorPosition.CharacterPosition < TextSelection.EndPosition.CharacterPosition;
                    }
                    return true;
                }
            }
        }

        //Clear the selection
        public void ClearSelection()
        {
            HasSelection = false;
            IsSelecting = false;
            SelectionEndPosition = null;
        }
    }
}