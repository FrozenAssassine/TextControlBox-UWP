using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace TextControlBox.Renderer
{
    internal class TextRenderer
    {
        public static CanvasTextLayout CreateTextResource(ICanvasResourceCreatorWithDpi ResourceCreator, CanvasTextLayout TextLayout, CanvasTextFormat TextFormat, string Text, Size targetSize, float ZoomedFontSize)
        {
            if (TextLayout != null)
                TextLayout.Dispose();
            return CreateTextLayout(ResourceCreator, TextFormat, Text, targetSize);
        }
        public static CanvasTextFormat CreateCanvasTextFormat(float ZoomedFontSize, FontFamily FontFamily)
        {
            CanvasTextFormat textFormat;
            textFormat = new CanvasTextFormat()
            {
                FontSize = ZoomedFontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                WordWrapping = CanvasWordWrapping.NoWrap,
                LineSpacing = ZoomedFontSize + 2,
            };
            textFormat.IncrementalTabStop = ZoomedFontSize * 3; //default 137px
            textFormat.FontFamily = FontFamily.Source;
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
        public static CanvasTextFormat CreateLinenumberTextFormat(float FontSize, FontFamily FontFamily)
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
            textFormat.FontFamily = FontFamily.Source;
            textFormat.TrimmingGranularity = CanvasTextTrimmingGranularity.None;
            textFormat.TrimmingSign = CanvasTrimmingSign.None;
            return textFormat;
        }
    }
}
