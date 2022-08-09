using System;
using System.Text;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class LineEndings
    {
        public static string LineEndingToString(LineEnding LineEnding)
        {
            return LineEnding == LineEnding.LF ? "\n" :
                LineEnding == LineEnding.CRLF ? "\r\n" : "\r";
        }
        public static LineEnding FindLineEnding(string Text)
        {
            if (Text.Contains("\r\n", StringComparison.Ordinal))
            {
                return LineEnding.CRLF;
            }
            else if (Text.Contains("\n", StringComparison.Ordinal))
            {
                return LineEnding.LF;
            }
            else if (Text.Contains("\r", StringComparison.Ordinal))
            {
                return LineEnding.CR;
            }
            else //Defaults
                return LineEnding.CRLF;
        }
        public static string ChangeLineEndings(string Text, LineEnding LineEnding)
        {
            LineEnding CurrentLineEnding = FindLineEnding(Text);
            string LineEndingToInsert = LineEndingToString(LineEnding);
            StringBuilder sb = new StringBuilder();
            var Splitted = Text.Split(LineEndingToString(CurrentLineEnding));
            for (int i = 0; i < Splitted.Length; i++)
            {
                sb.Append(Splitted[i] + (i == Splitted.Length - 1 ? "" : LineEndingToInsert));
            }
            return sb.ToString();
        }
    }
}
