using System;
using System.Diagnostics;

namespace TextControlBox.Text
{
    public class Line
    {
        private string _Content = "";
        public string Content { get => _Content; set { _Content = value; this.Length = value.Length; } }
        public int Length { get; private set; }

        public Line(string Content = "")
        {
            this.Content = Content;
        }
        public void SetText(string Value)
        {
            Content = Value;
        }
        public void AddText(string Value, int Position)
        {
            if (Position < 0)
                Position = 0;

            if (Position >= Content.Length)
                Content += Value;
            else if (Length <= 0)
                AddToEnd(Value);
            else
                Content = Content.Insert(Position, Value);
        }
        public void AddToEnd(string Value)
        {
            Content += Value;
        }
        public void AddToStart(string Value)
        {
            Content = Content.Insert(0, Value);
        }
        public string Remove(int Index, int Count = -1)
        {
            if (Index >= Length || Index < 0)
                return Content;

            if (Count <= -1)
                Content = Content.Remove(Index);
            else
                Content = Content.Remove(Index, Count);

            return Content;
        }
        public string Substring(int Index, int Count = -1)
        {
            if (Index >= Length)
                Content = "";
            else if (Count == -1)
                Content = Content.Substring(Index);
            else
                Content = Content.Substring(Index, Count);
            return Content;
        }
        public void ReplaceText(int Start, int End, string Text)
        {
            int end = Math.Max(Start, End);
            int start = Math.Min(Start, End);

            if (start == 0 && end >= Length)
                Content = "";
            else
            {
                Content = Content.Remove(End) + Text + Content.Remove(0, start);
            }
        }
    }
}
