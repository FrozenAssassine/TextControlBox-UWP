using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;

namespace TextControlBox_TestApp.TextControlBox.Renderer
{
    public class CursorRenderer
    {
        public static int GetCursorLineFromPoint(Point Point, float SingleLineHeight, int NumberOfRenderedLines, int NumberOfStartLine, int NumberOfUnderedLines)
        {
            //Calculate the relative linenumber, where the pointer was pressed at
            int Linenumber = (int)(Point.Y / SingleLineHeight) + 1;

            if (Linenumber > NumberOfStartLine + NumberOfRenderedLines)
                Linenumber = NumberOfStartLine + NumberOfRenderedLines;

            return Linenumber + NumberOfUnderedLines;
        }
        public static int GetCharacterPositionFromPoint(Line CurrentLine, CanvasTextLayout TextLayout, Point CursorPosition, float MarginLeft)
        {
            if (CurrentLine == null)
                return 0;

            TextLayout.HitTest(
                (float)CursorPosition.X - MarginLeft, 0,
                out var textLayoutRegion);
            return textLayoutRegion.CharacterIndex;
        }

        //Return the cursor Width
        public static void RenderCursor(CanvasTextLayout TextLayout, int CharacterPosition, float XOffset, float Y, float FontSize, CanvasDrawEventArgs args, CanvasSolidColorBrush CursorColorBrush)
        {
            if (TextLayout == null)
                return;
            Vector2 vector = TextLayout.GetCaretPosition(CharacterPosition == -1 ? 0 : CharacterPosition, false);
            args.DrawingSession.FillRectangle(vector.X + XOffset, Y, 1, FontSize, CursorColorBrush);
        }
        public static double GetCursorOverNewLine(CanvasTextLayout TextLayout, int Cursor_CharacterPosition)
        {
            if (TextLayout == null)
                return -1;

            CanvasTextLayoutRegion[] descriptions = TextLayout.GetCharacterRegions(Cursor_CharacterPosition, 1);
            if (descriptions.Length > 0)
            {
                return descriptions[0].LayoutBounds.Width;
            }
            return -1;
        }
    }
}
