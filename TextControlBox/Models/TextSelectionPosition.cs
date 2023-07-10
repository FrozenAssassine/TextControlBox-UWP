using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox
{
    public class TextSelectionPosition
    {
        public TextSelectionPosition(int Index = 0, int Length = 0)
        {
            this.Index = Index;
            this.Length = Length;
        }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
