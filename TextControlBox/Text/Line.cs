using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace TextControlBox.Text
{
    public static class Helper
    {
        public static byte[] GetBytes(this string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
        public static string GetString(this byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
    }

    public class LineByte
    {
        private byte[] Bytes = null;

        public string Content { get => Bytes.GetString(); set { Bytes = value.GetBytes(); this.Length = value.Length; } }
        public int Length { get; private set; }

        public LineByte(string Content = "")
        {
            Bytes = Content.GetBytes();
            //this.Content = Content;
        }
        public void SetText(string Value)
        {
            Content = Value;
        }
        public void AddText(string Value, int Position)
        {
            if (Position < 0)
                Position = 0;

            if (Position >= Content.Length || Length <= 0)
                Content += Value;
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
