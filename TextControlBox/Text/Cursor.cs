using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class Cursor
    {
        private static bool ControlIsPressed { get => Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down); }

        public static int CursorPositionToIndex(List<Line> TotalLines, CursorPosition CursorPosition)
        {
            int CursorIndex = 0;
            for (int i = 0; i < CursorPosition.LineNumber - 1; i++)
            {
                CursorIndex += ListHelper.GetLine(TotalLines, i).Length + 1;
            }
            return CursorIndex + CursorPosition.CharacterPosition;
        }

        public static bool Equals(CursorPosition CurPos1, CursorPosition CurPos2)
        {
            if (CurPos1 == null || CurPos2 == null)
                return false;

            if (CurPos1.LineNumber == CurPos2.LineNumber)
                return CurPos1.CharacterPosition == CurPos2.CharacterPosition;
            return false;
        }

        //Convert the coordinates from Relative to Absolute and the other way around
        public static CursorPosition RelativeToAbsolute(CursorPosition curpos, int nmbOfUnrenderdLines)
        {
            return curpos.ChangeLineNumber(curpos.LineNumber - nmbOfUnrenderdLines);
        }

        //Calculate the number of characters from the cursorposition to the next character or digit to the left and to the right
        public static int CalculateStepsToMoveLeft2(Line CurrentLine, int CursorCharPosition)
        {
            int Count = 0;
            for (int i = CursorCharPosition - 1; i >= 0; i--)
            {
                char CurrentCharacter = CurrentLine.Content[i < CurrentLine.Length ? i : CurrentLine.Length - 1];
                if (char.IsLetterOrDigit(CurrentCharacter))
                    Count++;
                else if (i == CursorCharPosition - 1 && char.IsWhiteSpace(CurrentCharacter))
                    return 0;
                else
                    break;
            }
            return Count;
        }
        public static int CalculateStepsToMoveRight2(Line CurrentLine, int CursorCharPosition)
        {
            int Count = 0;
            for (int i = CursorCharPosition; i < CurrentLine.Length; i++)
            {
                if (char.IsLetterOrDigit(CurrentLine.Content[i]))
                    Count++;
                else if (i == CursorCharPosition && char.IsWhiteSpace(CurrentLine.Content[i]))
                    return 0;
                else
                    break;
            }
            return Count;
        }

        //Calculates how many characters the cursor needs to move if control is pressed
        //Returns 1 if control is not pressed
        public static int CalculateStepsToMoveLeft(Line CurrentLine, int CursorCharPosition)
        {
            if (!ControlIsPressed)
                return 1;
            int Count = 0;
            for (int i = CursorCharPosition - 1; i >= 0; i--)
            {
                char CurrentCharacter = CurrentLine.Content[i < CurrentLine.Length ? i : CurrentLine.Length - 1];
                if (char.IsLetterOrDigit(CurrentCharacter))
                    Count++;
                else if (i == CursorCharPosition - 1 && char.IsWhiteSpace(CurrentCharacter))
                    Count++;
                else
                    break;
            }
            //If it ignores the ControlKey return the real value of Count otherwise
            //return 1 if Count is 0
            return Count == 0 ? 1 : Count;
        }
        public static int CalculateStepsToMoveRight(Line CurrentLine, int CursorCharPosition)
        {
            if (!ControlIsPressed)
                return 1;
            int Count = 0;
            for (int i = CursorCharPosition; i < CurrentLine.Length; i++)
            {
                if (char.IsLetterOrDigit(CurrentLine.Content[i]))
                    Count++;
                else if (i == CursorCharPosition && char.IsWhiteSpace(CurrentLine.Content[i]))
                    Count++;
                else
                    break;
            }
            //If it ignores the ControlKey return the real value of Count otherwise
            //return 1 if Count is 0
            return Count == 0 ? 1 : Count;
        }

        //Move cursor:
        public static CursorPosition MoveLeft(CursorPosition CurrentCursorPosition, List<Line> TotalLines, Line CurrentLine)
        {
            CursorPosition ReturnValue = new CursorPosition(CurrentCursorPosition);
            if (ReturnValue.LineNumber < 0)
            {
                return ReturnValue;
            }
            else
            {
                if (ReturnValue.CharacterPosition == 0 && ReturnValue.LineNumber > 0)
                {
                    ReturnValue.CharacterPosition = ListHelper.GetLine(TotalLines, ReturnValue.LineNumber - 1).Length;
                    ReturnValue.LineNumber -= 1;
                }
                else if (ReturnValue.CharacterPosition > 0)
                {
                    ReturnValue.CharacterPosition -= CalculateStepsToMoveLeft(CurrentLine, CurrentCursorPosition.CharacterPosition);
                }
            }

            return ReturnValue;
        }
        public static CursorPosition MoveRight(CursorPosition CurrentCursorPosition, List<Line> TotalLines, Line CurrentLine)
        {
            CursorPosition ReturnValue = new CursorPosition(CurrentCursorPosition);
            int LineLength = ListHelper.GetLine(TotalLines, ReturnValue.LineNumber).Length;

            if (ReturnValue.LineNumber > TotalLines.Count - 1)
            {
                return ReturnValue;
            }
            else
            {
                if (ReturnValue.CharacterPosition == LineLength && ReturnValue.LineNumber < TotalLines.Count - 1)
                {
                    ReturnValue.CharacterPosition = 0;
                    ReturnValue.LineNumber += 1;
                }
                else if (ReturnValue.CharacterPosition < LineLength)
                {
                    ReturnValue.CharacterPosition += CalculateStepsToMoveRight(CurrentLine, CurrentCursorPosition.CharacterPosition);
                }
            }

            if (ReturnValue.CharacterPosition > LineLength)
                ReturnValue.CharacterPosition = LineLength;
            return ReturnValue;
        }
        public static CursorPosition MoveDown(CursorPosition CurrentCursorPosition, int TotalLinesLength)
        {
            CursorPosition ReturnValue = new CursorPosition(CurrentCursorPosition);
            if (CurrentCursorPosition.LineNumber < TotalLinesLength - 1)
                ReturnValue = CursorPosition.ChangeLineNumber(CurrentCursorPosition, CurrentCursorPosition.LineNumber + 1);
            return ReturnValue;
        }
        public static CursorPosition MoveUp(CursorPosition CurrentCursorPosition)
        {
            CursorPosition ReturnValue = new CursorPosition(CurrentCursorPosition);
            if (CurrentCursorPosition.LineNumber > 0)
                ReturnValue = CursorPosition.ChangeLineNumber(ReturnValue, CurrentCursorPosition.LineNumber - 1);
            return ReturnValue;
        }

        public static CursorPosition MoveToLineEnd(CursorPosition CursorPosition, Line CurrentLine)
        {
            CursorPosition.CharacterPosition = CurrentLine.Length;
            return CursorPosition;
        }
        
        public static CursorPosition MoveToLineStart(CursorPosition CursorPosition)
        {
            CursorPosition.CharacterPosition = 0;
            return CursorPosition;
        }
    }
}