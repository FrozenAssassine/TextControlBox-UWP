using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace TextControlBox.Text
{
    public class Line
    {
        private string _Content = "";
        public string Content { get => _Content; set { _Content = value; } }
        public int Length => Content.Length;

        public Line(string Content = "")
        {
            _Content = Content;
        }
        public void SetText(string Value)
        {
            _Content = Value;
        }
        public void AddText(string Value, int Position)
        {
            if (Position < 0)
                Position = 0;

            if (Position >= _Content.Length || Length <= 0)
                _Content += Value;
            else
                _Content = _Content.Insert(Position, Value);
        }
        public void AddToEnd(string Value)
        {
            _Content += Value;
        }
        public void AddToStart(string Value)
        {
            _Content = _Content.Insert(0, Value);
        }
        public string Remove(int Index, int Count = -1)
        {
            if (Index >= Length || Index < 0)
                return _Content;

            if (Count <= -1)
                _Content = _Content.Remove(Index);
            else
                _Content = _Content.Remove(Index, Count);

            return _Content;
        }
        public string Substring(int Index, int Count = -1)
        {
            if (Index >= Length)
                _Content = "";
            else if (Count == -1)
                _Content = _Content.Substring(Index);
            else
                _Content = _Content.Substring(Index, Count);
            return _Content;
        }
        public void ReplaceText(int Start, int End, string Text)
        {
            int end = Math.Max(Start, End);
            int start = Math.Min(Start, End);

            if (start == 0 && end >= Length)
                _Content = "";
            else
            {
                _Content = _Content.Remove(End) + Text + _Content.Remove(0, start);
            }
        }
    }
}
