using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace TextControlBox.Text
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
            if (Text.IndexOf("\r\n") > -1)
            {
                return LineEnding.CRLF;
            }
            else if (Text.IndexOf("\n") > -1)
            {
                return LineEnding.LF;
            }
            else if (Text.IndexOf("\r") > -1)
            {
                return LineEnding.CR;
            }
            else {
                return LineEnding.CRLF;
            }
        }
        public static string ChangeLineEndings(string Text, LineEnding LineEnding)
        {
            LineEnding CurrentLineEnding = FindLineEnding(Text);
            Debug.WriteLine(CurrentLineEnding);
            string LineEndingToInsert = LineEndingToString(LineEnding);
            StringBuilder sb = new StringBuilder();
            var Splitted = Text.Split(LineEndingToString(CurrentLineEnding));
            for (int i = 0; i < Splitted.Length; i++)
            {
                sb.Append(Splitted[i] + (i == Splitted.Length - 1 ? "" : LineEndingToInsert));
            }
            return sb.ToString();
        }
        public static string CleanLineEndings(string Text, LineEnding lineEnding)
        {
            Text = Regex.Replace(Text, "\r\n", "\n");
            Text = Regex.Replace(Text, "\r", "\n");
            return Regex.Replace(Text, "\n", LineEndingToString(lineEnding));

            //return Text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", LineEndingToString(lineEnding));
        }
    }
    public enum LineEnding
    {
        LF, CRLF, CR
    }
}
