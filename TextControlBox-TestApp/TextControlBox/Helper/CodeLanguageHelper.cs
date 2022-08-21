using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextControlBox_TestApp.TextControlBox.Languages;

namespace TextControlBox_TestApp.TextControlBox.Helper
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
}
