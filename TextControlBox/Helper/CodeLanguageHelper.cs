using System.Collections.Generic;
using TextControlBox.Languages;
using TextControlBox.Renderer;

namespace TextControlBox.Helper
{
    public class CodeLanguageHelper
    {
        public static CodeLanguage GetCodeLanguage(CodeLanguages Languages)
        {
            switch (Languages)
            {
                case CodeLanguages.Csharp:
                    return new Csharp();
                case CodeLanguages.Gcode:
                    return new GCode();
                case CodeLanguages.Html:
                    return new Html();
            }
            return null;
        }
        public static CodeLanguages GetCodeLanguages(CodeLanguage Language)
        {
            if (System.Object.ReferenceEquals(Language, new Csharp()))
                return CodeLanguages.Csharp;
            else if (System.Object.ReferenceEquals(Language, new GCode()))
                return CodeLanguages.Gcode;
            else if (System.Object.ReferenceEquals(Language, new Html()))
                return CodeLanguages.Html;
            else return CodeLanguages.None;
        }
    }
    public enum CodeLanguages
    {
        Csharp, Gcode, Html, None
    }
    public class CodeLanguage
    {
        public string Name { get; set; }
        public List<SyntaxHighlights> Highlights = new List<SyntaxHighlights>();
    }
}
