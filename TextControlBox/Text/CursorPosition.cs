namespace TextControlBox.Text
{
    public class CursorPosition
    {
        internal CursorPosition(int CharacterPosition = 0, int LineNumber = 0)
        {
            this.CharacterPosition = CharacterPosition;
            this.LineNumber = LineNumber;
        }
        internal CursorPosition(CursorPosition CurrentCursorPosition)
        {
            if (CurrentCursorPosition == null)
                return;
            this.CharacterPosition = CurrentCursorPosition.CharacterPosition;
            this.LineNumber = CurrentCursorPosition.LineNumber;
        }
        public int CharacterPosition { get; internal set; } = 0;
        public int LineNumber { get; internal set; } = 0;
        internal CursorPosition Change(int CharacterPosition, int LineNumber)
        {
            this.CharacterPosition = CharacterPosition;
            this.LineNumber = LineNumber;
            return this;
        }
        internal void AddToCharacterPos(int Add)
        {
            CharacterPosition += Add;
        }
        internal void SubtractFromCharacterPos(int Value)
        {
            CharacterPosition -= Value;
        }
        internal new string ToString()
        {
            return LineNumber + ":" + CharacterPosition;
        }

        internal CursorPosition ChangeLineNumber(int LineNumber)
        {
            this.LineNumber = LineNumber;
            return this;
        }

        internal static CursorPosition ChangeCharacterPosition(CursorPosition CurrentCursorPosition, int CharacterPosition)
        {
            return new CursorPosition(CharacterPosition, CurrentCursorPosition.LineNumber);
        }
        internal static CursorPosition ChangeLineNumber(CursorPosition CurrentCursorPosition, int LineNumber)
        {
            return new CursorPosition(CurrentCursorPosition.CharacterPosition, LineNumber);
        }
    }
}
