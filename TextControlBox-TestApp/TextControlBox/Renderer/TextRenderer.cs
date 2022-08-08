using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TextControlBox_TestApp.TextControlBox.Renderer
{
    public class TextRenderer
    {
        public static CanvasTextLayout CreateTextResource(ICanvasResourceCreatorWithDpi ResourceCreator, CanvasTextLayout TextLayout, CanvasTextFormat TextFormat, string Text, Size targetSize)
        {
            if (TextLayout != null)
                TextLayout.Dispose();
            return CreateTextLayout(ResourceCreator, TextFormat, Text, targetSize);
        }
        public static CanvasTextFormat CreateCanvasTextFormat(float FontSize)
        {
            CanvasTextFormat textFormat;
            textFormat = new CanvasTextFormat()
            {
                FontSize = FontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                WordWrapping = CanvasWordWrapping.NoWrap,
                LineSpacing = FontSize + 2,  
            };
            Debug.WriteLine("Default tabsize: " + textFormat.IncrementalTabStop);
            textFormat.IncrementalTabStop = 137;
            textFormat.FontFamily = "Consolas";
            textFormat.TrimmingGranularity = CanvasTextTrimmingGranularity.None;
            textFormat.TrimmingSign = CanvasTrimmingSign.None;
            return textFormat;
        }
        public static CanvasTextLayout CreateTextLayout(ICanvasResourceCreator ResourceCreator, CanvasTextFormat TextFormat, string Text, Size CanvasSize)
        {
            return new CanvasTextLayout(ResourceCreator, Text, TextFormat, (float)CanvasSize.Width, (float)CanvasSize.Height);
        }
        public static CanvasTextLayout CreateTextLayout(ICanvasResourceCreator ResourceCreator, CanvasTextFormat TextFormat, string Text, float Width, float Height)
        {
            return new CanvasTextLayout(ResourceCreator, Text, TextFormat, Width, Height);
        }
        public static CanvasTextFormat CreateLinenumberTextFormat(float FontSize)
        {
            CanvasTextFormat textFormat;

            textFormat = new CanvasTextFormat()
            {
                FontSize = FontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Right,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                WordWrapping = CanvasWordWrapping.NoWrap,
                LineSpacing = FontSize + 2,
            };
            textFormat.FontFamily = "Consolas";
            textFormat.TrimmingGranularity = CanvasTextTrimmingGranularity.None;
            textFormat.TrimmingSign = CanvasTrimmingSign.None;
            return textFormat;
        }
    }
}
