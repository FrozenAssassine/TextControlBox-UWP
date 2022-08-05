using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox_TestApp.TextControlBox.Renderer
{
    public class LineHighlighter
    {
        public static void Render(float CanvasWidth, CanvasTextLayout TextLayout, float XOffset, float Y, float FontSize, CanvasDrawEventArgs args, CanvasSolidColorBrush BackgroundBrush)
        {
            if (TextLayout == null)
                return;

            args.DrawingSession.FillRectangle(0, Y, CanvasWidth, FontSize, BackgroundBrush);
        }
    }
}
