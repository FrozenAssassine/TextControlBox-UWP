using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TextControlBox.Text;

namespace TextControlBox.Helper
{
    public class ListHelper
    {
        public struct ValueResult
        {
            public ValueResult(int Index, int Count)
            {
                this.Index = Index;
                this.Count = Count;
            }
            public int Index;
            public int Count;
        }
        private static ValueResult CheckValues(PooledList<Line> TotalLines, int Index, int Count)
        {
            if (Index >= TotalLines.Count)
            {
                Index = TotalLines.Count - 1 < 0 ? 0 : TotalLines.Count - 1;
                Count = 0;
            }
            if (Index + Count - 1 >= TotalLines.Count)
            {
                int difference = TotalLines.Count - Index - 1;
                if (difference >= 0)
                    Count = difference;
            }

            if (Count < 0)
                Count = 0;
            if (Index < 0)
                Index = 0;

            return new ValueResult(Index, Count);
        }

        public static void Clear(PooledList<Line> TotalLines, bool AddNewLine = false)
        {
            TotalLines.Clear();
            TotalLines.TrimExcess();

            if (AddNewLine)
            {
                TotalLines.Add(new Line());            
            }
        }

        public static Line GetLine(PooledList<Line> TotalLines, int Index)
        {
            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());

            if (Index == -1)
                return TotalLines[TotalLines.Count - 1];

            return TotalLines[Index >= TotalLines.Count ? TotalLines.Count - 1 : Index >= 0 ? Index : 0];
        }

        public static PooledList<Line> GetLines(PooledList<Line> TotalLines, int Index, int Count)
        {
            var res = CheckValues(TotalLines, Index, Count);
            return TotalLines.GetRange(res.Index, res.Count).ToPooledList();
        }

        public static string GetLinesAsString(PooledList<Line> TotalLines, int Index, int Count, string NewLineCharacter)
        {
            return GetLinesAsString(GetLines(TotalLines, Index, Count), NewLineCharacter);
        }
        public static string GetLinesAsString(PooledList<Line> Lines, string NewLineCharacter)
        {
            return string.Join(NewLineCharacter, Lines.Select(a => a.Content));
        }

        public static PooledList<Line> GetLinesFromString(string content, string NewLineCharacter)
        {
            var Splitted = content.Split(NewLineCharacter);
            PooledList<Line> Content = new PooledList<Line>(Splitted.Length);
            for (int i = 0; i < Splitted.Length; i++)
            {
                Content.Add(new Line(Splitted[i]));
            }
            return Content;
        }

        public static string[] GetStringLinesFromString(string content, string NewLineCharacter)
        {
            return content.Split(NewLineCharacter);
        }

        public static void Insert(PooledList<Line> TotalLines, Line Line, int Position)
        {
            if (Position >= TotalLines.Count || Position == -1)
                TotalLines.Add(Line);
            else
                TotalLines.Insert(Position, Line);
        }

        public static void InsertRange(PooledList<Line> TotalLines, PooledList<Line> Lines, int Position)
        {
            if (Position >= TotalLines.Count)
                TotalLines.AddRange(Lines);
            else
                TotalLines.InsertRange(Position < 0 ? 0 : Position, Lines);
        }

        public static void RemoveRange(PooledList<Line> TotalLines, int Index, int Count)
        {
            var res = CheckValues(TotalLines, Index, Count);
            TotalLines.RemoveRange(res.Index, res.Count);
            TotalLines.TrimExcess();
        }
        public static void DeleteAt(PooledList<Line> TotalLines, int Index)
        {
            if (Index >= TotalLines.Count)
                Index = TotalLines.Count - 1 < 0 ? TotalLines.Count - 1 : 0;

            TotalLines.RemoveAt(Index);
            TotalLines.TrimExcess();
        }
    }
}