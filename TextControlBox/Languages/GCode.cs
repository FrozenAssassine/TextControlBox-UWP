using TextControlBox.Helper;
using TextControlBox.Renderer;
using Windows.UI;

namespace TextControlBox.Languages
{
    public class GCode : CodeLanguage
    {
        Color green = Color.FromArgb(255, 40, 255, 0);
        Color pink = Color.FromArgb(255, 255, 0, 220);
        Color aqua = Color.FromArgb(255, 0, 255, 255);

        public GCode()
        {
            Highlights.Add(new SyntaxHighlights(@"^([1-9]\d*|0)(\.\d+)?$", green)); //Numbers
            Highlights.Add(new SyntaxHighlights(@"[G|M]+[0-999].*?[\s|\n]", pink)); //M000-Mxxx | G000 - Gxxx
            Highlights.Add(new SyntaxHighlights(@"X|x|Y|y|Z|z", aqua)); //M000-Mxxx | G000 - Gxxx
        }
    }
}
