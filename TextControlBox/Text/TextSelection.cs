﻿namespace TextControlBox.Text
{
    public class TextSelection
    {
        public TextSelection()
        {
            Index = 0;
            Length = 0;
            StartPosition = null;
            EndPosition = null;
        }
        public TextSelection(int index = 0, int length = 0, CursorPosition startPosition = null, CursorPosition endPosition = null)
        {
            Index = index;
            Length = length;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
        public TextSelection(CursorPosition startPosition = null, CursorPosition endPosition = null)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
        public TextSelection(TextSelection textSelection)
        {
            StartPosition = new CursorPosition(textSelection.StartPosition);
            EndPosition = new CursorPosition(textSelection.EndPosition);
            Index = textSelection.Index;
            Length = textSelection.Length;
        }

        public int Index { get; set; }
        public int Length { get; set; }

        public CursorPosition StartPosition { get; set; }
        public CursorPosition EndPosition { get; set; }

        public new string ToString()
        {
            return StartPosition.LineNumber + ":" + StartPosition.CharacterPosition + " | " + EndPosition.LineNumber + ":" + EndPosition.CharacterPosition;
        }
    }

    public class TextSelectionPosition
    {
        public TextSelectionPosition(int Index = 0, int Length = 0)
        {
            this.Index = Index;
            this.Length = Length;
        }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
