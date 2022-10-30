using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TextControlBox.Extensions
{
    public static class ColorExtension
    {
        public static Windows.UI.Color ToMediaColor(this System.Drawing.Color drawingColor)
        {
            return Windows.UI.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
        public static string ToHex(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
