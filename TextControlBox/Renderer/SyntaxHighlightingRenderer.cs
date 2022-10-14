using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextControlBox.Helper;
using Windows.UI;

namespace TextControlBox.Renderer
{
    public class SyntaxHighlightingRenderer
    {
        public static void UpdateSyntaxHighlighting(CanvasTextLayout DrawnTextLayout, CodeLanguage CodeLanguage, bool SyntaxHighlighting, string RenderedText)
        {
            if (CodeLanguage == null || !SyntaxHighlighting)
                return;

            var Highlights = CodeLanguage.Highlights;
            for (int i = 0; i < Highlights.Count; i++)
            {
                var matches = Regex.Matches(RenderedText, Highlights[i].Pattern);
                for (int j = 0; j < matches.Count; j++)
                {
                    DrawnTextLayout.SetColor(matches[j].Index, matches[j].Length, Highlights[i].Color);
                }
            }
        }

    }
    public class SyntaxHighlights
    {
        public SyntaxHighlights(string Pattern, Windows.UI.Color Color)
        {
            this.Pattern = Pattern;
            this.Color = Color;
        }

        public string Pattern { get; set; }
        public Color Color { get; set; }
    }
}
