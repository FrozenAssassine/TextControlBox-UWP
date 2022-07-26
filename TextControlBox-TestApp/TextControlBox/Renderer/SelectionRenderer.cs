using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        //Get the index, where pointer overlaps the text
        private int GetHitIndex(CanvasTextLayout TextLayout, Point mouseOverPt, float MarginLeft = 0)
        {
            if (TextLayout == null)
                return -1;

            HasSelection = TextLayout.HitTest(
                (float)mouseOverPt.X - MarginLeft,
                (float)mouseOverPt.Y,
                out var textLayoutRegion);
            return textLayoutRegion.CharacterIndex;
        }

        //Draw the actual selection and return the cursorposition. Return -1 if no selection was drawn
        public TextSelection DrawSelection(CanvasTextLayout TextLayout, List<Line> RenderedLines, CanvasDrawEventArgs args, float MarginLeft, float MarginTop, int UnrenderedLinesToRenderStart, int NumberOfRenderedLines, ScrollBarPosition ScrollBarPosition)
        {
            if (HasSelection && SelectionEndPosition != null && SelectionStartPosition != null)
            {
                int SelStartIndex = 0;
                int SelEndIndex = 0;
                int CharacterPosStart = SelectionStartPosition.CharacterPosition;
                int CharacterPosEnd = SelectionEndPosition.CharacterPosition;
                Debug.WriteLine(CharacterPosStart + "::" + CharacterPosEnd);

                Debug.WriteLine("UnrenderedLinesToRenderStart: " + UnrenderedLinesToRenderStart);

                //Render the selection on position 0 if the user scrolled the start away
                if (SelectionEndPosition.LineNumber < SelectionStartPosition.LineNumber)
                {
                    if (SelectionEndPosition.LineNumber < UnrenderedLinesToRenderStart)
                        CharacterPosEnd = 0;
                    if (SelectionStartPosition.LineNumber < UnrenderedLinesToRenderStart+1)
                        CharacterPosStart = 0;
                }
                else if(SelectionEndPosition.LineNumber == SelectionStartPosition.LineNumber)
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
                    if (SelectionEndPosition.LineNumber < UnrenderedLinesToRenderStart+1)
                        CharacterPosEnd = 0;
                }

                if (SelectionStartPosition.LineNumber == SelectionEndPosition.LineNumber)
                {
                    int LenghtToLine = 0;
                    for (int i = 0; i < SelectionStartPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        LenghtToLine += RenderedLines[i].Content.Length + 2;
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
                        SelStartIndex += RenderedLines[i].Content.Length + 2;
                    }
                    SelStartIndex += CharacterPosStart;

                    for (int i = 0; i < SelectionEndPosition.LineNumber - UnrenderedLinesToRenderStart; i++)
                    {
                        if (i >= NumberOfRenderedLines) //Out of range of the List (do nothing)
                            break;
                        SelEndIndex += RenderedLines[i].Content.Length + 2;
                    }
                    SelEndIndex += CharacterPosEnd;
                }

                SelectionStart = Math.Min(SelStartIndex, SelEndIndex);

                if(SelEndIndex > SelStartIndex)
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

        //Clear the selection
        public void ClearSelection()
        {
            HasSelection = false;
            IsSelecting = false;
            SelectionEndPosition = null;
        }
    }
    public class TextSelection
    {
        public TextSelection(int index = 0, int length = 0, CursorPosition startPosition = null, CursorPosition endPosition = null)
        {
            Index = index;
            Length = length;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
        public TextSelection(CursorPosition startPosition = null, CursorPosition endPosition = null)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        public int Index { get; set; }
        public int Length { get; set; }

        public CursorPosition StartPosition { get; set; }
        public CursorPosition EndPosition { get; set; }

        public new string ToString()
        {
            return StartPosition.LineNumber + ":" + StartPosition.CharacterPosition + " | " + EndPosition.LineNumber + ":" + EndPosition.CharacterPosition;
        }
    }

    public class TextSelectionPosition
    {
        public TextSelectionPosition(int Index = 0, int Length = 0)
        {
            this.Index = Index;
            this.Length = Length;
        }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}