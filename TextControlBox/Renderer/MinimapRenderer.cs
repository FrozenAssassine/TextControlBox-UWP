using Collections.Pooled;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextControlBox.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace TextControlBox.Renderer
{
    internal class MinimapRenderer
    {
        private float FontSize = 1.5f;
        private float LineHeight = 0;

        private CanvasTextFormat TextFormat = null;
        private FontFamily FontFamily = new FontFamily("Consolas");
        
        public int CalculateNumberOfLinesOnScreen(CanvasControl sender)
        {
            return (int)Math.Abs(sender.Size.Height / LineHeight);
        }
        
        public (CanvasTextLayout textLayout, string text) CreateTextLayout(TextControlBox textbox, PooledList<string> TotalLines, int startline, int renderlinesCount, CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (TextFormat == null)
                TextFormat = TextRenderer.CreateCanvasTextFormat(FontSize, (float)FontSize * 1.5f, FontFamily);

            LineHeight = FontSize * 1.5f;

            int screenLines = CalculateNumberOfLinesOnScreen(sender);

            var lines = TotalLines.GetLines_Large(startline / (screenLines / renderlinesCount), screenLines);
            string text = lines.GetString("\n");
            return (TextRenderer.CreateTextLayout(args.DrawingSession, TextFormat, text, new Windows.Foundation.Size { Height = textbox.ActualHeight, Width = sender.Size.Width}), text);
        }
    }
}
