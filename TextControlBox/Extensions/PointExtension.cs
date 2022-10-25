using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TextControlBox.Extensions
{
    public static class PointExtension
    {
        public static Point Subtract(this Point point, double subtractX, double subtractY)
        {
            return new Point(point.X - subtractX, point.Y - subtractY);
        }
        public static Point Subtract(this Point point, Point subtract)
        {
            return new Point(point.X - subtract.X, point.Y - subtract.Y);
        }
    }
}
