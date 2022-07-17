using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using TextControlBox_TestApp.TextControlBox.Renderer;

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
        public static int GetLongestLineLenght(List<Line> Lines)
        {
            int OldLenght = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                var lenght = Lines[i].Content.Length;
                if (lenght > OldLenght)
                {
                    OldLenght = lenght;
                }
            }
            return OldLenght;
        }

        public static bool CursorPositionsAreEqual(CursorPosition First, CursorPosition Second)
        {
           return First.LineNumber == Second.LineNumber && First.CharacterPosition == Second.CharacterPosition;
        }
        public static bool IndexIsInRangeOf(List<Line> Lines, int Index)
        {
            return Index < Lines.Count && Index > -1;
        }
    }
}
