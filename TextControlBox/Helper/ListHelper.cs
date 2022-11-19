using Collections.Pooled;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TextControlBox.Extensions;
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
            if (Index + Count >= TotalLines.Count)
            {
                int difference = TotalLines.Count - Index;
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

        public static IEnumerable<Line> GetReadOnlyLines(PooledList<Line> TotalLines, int Index, int Count)
        {
            var res = CheckValues(TotalLines, Index, Count);
            return TotalLines.Skip(res.Index).Take(res.Count);
        }
        public static PooledList<Line> GetLines(PooledList<Line> TotalLines, int Index, int Count)
        {
            var res = CheckValues(TotalLines, Index, Count);
            Debug.WriteLine("GetLines " + res.Index + ":" + res.Count);
            return TotalLines.Skip(res.Index).Take(res.Count).ToPooledList();
        }

        private static System.Span<Line> GetLinesAsSpan(PooledList<Line> TotalLines, int Index, int Count)
        {
            var res = CheckValues(TotalLines, Index, Count);
            return TotalLines.GetRange(res.Index, res.Count);
        }
        
        public static string GetLinesAsString(PooledList<Line> TotalLines, int Index, int Count, string NewLineCharacter)
        {
            if(Count == 1)
            {
                return GetLine(TotalLines, Index).Content;
            }
            return GetLinesAsString(GetLinesAsSpan(TotalLines, Index, Count), NewLineCharacter);
        }
        public static string GetLinesAsString(PooledList<Line> Lines, string NewLineCharacter)
        {
            return string.Join(NewLineCharacter, Lines.Select(x => x.Content));
        }
        public static string GetLinesAsString(System.Span<Line> Lines, string NewLineCharacter)
        {
            return string.Join(NewLineCharacter, Lines.ToArray().Select(x => x.Content));
        }
        
        public static IEnumerable<Line> GetLinesFromString(string content, string NewLineCharacter)
        {
            return CreateLines(content.Split(NewLineCharacter));
        }
        
        public static string[] GetStringLinesFromString(string content, string NewLineCharacter)
        {
            return content.Split(NewLineCharacter);
        }

        public static IEnumerable<Line> CreateLines(string[] lines)
        {
            return CreateLines(lines, 0);
        }
        public static IEnumerable<Line> CreateLines(string[] lines, int Start, string Beginning, string End)
        {
            //Paste the text
            for (int i = Start; i < lines.Length; i++)
            {
                if (i == 0)
                    yield return new Line(Beginning + lines[i]);
                else if (i == lines.Length - 1)
                {
                    yield return new Line(lines[i] + End);
                }
                else
                    yield return new Line(lines[i]);
            }
        }
        public static IEnumerable<Line> CreateLines(string[] lines, int Start, int Count = -1)
        {
            //Paste the text
            for (int i = Start; i < (Count == -1 || Count >= lines.Length ? lines.Length : Count); i++)
            {
                yield return new Line(lines[i]);
            }
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
        public static void InsertRange(PooledList<Line> TotalLines, IEnumerable<Line> Lines, int Position)
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