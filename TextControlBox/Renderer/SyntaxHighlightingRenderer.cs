using Microsoft.Graphics.Canvas.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextControlBox.Extensions;
using TextControlBox.Helper;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TextControlBox.Renderer
{
    public class SyntaxHighlightingRenderer
    {
        public static FontWeight BoldFont = new FontWeight { Weight = 600 };
        public static FontStyle ItalicFont = FontStyle.Italic;
        public static System.Drawing.ColorConverter ColorConverter = new System.Drawing.ColorConverter();

        public static void UpdateSyntaxHighlighting(CanvasTextLayout DrawnTextLayout, ApplicationTheme theme, CodeLanguage CodeLanguage, bool SyntaxHighlighting, string RenderedText)
        {
            if (CodeLanguage == null || !SyntaxHighlighting)
                return;
            
            var Highlights = CodeLanguage.Highlights;
            for (int i = 0; i < Highlights.Count; i++)
            {
                var matches = Regex.Matches(RenderedText, Highlights[i].Pattern);
                for (int j = 0; j < matches.Count; j++)
                {
                    var highlight = Highlights[i];
                    var match = matches[j];

                    var color = (System.Drawing.Color)ColorConverter.ConvertFromString(theme == ApplicationTheme.Light ? highlight.ColorLight : highlight.ColorDark);
                    DrawnTextLayout.SetColor(match.Index, match.Length, color.ToMediaColor());

                    if (highlight.CodeStyle != null)
                    {
                        if (highlight.CodeStyle.Italic)
                            DrawnTextLayout.SetFontStyle(match.Index, match.Length, ItalicFont);
                        else if (highlight.CodeStyle.Bold)
                            DrawnTextLayout.SetFontWeight(match.Index, match.Length, BoldFont);
                        else if(highlight.CodeStyle.Underlined)
                            DrawnTextLayout.SetUnderline(match.Index, matches[j].Length, true);
                    }
                }
            }
        }

        public static CodeLanguage GetCodeLanguageFromJson(string Json)
        {
            try
            {
                return JsonConvert.DeserializeObject<CodeLanguage>(Json);
            }
            catch (JsonReaderException ex)
            {
                return null;
            }
            catch (JsonSerializationException ex)
            {
                return null;
            }
        }

        public static async Task<JsonLoadResult> LoadFromFile(string Path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path);
            if (file == null)
                return new JsonLoadResult(JsonLoadMessage.FileNotFound, null);

            try
            {
                using (Stream stream = await file.OpenStreamForReadAsync())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var res = GetCodeLanguageFromJson(reader.ReadToEnd());
                        return new JsonLoadResult(res == null ? JsonLoadMessage.JsonParseError : JsonLoadMessage.Success, res);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new JsonLoadResult(JsonLoadMessage.NoAccessToPath, null);
            }
        }
    }
    public class JsonLoadResult
    {
        public JsonLoadResult(JsonLoadMessage Message, CodeLanguage CodeLanguage)
        {
            this.JsonLoadMessage = Message;
            this.CodeLanguage = CodeLanguage;
        }
        public JsonLoadMessage JsonLoadMessage { get; set;}
        public CodeLanguage CodeLanguage { get; set;}
    }
    public enum JsonLoadMessage
    {
        Success, FileNotFound, NoAccessToPath, JsonParseError
    }
    public class SyntaxHighlights
    {
        public SyntaxHighlights(string Pattern, string ColorLight, string ColorDark, bool Bold = false, bool Italic = false, bool Underlined = false)
        {
            this.Pattern = Pattern;
            this.ColorDark = ColorDark;
            this.ColorLight = ColorLight;
            if(Underlined || Italic || Bold)
                this.CodeStyle = new CodeFontStyle(Underlined, Italic, Bold);
        }
        public CodeFontStyle CodeStyle { get; set; } = null;
        public string Pattern { get; set; }
        public string ColorDark { get; set; }
        public string ColorLight { get; set; }
    }
    public class CodeFontStyle
    {
        public CodeFontStyle(bool Underlined, bool Italic, bool Bold)
        {
            this.Italic = Italic;
            this.Bold = Bold;
            this.Underlined = Underlined;
        }
        public bool Underlined { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
    }
    public class CodeLanguage
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public List<SyntaxHighlights> Highlights = new List<SyntaxHighlights>();
    }
}
