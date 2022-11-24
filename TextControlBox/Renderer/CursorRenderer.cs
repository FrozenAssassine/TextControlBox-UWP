using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using TextControlBox.Extensions;
using TextControlBox.Text;
using Windows.Foundation;

namespace TextControlBox.Renderer
{
    internal class CursorRenderer
    {
        public static int GetCursorLineFromPoint(Point Point, float SingleLineHeight, int NumberOfRenderedLines, int NumberOfStartLine)
        {
            //Calculate the relative linenumber, where the pointer was pressed at
            int Linenumber = (int)(Point.Y / SingleLineHeight);
            if (Linenumber < 0)
                Linenumber = 0;

            Linenumber += NumberOfStartLine;

            if (Linenumber >= NumberOfStartLine + NumberOfRenderedLines)
                Linenumber = NumberOfStartLine + NumberOfRenderedLines - 1;

            return Linenumber;
        }
        public static int GetCharacterPositionFromPoint(Line CurrentLine, CanvasTextLayout TextLayout, Point CursorPosition, float MarginLeft)
        {
            if (CurrentLine == null || TextLayout == null)
                return 0;

            TextLayout.HitTest(
                (float)CursorPosition.X - MarginLeft, 0,
                out var textLayoutRegion);
            return textLayoutRegion.CharacterIndex;
        }

        //Return the position in pixels of the cursor in the current line
        public static float GetCursorPositionInLine(CanvasTextLayout CurrentLineTextLayout, CursorPosition CursorPosition, float XOffset)
        {
            if (CurrentLineTextLayout == null)
                return 0;

            return CurrentLineTextLayout.GetCaretPosition(CursorPosition.CharacterPosition < 0 ? 0 : CursorPosition.CharacterPosition, false).X + XOffset;
        }

        //Return the cursor Width
        public static void RenderCursor(CanvasTextLayout TextLayout, int CharacterPosition, float XOffset, float Y, float FontSize, CursorSize CustomSize, CanvasDrawEventArgs args, CanvasSolidColorBrush CursorColorBrush)
        {
            if (TextLayout == null)
                return;

            Vector2 vector = TextLayout.GetCaretPosition(CharacterPosition < 0 ? 0 : CharacterPosition, false);
            if (CustomSize == null)
                args.DrawingSession.FillRectangle(vector.X + XOffset, Y, 1, FontSize, CursorColorBrush);
            else
                args.DrawingSession.FillRectangle(vector.X + XOffset + CustomSize.OffsetX, Y + CustomSize.OffsetY, (float)CustomSize.Width, (float)CustomSize.Height, CursorColorBrush);
        }
    }
}
