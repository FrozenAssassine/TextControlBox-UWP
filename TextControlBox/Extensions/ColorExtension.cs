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
    }
}
