using Collections.Pooled;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Threading.Tasks;
using TextControlBox.Text;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace TextControlBox.Helper
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
            if (text.Length == 0)
                return new Size(0, 0);

            //If the text starts with a tab or a whitespace, replace it with the last character of the line, to
            //get the actual width of the line, because tabs and whitespaces at the beginning are not counted to the lenght
            //Do the same for the end
            double WidthOfPlaceHolder = 0;
            if (text.StartsWith('\t') || text.StartsWith(' '))
            {
                text = text.Insert(0, "|");
                WidthOfPlaceHolder += MeasureTextSize(device, "|", textFormat).Width;
            }
            if (text.EndsWith('\t') || text.EndsWith(' '))
            {
                text = text += "|";
                WidthOfPlaceHolder += MeasureTextSize(device, "|", textFormat).Width;
            }

            CanvasTextLayout layout = new CanvasTextLayout(device, text, textFormat, 0, 0);
            return new Size(layout.DrawBounds.Width - WidthOfPlaceHolder, layout.DrawBounds.Height);
        }

        //Get the longest line and create a string with all the content, that is in the textbox, to save performance by iterating just one time throught the list
        public static int GetLongestLineIndex(PooledList<Line> TotalLines)
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
        public static bool IndexIsInRangeOf(PooledList<Line> Lines, int Index)
        {
            return Index < Lines.Count && Index > -1;
        }

        public static string[] SplitAt(string Text, int Index)
        {
            string First = Index < Text.Length ? Text.Remove(Index) : Text;
            string Second = Index < Text.Length ? Text.Substring(Index) : "";
            return new string[] { First, Second };
        }
        public static Rect GetElementRect(FrameworkElement element)
        {
            return new Rect(element.TransformToVisual(null).TransformPoint(new Point()), new Size(element.ActualWidth, element.ActualHeight));
        }
        public static int CountCharacters(PooledList<Line> TotalLines)
        {
            int Count = 0;
            for (int i = 0; i < TotalLines.Count; i++)
            {
                Count += TotalLines[i].Length + 1;
            }
            return Count - 1;
        }
        public static void ChangeCursor(CoreCursorType CursorType)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CursorType, 0);
        }
        public static bool IsKeyPressed(VirtualKey key)
        {
            return Window.Current.CoreWindow.GetKeyState(key).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static async Task<bool> IsOverTextLimit(int TextLength)
        {
            if (TextLength > 100000000)
            {
                await new MessageDialog("Current textlimit is 100 million characters, but your file has " + TextLength + " characters").ShowAsync();
                return true;
            }
            return false;
        }
    }
}
