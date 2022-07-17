using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Popups;

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
            for (int i = 0; i< Splitted.Length; i++)
            {
                sb.Append(Splitted[i] + (i == Splitted.Length - 1 ? "" : LineEndingToInsert));
            }
            return sb.ToString();

            //THIS doesn't work, because it replaces the replaced occurences again and again...
            //if (LineEnding == LineEnding.CR) //-> \r
            //    return Text.Replace('\n', '\r').Replace("\r\n", "\r");
            //else if (LineEnding == LineEnding.LF) //-> \n
            //    return Text.Replace('\r', '\n').Replace("\r\n", "\n");
            //else if (LineEnding == LineEnding.CRLF) //-> \r
            //    return Text.Replace("", Text.Replace("\r", "\r\n").Replace("\n", "\r\n");
            //else
            //{
            //    Debug.WriteLine("Could not replace any NewLine character");
            //    return Text;
            //}
        }
    }
}
