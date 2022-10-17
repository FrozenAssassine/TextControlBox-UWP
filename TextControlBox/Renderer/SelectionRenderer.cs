using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TextControlBox.Text;
using Windows.Foundation;
using Windows.UI;

namespace TextControlBox.Renderer
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
        public TextSelection DrawSelection(CanvasTextLayout TextLayout, List<Line> RenderedLines, CanvasDrawEventArgs args, float MarginLeft, float MarginTop, int UnrenderedLinesToRenderStart, int NumberOfRenderedLines, float FontSize)
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
                        {
                            Debug.WriteLine("Out of range1");
                            break;
                        }
                        SelStartIndex += RenderedLines[i].Length + 2;
                    }
                    SelStartIndex += CharacterPosStart;

                    for (int i = 0; i < SelectionEndPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        if (i >= NumberOfRenderedLines)
                        {
                            Debug.WriteLine("Out of range2");
                            break;
                        } //Out of range of the List (do nothing)
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
                    //Change the width if selection in an emty line or starts at a line end
                    if (descriptions[i].LayoutBounds.Width == 0 && descriptions.Length > 1)
                    {
                        var bounds = descriptions[i].LayoutBounds;
                        descriptions[i].LayoutBounds = new Rect { Width = FontSize / 4, Height = bounds.Height, X = bounds.X, Y = bounds.Y };
                    }

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
            SelectionStartPosition = null;
        }

        public void SetSelection(TextSelection selection)
        {
            SetSelection(selection.StartPosition, selection.EndPosition);
        }
        public void SetSelection(CursorPosition StartPosition, CursorPosition EndPosition)
        {
            IsSelecting = true;
            SelectionStartPosition = StartPosition;
            SelectionEndPosition = EndPosition;
            IsSelecting = false;
            HasSelection = true;
        }
        public void SetSelectionStart(CursorPosition StartPosition)
        {
            IsSelecting = true;
            SelectionStartPosition = StartPosition;
            IsSelecting = false;
            HasSelection = true;
        }
        public void SetSelectionEnd(CursorPosition EndPosition)
        {
            IsSelecting = true;
            SelectionEndPosition = EndPosition;
            IsSelecting = false;
            HasSelection = true;
        }
    }
}