using System.Collections.Generic;
using TextControlBox.Helper;
using Windows.UI;

namespace TextControlBox.Languages
{
    public class Csharp : CodeLanguage
    {
        Color green = Color.FromArgb(255, 40, 255, 0);
        Color orange = Color.FromArgb(255, 255, 214, 140);
        Color aqua = Color.FromArgb(255, 0, 255, 255);
        Color gray = Color.FromArgb(255, 100, 100, 100);
        Color pink = Color.FromArgb(255, 255, 0, 220);
        Color yellow = Color.FromArgb(255, 255, 210, 0);
        Color lightgray = Color.FromArgb(255, 120, 120, 120);
        Color red = Color.FromArgb(255, 255, 50, 0);

        readonly IEnumerable<string> Variables = new string[] { "object", "string", "var", "bool", "int", "double", "float", "uint", "long" };
        readonly IEnumerable<string> Attributes = new string[] { "break", "typeof", "this", "null", "class", "get", "set", "init", "void", "const", "readonly", "class", "sealed", "partial", "namespace", "while", "foreach", "for", "if", "else", "true", "false", "new", "private", "public", "protected", "override", "using", "return", "static", "switch", "case", "do", "enum" };
        readonly IEnumerable<string> Classes = new string[] { "List", "Color", "Console", "Debug" };
        readonly IEnumerable<string> Regions = new string[] { "#region", "#endregion", "#elif", "#else", "#endif", "#error", "#warning", "#line", "#nullable", "#if" };

        public Csharp()
        {
            Name = "C#";

            Highlights.Add(new SyntaxHighlights(@"\b(?:0x[a-f0-9]+|(?:\d(?:_\d+)*\d*(?:\.\d*)?|\.\d\+)(?:e[+\-]?\d+)?)\b", green)); //Numbers

            Highlights.Add(new SyntaxHighlights(@"(?<!(def\s))(?<=^|\s)[a-zA-Z_][\w_]*(?=\()", pink));

            Highlights.Add(new SyntaxHighlights($"(?i)\\b({string.Join('|', Attributes)})\\b", aqua)); //Attributes
            Highlights.Add(new SyntaxHighlights($"(?i)\\b({string.Join('|', Variables)})\\b", aqua)); //Variables
            Highlights.Add(new SyntaxHighlights($"(?i)\\b({string.Join('|', Classes)})\\b", pink));//Classes
            Highlights.Add(new SyntaxHighlights($"(?i)\\b(try|catch|finally)\\b", red));//try catch finally

            //Strings
            Highlights.Add(new SyntaxHighlights(@"""[^\n]*?""", orange));
            Highlights.Add(new SyntaxHighlights(@"'[^\n]*?'", orange));
            Highlights.Add(new SyntaxHighlights(@"(?s)(\""\""\"")(.*?)(\""\""\"")", orange));

            Highlights.Add(new SyntaxHighlights(@"(\/\/.+?$|\/\*.+?\*\/)", gray)); //Multiline comment
            Highlights.Add(new SyntaxHighlights(@"//(.*?)\r?\n", gray)); //Single line comment
            Highlights.Add(new SyntaxHighlights($"(?<=({string.Join('|', Variables)}) ).*(?=" + " {|=)", yellow)); //Variable-names
            Highlights.Add(new SyntaxHighlights($"(?i)\\b({string.Join('|', Regions)})\\b", lightgray)); //Regions
            Highlights.Add(new SyntaxHighlights(";", lightgray)); //Semicolons
        }
    }
}
