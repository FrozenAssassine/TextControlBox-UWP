namespace TextControlBox.Text
{
    public class CursorPosition
    {
        public CursorPosition(int CharacterPosition = 0, int LineNumber = 0)
        {
            this.CharacterPosition = CharacterPosition;
            this.LineNumber = LineNumber;
        }
        public CursorPosition(CursorPosition CurrentCursorPosition)
        {
            if (CurrentCursorPosition == null)
                return;
            this.CharacterPosition = CurrentCursorPosition.CharacterPosition;
            this.LineNumber = CurrentCursorPosition.LineNumber;
        }
        public int CharacterPosition { get; set; } = 0;
        public int LineNumber { get; set; } = 0;
        public CursorPosition Change(int CharacterPosition, int LineNumber)
        {
            this.CharacterPosition = CharacterPosition;
            this.LineNumber = LineNumber;
            return this;
        }
        public void AddToCharacterPos(int Add)
        {
            CharacterPosition += Add;
        }
        public void SubtractFromCharacterPos(int Value)
        {
            CharacterPosition -= Value;
        }
        public new string ToString()
        {
            return LineNumber + ":" + CharacterPosition;
        }

        public CursorPosition ChangeLineNumber(int LineNumber)
        {
            this.LineNumber = LineNumber;
            return this;
        }

        public static CursorPosition ChangeCharacterPosition(CursorPosition CurrentCursorPosition, int CharacterPosition)
        {
            return new CursorPosition(CharacterPosition, CurrentCursorPosition.LineNumber);
        }
        public static CursorPosition ChangeLineNumber(CursorPosition CurrentCursorPosition, int LineNumber)
        {
            return new CursorPosition(CurrentCursorPosition.CharacterPosition, LineNumber);
        }
    }
}
