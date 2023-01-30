using Collections.Pooled;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TextControlBox.Extensions;
using TextControlBox.Helper;

namespace TextControlBox.Text
{
    internal class Selection
    {
        public static bool Equals(TextSelection Sel1, TextSelection Sel2)
        {
            if (Sel1 == null || Sel2 == null)
                return false;

            return Cursor.Equals(Sel1.StartPosition, Sel2.StartPosition) &&
                Cursor.Equals(Sel1.EndPosition, Sel2.EndPosition);
        }

        //Order the selection that StartPosition is always smaller than EndPosition
        public static TextSelection OrderTextSelection(TextSelection Selection)
        {
            if (Selection == null)
                return Selection;

            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int StartPosition;
            int EndPosition;
            if (StartLine == EndLine)
            {
                StartPosition = Math.Min(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                EndPosition = Math.Max(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
            }
            else
            {
                if (Selection.StartPosition.LineNumber < Selection.EndPosition.LineNumber)
                {
                    EndPosition = Selection.EndPosition.CharacterPosition;
                    StartPosition = Selection.StartPosition.CharacterPosition;
                }
                else
                {
                    EndPosition = Selection.StartPosition.CharacterPosition;
                    StartPosition = Selection.EndPosition.CharacterPosition;
                }
            }

            return new TextSelection(Selection.Index, Selection.Length, new CursorPosition(StartPosition, StartLine), new CursorPosition(EndPosition, EndLine));
        }

        public static bool WholeTextSelected(TextSelection Selection, PooledList<string> TotalLines)
        {
            if (Selection == null)
                return false;
            var sel = OrderTextSelection(Selection);
            return Utils.CursorPositionsAreEqual(sel.StartPosition, new CursorPosition(0, 0)) &&
                Utils.CursorPositionsAreEqual(sel.EndPosition, new CursorPosition(TotalLines.GetLineLength(-1), TotalLines.Count - 1));
        }
        //returns whether the selection starts at character zero and ends 
        public static bool WholeLinesAreSelected(TextSelection Selection, PooledList<string> TotalLines)
        {
            if (Selection == null)
                return false;
            var sel = OrderTextSelection(Selection);
            return Utils.CursorPositionsAreEqual(sel.StartPosition, new CursorPosition(0, sel.StartPosition.LineNumber)) &&
                Utils.CursorPositionsAreEqual(sel.EndPosition, new CursorPosition(TotalLines.GetLineText(sel.EndPosition.LineNumber).Length, sel.EndPosition.LineNumber));
        }

        public static CursorPosition GetMax(CursorPosition Pos1, CursorPosition Pos2)
        {
            if (Pos1.LineNumber == Pos2.LineNumber)
                return Pos1.CharacterPosition > Pos2.CharacterPosition ? Pos1 : Pos2;
            return Pos1.LineNumber > Pos2.LineNumber ? Pos1 : Pos2;
        }
        public static CursorPosition GetMin(CursorPosition Pos1, CursorPosition Pos2)
        {
            if (Pos1.LineNumber == Pos2.LineNumber)
                return Pos1.CharacterPosition > Pos2.CharacterPosition ? Pos2 : Pos1;
            return Pos1.LineNumber > Pos2.LineNumber ? Pos2 : Pos1;
        }
        public static CursorPosition GetMin(TextSelection Selection)
        {
            return GetMin(Selection.StartPosition, Selection.EndPosition);
        }
        public static CursorPosition GetMax(TextSelection Selection)
        {
            return GetMax(Selection.StartPosition, Selection.EndPosition);
        }

        public static CursorPosition InsertText(TextSelection Selection, CursorPosition CursorPosition, PooledList<string> TotalLines, string Text, string NewLineCharacter)
        {
            if (Selection != null)
                return Replace(Selection, TotalLines, Text, NewLineCharacter);

            string curLine = TotalLines.GetLineText(CursorPosition.LineNumber);

            string[] lines = Text.Split(NewLineCharacter);

            //Singleline
            if (lines.Length == 1 && Text != string.Empty)
            {
                Text = Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                TotalLines.SetLineText(-1, TotalLines.GetLineText(-1).AddText(Text, CursorPosition.CharacterPosition));
                CursorPosition.AddToCharacterPos(Text.Length);
                return CursorPosition;
            }

            //Multiline:
            int CurPos = CursorPosition.CharacterPosition;
            if (CurPos > curLine.Length)
                CurPos = curLine.Length;

            //GEt the text in front of the cursor
            string TextInFrontOfCursor = curLine.Substring(0, CurPos < 0 ? 0 : CurPos);
            //Get the text behind the cursor
            string TextBehindCursor = curLine.SafeRemove(0, CurPos < 0 ? 0 : CurPos);

            TotalLines.DeleteAt(CursorPosition.LineNumber);
            TotalLines.InsertOrAddRange(ListHelper.CreateLines(lines, 0, TextInFrontOfCursor, TextBehindCursor), CursorPosition.LineNumber);

            return new CursorPosition(CursorPosition.CharacterPosition + lines.Length > 0 ? lines[lines.Length - 1].Length : 0, CursorPosition.LineNumber + lines.Length - 1);
        }

        public static CursorPosition Replace(TextSelection Selection, PooledList<string> TotalLines, string Text, string NewLineCharacter)
        {
            //Just delete the text if the string is emty
            if (Text == "")
            {
                return Remove(Selection, TotalLines);
            }

            Selection = OrderTextSelection(Selection);
            int StartLine = Selection.StartPosition.LineNumber;
            int EndLine = Selection.EndPosition.LineNumber;
            int StartPosition = Selection.StartPosition.CharacterPosition;
            int EndPosition = Selection.EndPosition.CharacterPosition;

            string[] lines = Text.Split(NewLineCharacter);
            string Start_Line = TotalLines.GetLineText(StartLine);

            //Selection is singleline and text to paste is also singleline
            if (StartLine == EndLine && lines.Length == 1)
            {
                if (StartPosition == 0 && EndPosition == TotalLines.GetLineLength(EndLine))
                    Start_Line = "";
                else
                    Start_Line = Start_Line.SafeRemove(StartPosition, EndPosition - StartPosition);

                TotalLines.SetLineText(StartLine, Start_Line.AddText(Text, StartPosition));

                return new CursorPosition(StartPosition + Text.Length, Selection.StartPosition.LineNumber);
            }
            else if (StartLine == EndLine && lines.Length > 1 && (StartPosition != 0 && EndPosition != Start_Line.Length))
            {
                string TextTo = Start_Line == "" ? "" : StartPosition >= Start_Line.Length ? Start_Line : Start_Line.Safe_Substring(0, StartPosition);
                string TextFrom = Start_Line == "" ? "" : EndPosition >= Start_Line.Length ? Start_Line : Start_Line.Safe_Substring(EndPosition);

                TotalLines.SetLineText(StartLine, (TextTo + lines[0]));

                TotalLines.InsertOrAddRange(ListHelper.CreateLines(lines, 1, "", TextFrom), StartLine + 1);
                //ListHelper.Insert(TotalLines, new Line(lines[lines.Length - 1] + TextFrom), StartLine + 1);

                return new CursorPosition(EndPosition + Text.Length, StartLine + lines.Length - 1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                Debug.WriteLine("Replace whole text");
                if (lines.Length < TotalLines.Count)
                {
                    ListHelper.Clear(TotalLines);
                    TotalLines.InsertOrAddRange(lines, 0);
                }
                else
                    ReplaceLines(TotalLines, 0, TotalLines.Count, lines);

                return new CursorPosition(TotalLines.GetLineLength(-1), TotalLines.Count - 1);
            }
            else
            {
                string End_Line = TotalLines.GetLineText(EndLine);

                //All lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.Safe_RemoveRange(StartLine, EndLine - StartLine + 1);
                    TotalLines.InsertOrAddRange(lines, StartLine);
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    TotalLines.SetLineText(EndLine, End_Line.Substring(EndPosition).AddToStart(lines[lines.Length - 1]));

                    TotalLines.Safe_RemoveRange(StartLine, EndLine - StartLine);
                    TotalLines.InsertOrAddRange(lines.Take(lines.Length - 1), StartLine);
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.SetLineText(StartLine, Start_Line.SafeRemove(StartPosition).AddToEnd(lines[0]));

                    TotalLines.Safe_RemoveRange(StartLine + 1, EndLine - StartLine);
                    TotalLines.InsertOrAddRange(lines.Skip(1), StartLine + 1);
                }
                else
                {
                    //Delete the selected parts
                    Start_Line = Start_Line.SafeRemove(StartPosition);
                    End_Line = End_Line.Safe_Substring(EndPosition);

                    //Only one line to insert
                    if (lines.Length == 1)
                    {
                        TotalLines.SetLineText(StartLine, Start_Line.AddToEnd(lines[0] + End_Line));
                        TotalLines.Safe_RemoveRange(StartLine + 1, EndLine - StartLine < 0 ? 0 : EndLine - StartLine);
                    }
                    else
                    {
                        TotalLines.SetLineText(StartLine, Start_Line.AddToEnd(lines[0]));
                        TotalLines.SetLineText(EndLine, End_Line.AddToStart(lines[lines.Length - 1]));

                        TotalLines.Safe_RemoveRange(StartLine + 1, EndLine - StartLine - 1 < 0 ? 0 : EndLine - StartLine - 1);
                        if (lines.Length > 2)
                        {
                            TotalLines.InsertOrAddRange(lines.GetLines(1, lines.Length - 2), StartLine + 1);
                        }
                    }
                }
                return new CursorPosition(Start_Line.Length + End_Line.Length - 1, StartLine + lines.Length - 1);
            }
        }

        public static CursorPosition Remove(TextSelection Selection, PooledList<string> TotalLines)
        {
            Selection = OrderTextSelection(Selection);
            int StartLine = Selection.StartPosition.LineNumber;
            int EndLine = Selection.EndPosition.LineNumber;
            int StartPosition = Selection.StartPosition.CharacterPosition;
            int EndPosition = Selection.EndPosition.CharacterPosition;

            string Start_Line = TotalLines.GetLineText(StartLine);
            string End_Line = TotalLines.GetLineText(EndLine);

            if (StartLine == EndLine)
            {
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                    TotalLines.SetLineText(StartLine, "");
                else
                    TotalLines.SetLineText(StartLine, Start_Line.SafeRemove(StartPosition, EndPosition - StartPosition));
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                ListHelper.Clear(TotalLines, true);
                return new CursorPosition(0, TotalLines.Count - 1);
            }
            else
            {
                //Whole lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.Safe_RemoveRange(StartLine, EndLine - StartLine + 1);
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    TotalLines.SetLineText(EndLine, End_Line.Safe_Substring(EndPosition));
                    TotalLines.Safe_RemoveRange(StartLine, EndLine - StartLine);
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.SetLineText(StartLine, Start_Line.SafeRemove(StartPosition));
                    TotalLines.Safe_RemoveRange(StartLine + 1, EndLine - StartLine);
                }
                //Both startline and endline are not completely selected
                else
                {
                    TotalLines.SetLineText(StartLine, Start_Line.SafeRemove(StartPosition) + End_Line.Safe_Substring(EndPosition));
                    TotalLines.Safe_RemoveRange(StartLine + 1, EndLine - StartLine);
                }
            }

            if (TotalLines.Count == 0)
                TotalLines.AddLine();

            return new CursorPosition(StartPosition, StartLine);
        }

        public static TextSelectionPosition GetIndexOfSelection(PooledList<string> TotalLines, TextSelection Selection)
        {
            var Sel = OrderTextSelection(Selection);
            int StartIndex = Cursor.CursorPositionToIndex(TotalLines, Sel.StartPosition);
            int EndIndex = Cursor.CursorPositionToIndex(TotalLines, Sel.EndPosition);

            int SelectionLength;
            if (EndIndex > StartIndex)
                SelectionLength = EndIndex - StartIndex;
            else
                SelectionLength = StartIndex - EndIndex;

            return new TextSelectionPosition(Math.Min(StartIndex, EndIndex), SelectionLength);
        }

        public static TextSelection GetSelectionFromPosition(PooledList<string> TotalLines, int StartPosition, int Length, int NumberOfCharacters)
        {
            TextSelection returnValue = new TextSelection();

            if (StartPosition + Length > NumberOfCharacters)
            {
                if (StartPosition > NumberOfCharacters)
                {
                    StartPosition = NumberOfCharacters;
                    Length = 0;
                }
                else
                {
                    Length = NumberOfCharacters - StartPosition;
                }
            }

            void GetIndexInLine(int CurrentIndex, int CurrentTotalLength)
            {
                int Position = Math.Abs(CurrentTotalLength - StartPosition);

                returnValue.StartPosition =
                    new CursorPosition(Position, CurrentIndex);

                if (Length == 0)
                    returnValue.EndPosition = new CursorPosition(returnValue.StartPosition);
                else
                {
                    int LengthCount = 0;
                    for (int i = CurrentIndex; i < TotalLines.Count; i++)
                    {
                        int lineLength = TotalLines[i].Length + 1;
                        if (LengthCount + lineLength > Length)
                        {
                            returnValue.EndPosition = new CursorPosition(Math.Abs(LengthCount - Length) + Position, i);
                            break;
                        }
                        LengthCount += lineLength;
                    }
                }
            }

            //Get the Length
            int TotalLength = 0;
            for (int i = 0; i < TotalLines.Count; i++)
            {
                int lineLength = TotalLines[i].Length + 1;
                if (TotalLength + lineLength > StartPosition)
                {
                    GetIndexInLine(i, TotalLength);
                    break;
                }

                TotalLength += lineLength;
            }
            return returnValue;
        }

        public static string GetSelectedText(PooledList<string> TotalLines, TextSelection TextSelection, int CurrentLineIndex, string NewLineCharacter)
        {
            //return the current line, if no text is selected:
            if (TextSelection == null)
            {
                return TotalLines.GetLineText(CurrentLineIndex) + NewLineCharacter;
            }

            int StartLine = Math.Min(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndLine = Math.Max(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndIndex = Math.Max(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);
            int StartIndex = Math.Min(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);

            StringBuilder StringBuilder = new StringBuilder();

            if (StartLine == EndLine) //Singleline
            {
                string line = TotalLines.GetLineText(StartLine < TotalLines.Count ? StartLine : TotalLines.Count - 1);

                if (StartIndex == 0 && EndIndex != line.Length)
                    StringBuilder.Append(line.SafeRemove(EndIndex));
                else if (EndIndex == line.Length && StartIndex != 0)
                    StringBuilder.Append(line.Safe_Substring(StartIndex));
                else if (StartIndex == 0 && EndIndex == line.Length)
                    StringBuilder.Append(line);
                else StringBuilder.Append(line.SafeRemove(EndIndex).Substring(StartIndex));
            }
            else if (WholeTextSelected(TextSelection, TotalLines))
            {
                StringBuilder.Append(ListHelper.GetLinesAsString(TotalLines, NewLineCharacter));
            }
            else //Multiline
            {
                //StartLine
                StringBuilder.Append(TotalLines.GetLineText(StartLine).Substring(StartIndex) + NewLineCharacter);

                //Other lines
                if (EndLine - StartLine > 1)
                    StringBuilder.Append(TotalLines.GetLines_Large(StartLine + 1, EndLine - StartLine - 1).GetString(NewLineCharacter) + NewLineCharacter);

                //Endline
                string CurrentLine = TotalLines.GetLineText(EndLine);

                StringBuilder.Append(EndIndex >= CurrentLine.Length ? CurrentLine : CurrentLine.SafeRemove(EndIndex));
            }
            return StringBuilder.ToString();
        }

        /// <summary>
        /// Replaces the lines in TotalLines, starting by Start replacing Count number of items, with the string in SplittedText
        /// All lines that can be replaced get replaced all lines that are needed additionally get added
        /// </summary>
        /// <param name="TotalLines"></param>
        /// <param name="Start"></param>
        /// <param name="Count"></param>
        /// <param name="SplittedText"></param>
        public static void ReplaceLines(PooledList<string> TotalLines, int Start, int Count, string[] SplittedText)
        {
            if (SplittedText.Length == 0)
            {
                TotalLines.Safe_RemoveRange(Start, Count);
                return;
            }

            //Same line-length -> check for any differences in the individual lines
            if (Count == SplittedText.Length)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!TotalLines.GetLineText(i).Equals(SplittedText[i], StringComparison.Ordinal))
                    {
                        TotalLines.SetLineText(Start + i, SplittedText[i]);
                    }
                }
            }
            //Delete items from Start to Count; Insert SplittedText at Start
            else if (Count > SplittedText.Length)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (i < SplittedText.Length)
                    {
                        TotalLines.SetLineText(Start + i, SplittedText[i]);
                    }
                    else
                    {
                        TotalLines.Safe_RemoveRange(Start + i, Count - i);
                        break;
                    }
                }
            }
            //Replace all items from Start - Count with existing (add more if out of range)
            else //SplittedText.Length > Count:
            {
                for (int i = 0; i < SplittedText.Length; i++)
                {
                    //replace all possible lines
                    if (i < Count)
                    {
                        TotalLines.SetLineText(Start + i, SplittedText[i]);
                    }
                    else //Add new lines
                    {
                        TotalLines.InsertOrAddRange(SplittedText.Skip(Start + i), Start + i);
                        break;
                    }
                }
            }
        }

        public static bool MoveLinesUp(PooledList<string> TotalLines, TextSelection selection, CursorPosition cursorposition)
        {
            //Move single line
            if (selection == null)
            {
                if (cursorposition.LineNumber > 0)
                {
                    TotalLines.SwapLines(cursorposition.LineNumber, cursorposition.LineNumber - 1);
                    cursorposition.LineNumber -= 1;
                    return true;
                }
            }
            //Can not move whole text
            //else if (WholeTextSelected(selection, TotalLines))
            //{
            //    return null;
            //}
            ////Move selected lines
            //else
            //{
            //    selection = OrderTextSelection(selection);
            //    if (selection.StartPosition.LineNumber > 0)
            //    {
            //        string aboveLineText = TotalLines.GetLineText(selection.StartPosition.LineNumber - 1);
            //        TotalLines.RemoveAt(selection.StartPosition.LineNumber - 1);
            //        TotalLines.InsertOrAdd(selection.EndPosition.LineNumber, aboveLineText);

            //        selection.StartPosition.ChangeLineNumber(selection.StartPosition.LineNumber - 1);
            //        selection.EndPosition.ChangeLineNumber(selection.EndPosition.LineNumber - 1);
            //        return selection;
            //    }
            //}
            return false;
        }
        public static bool MoveLinesDown(PooledList<string> TotalLines, TextSelection selection, CursorPosition cursorposition)
        {
            //Move single line
            if (selection == null || selection.StartPosition.LineNumber == selection.EndPosition.LineNumber)
            {
                if (cursorposition.LineNumber < TotalLines.Count)
                {
                    TotalLines.SwapLines(cursorposition.LineNumber, cursorposition.LineNumber + 1);
                    cursorposition.LineNumber += 1;
                    return true;
                }
            }
            //Can not move whole text
            //else if (WholeTextSelected(selection, TotalLines))
            //{
            //    return null;
            //}
            //Move selected lines
            //else
            //{
            //    selection = OrderTextSelection(selection);
            //    if (selection.EndPosition.LineNumber + 1 < TotalLines.Count)
            //    {
            //        string aboveLineText = TotalLines.GetLineText(selection.EndPosition.LineNumber + 1);
            //        TotalLines.RemoveAt(selection.EndPosition.LineNumber + 1);
            //        TotalLines.InsertOrAdd(selection.StartPosition.LineNumber, aboveLineText);

            //        selection.StartPosition.ChangeLineNumber(selection.StartPosition.LineNumber + 1);
            //        selection.EndPosition.ChangeLineNumber(selection.EndPosition.LineNumber + 1);
            //        return selection;
            //    }
            //}
            return false;
        }
    }
}