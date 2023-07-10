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
        /// <summary>
        /// The characterposition of the cursor in the current line
        /// </summary>
        public int CharacterPosition { get; internal set; } = 0;
        /// <summary>
        /// The linenumber in which the cursor currently is
        /// </summary>
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
            if (CharacterPosition < 0)
                CharacterPosition = 0;
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
