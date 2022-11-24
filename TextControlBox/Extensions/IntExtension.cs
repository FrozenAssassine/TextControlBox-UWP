using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox.Extensions
{
    internal static class IntExtension
    {
        public static bool IsInRange(this int value, int start, int count)
        {
            return value >= start && value <= start + count;
        }
    }
}
