using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System.Collections.Generic;
using Windows.Foundation;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class Utils
    {
        public static Size MeasureTextSize(CanvasDevice device, string text, CanvasTextFormat textFormat, float limitedToWidth = 0.0f, float limitedToHeight = 0.0f)
        {
            CanvasTextLayout layout = new CanvasTextLayout(device, text, textFormat, limitedToWidth, limitedToHeight);
            return new Size(layout.DrawBounds.Width, layout.DrawBounds.Height);
        }

        public static Size MeasureLineLenght(CanvasDevice device, Line line, CanvasTextFormat textFormat)
        {
            string text = line.Content;

            //If the text starts with a tab or a whitespace, replace them with the last character of the line, to
            //get the actual width of the line, because tabs and whitespaces at the beginning are not counted to the lenght
            double WidthOfPlaceHolder = 0;

            if (text.StartsWith('\t') || text.StartsWith(' '))
            {
                text = text.Insert(0, "|");
                WidthOfPlaceHolder = MeasureTextSize(device, "|", textFormat).Width;
            }
            CanvasTextLayout layout = new CanvasTextLayout(device, text, textFormat, 0, 0);
            return new Size(layout.DrawBounds.Width - WidthOfPlaceHolder, layout.DrawBounds.Height);
        }

        //Get the longest line and create a string with all the content, that is in the textbox, to save performance by iterating just one time throught the list
        public static int GetLongestLineIndex(List<Line> TotalLines)
        {
            int LongestIndex = 0;
            int OldLenght = 0;
            for (int i = 0; i < TotalLines.Count; i++)
            {
                var lenght = TotalLines[i].Length;
                if (lenght > OldLenght)
                {
                    LongestIndex = i;
                    OldLenght = lenght;
                }
            }
            return LongestIndex;
        }

        public static bool CursorPositionsAreEqual(CursorPosition First, CursorPosition Second)
        {
            return First.LineNumber == Second.LineNumber && First.CharacterPosition == Second.CharacterPosition;
        }
        public static bool IndexIsInRangeOf(List<Line> Lines, int Index)
        {
            return Index < Lines.Count && Index > -1;
        }

        public static string[] SplitAt(string Text, int Index)
        {
            string First = Index < Text.Length ? Text.Remove(Index) : Text;
            string Second = Index < Text.Length ? Text.Substring(Index) : "";
            return new string[] { First, Second };
        }

        public static Line GetLineFromList(int Index, List<Line> TotalLines)
        {
            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());

            return TotalLines[Index >= TotalLines.Count ? TotalLines.Count - 1 : Index >= 0 ? Index : 0];
        }

        public static List<Line> GetLinesFromList(List<Line> TotalLines, int Index, int Count)
        {
            if (Index >= TotalLines.Count)
            {
                Index = TotalLines.Count - 1;
                Count = 0;
            }
            else if(Index + Count - 1 >= TotalLines.Count)
            {
                int difference = TotalLines.Count - Index + Count - 1;
                if (difference <= 0)
                    Count += difference;
            }
            return TotalLines.GetRange(Index, Count);
        }
    }
}
