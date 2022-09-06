using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class ListHelper
    {
        public static Line GetLine(List<Line> TotalLines, int Index)
        {
            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());

            if (Index == -1)
                return TotalLines[TotalLines.Count - 1];

            return TotalLines[Index >= TotalLines.Count ? TotalLines.Count - 1 : Index >= 0 ? Index : 0];
        }

        public static List<Line> GetLines(List<Line> TotalLines, int Index, int Count)
        {
            if (Index >= TotalLines.Count)
            {
                Index = TotalLines.Count - 1;
                Count = 0;
            }
            else if (Index + Count - 1 >= TotalLines.Count)
            {
                int difference = TotalLines.Count - Index + Count - 1;
                if (difference <= 0)
                    Count += difference;
            }
            return TotalLines.GetRange(Index, Count);
        }

        public static string GetLinesAsString(List<Line> TotalLines, int Index, int Count, string NewLineCharacter)
        {
            return string.Join(NewLineCharacter, GetLines(TotalLines, Index, Count).Select(a => a.Content));
        }
        public static string GetLinesAsString(List<Line> Lines, string NewLineCharacter)
        {
            return string.Join(NewLineCharacter, Lines.Select(a => a.Content));
        }

        public static List<Line> GetLinesFromString(string content, string NewLineCharacter)
        {
            var Splitted = content.Split(NewLineCharacter);
            List<Line> Content = new List<Line>(Splitted.Length);
            for (int i = 0; i < Splitted.Length; i++)
            {
                Content.Add(new Line(Splitted[i]));
            }
            return Content;
        }

        public static void Insert(List<Line> TotalLines, Line Line, int Position)
        {
            if (Position >= TotalLines.Count)
                TotalLines.Add(Line);
            else
                TotalLines.Insert(Position, Line);
        }

        public static void InsertRange(List<Line> TotalLines, List<Line> Lines, int Position)
        {
            if (Position >= TotalLines.Count)
                TotalLines.AddRange(Lines);
            else
                TotalLines.InsertRange(Position < 0 ? 0 : Position, Lines);
        }

        public static void RemoveRange(List<Line> TotalLines, int Index, int Count)
        {
            if (Index >= TotalLines.Count)
            {
                Index = TotalLines.Count - 1;
                Count = 0;
            }
            else if (Index + Count - 1 >= TotalLines.Count)
            {
                int difference = TotalLines.Count - Index + Count - 1;
                if (difference > 0)
                    Count = difference;
            }
            Debug.WriteLine(Index + "::" + Count);
            TotalLines.RemoveRange(Index, Count);
        }
        public static void DeleteAt(List<Line> TotalLines, int Index)
        {
            if (Index >= TotalLines.Count)
                Index = TotalLines.Count - 1;
            if (Index < 0)
                Index = 0;

            TotalLines.RemoveAt(Index);
        }
    }
}
