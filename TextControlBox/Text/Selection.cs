using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TextControlBox.Helper;
using Windows.UI.Xaml.Shapes;

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

        public static bool WholeTextSelected(TextSelection Selection, PooledList<Line> TotalLines)
        {
            if (Selection == null)
                return false;
            var sel = OrderTextSelection(Selection);
            return Utils.CursorPositionsAreEqual(sel.StartPosition, new CursorPosition(0, 0)) &&
                Utils.CursorPositionsAreEqual(sel.EndPosition, new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count - 1));
        }
        //returns whether the selection starts at character zero and ends 
        public static bool WholeLinesAreSelected(TextSelection Selection, PooledList<Line> TotalLines)
        {
            if (Selection == null)
                return false;
            var sel = OrderTextSelection(Selection);
            return Utils.CursorPositionsAreEqual(sel.StartPosition, new CursorPosition(0, sel.StartPosition.LineNumber)) &&
                Utils.CursorPositionsAreEqual(sel.EndPosition, new CursorPosition(ListHelper.GetLine(TotalLines, sel.EndPosition.LineNumber).Length, sel.EndPosition.LineNumber));
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

        public static CursorPosition InsertText(TextSelection Selection, CursorPosition CursorPosition, PooledList<Line> TotalLines, string Text, string NewLineCharacter)
        {
            if (Selection != null)
                return Replace(Selection, TotalLines, Text, NewLineCharacter);

            string[] lines = Text.Split(NewLineCharacter);
            Line CurrentLine = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber);

            //Singleline
            if (lines.Length == 1 && Text != string.Empty)
            {
                Text = Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                ListHelper.GetLine(TotalLines, -1).AddText(Text, CursorPosition.CharacterPosition);
                CursorPosition.AddToCharacterPos(Text.Length);
                return CursorPosition;
            }

            //Multiline:
            int CurPos = CursorPosition.CharacterPosition;
            if (CurPos > CurrentLine.Length)
                CurPos = CurrentLine.Length;
            
            //GEt the text in front of the cursor
            string TextInFrontOfCursor = CurrentLine.Content.Substring(0, CurPos < 0 ? 0 : CurPos);
            //Get the text behind the cursor
            string TextBehindCursor = CurrentLine.Content.Remove(0, CurPos < 0 ? 0 : CurPos);

            ListHelper.DeleteAt(TotalLines, CursorPosition.LineNumber);
            ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(lines, 0, TextInFrontOfCursor, TextBehindCursor), CursorPosition.LineNumber);

            return new CursorPosition(CursorPosition.CharacterPosition + lines.Length > 0 ? lines[lines.Length - 1].Length : 0, CursorPosition.LineNumber + lines.Length - 1);
        }

        public static CursorPosition Replace(TextSelection Selection, PooledList<Line> TotalLines, string Text, string NewLineCharacter)
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
            Line Start_Line = ListHelper.GetLine(TotalLines, StartLine);

            //Selection is singleline and text to paste is also singleline
            if (StartLine == EndLine && lines.Length == 1)
            {
                if (StartPosition == 0 && EndPosition == ListHelper.GetLine(TotalLines, EndLine).Length)
                    Start_Line.Content = "";
                else
                    Start_Line.Remove(StartPosition, EndPosition - StartPosition);

                Start_Line.AddText(Text, StartPosition);
                return new CursorPosition(StartPosition + Text.Length, Selection.StartPosition.LineNumber);
            }
            else if (StartLine == EndLine && lines.Length > 1 && (StartPosition != 0 && EndPosition != Start_Line.Length))
            {
                string TextTo = Start_Line.Content == "" ? "" : StartPosition >= Start_Line.Length ? Start_Line.Content : Start_Line.Content.Substring(0, StartPosition);
                string TextFrom = Start_Line.Content == "" ? "" : EndPosition >= Start_Line.Length ? Start_Line.Content : Start_Line.Content.Substring(EndPosition);

                Start_Line.SetText(TextTo + lines[0]);

                var linesToAdd = ListHelper.CreateLines(lines, 1, "", TextFrom);
                ListHelper.InsertRange(TotalLines, linesToAdd, StartLine + 1);
                //ListHelper.Insert(TotalLines, new Line(lines[lines.Length - 1] + TextFrom), StartLine + 1);

                return new CursorPosition(EndPosition + Text.Length, StartLine + lines.Length - 1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                ReplaceLines(TotalLines, 0, TotalLines.Count, lines);
                return new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count - 1);
            }
            else
            {
                Line End_Line = ListHelper.GetLine(TotalLines, EndLine);

                //All lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    //ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine + 1);

                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine + 1);
                    ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(lines), StartLine);
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    End_Line.Substring(EndPosition);
                    End_Line.AddToStart(lines[lines.Length - 1]);

                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine);
                    ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(lines, 0, lines.Length - 1), StartLine);
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    Start_Line.Remove(StartPosition);
                    Start_Line.AddToEnd(lines[0]);

                    ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine);
                    ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(lines, 1), StartLine + 1);
                }
                else
                {                    
                    //Delete the selected parts
                    Start_Line.Remove(StartPosition);
                    End_Line.Substring(EndPosition);

                    //Only one line to insert
                    if (lines.Length == 1)
                    {
                        Start_Line.AddToEnd(lines[0] + End_Line.Content);

                        ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine < 0 ? 0 : EndLine - StartLine);
                    }
                    else
                    {
                        Start_Line.AddToEnd(lines[0]);
                        ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine - 1 < 0 ? 0 : EndLine - StartLine - 1);
                        ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(lines, 1, lines.Length - 1), StartLine + 1);
                        End_Line.AddToStart(lines[lines.Length - 1]);

                    }
                }
                return new CursorPosition(Start_Line.Length + End_Line.Length - 1, StartLine + lines.Length - 1);
            }
        }

        public static CursorPosition Remove(TextSelection Selection, PooledList<Line> TotalLines)
        {
            Selection = OrderTextSelection(Selection);
            int StartLine = Selection.StartPosition.LineNumber;
            int EndLine = Selection.EndPosition.LineNumber;
            int StartPosition = Selection.StartPosition.CharacterPosition;
            int EndPosition = Selection.EndPosition.CharacterPosition;

            Line Start_Line = ListHelper.GetLine(TotalLines, StartLine);
            Line End_Line = ListHelper.GetLine(TotalLines, EndLine);

            if (StartLine == EndLine)
            {
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                    End_Line.Content = "";
                else
                    Start_Line.Remove(StartPosition, EndPosition - StartPosition);
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
                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine + 1);
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    End_Line.Substring(EndPosition);
                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine);
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    Start_Line.Remove(StartPosition);
                    ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine);
                }
                //Both startline and endline are not completely selected
                else
                {
                    Start_Line.Remove(StartPosition);
                    Start_Line.Content += End_Line.Content.Substring(EndPosition);
                    ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine);
                }
            }

            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());

            return new CursorPosition(StartPosition, StartLine);
        }

        public static TextSelectionPosition GetIndexOfSelection(PooledList<Line> TotalLines, TextSelection Selection)
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

        public static TextSelection GetSelectionFromPosition(PooledList<Line> TotalLines, int StartPosition, int Length, int NumberOfCharacters)
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

        //Returns the whole lines, without respecting the characterposition of the selection
        public static PooledList<Line> GetSelectedLines(PooledList<Line> TotalLines, TextSelection Selection)
        {
            if (Selection == null)
                return null;

            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);

            if (EndLine == StartLine)
            {
                return ListHelper.GetLines(TotalLines, StartLine, 1);
            }
            else
            {
                int Count = EndLine - StartLine + (EndLine - StartLine + 1 < TotalLines.Count ? 1 : 0);
                return ListHelper.GetLines(TotalLines, StartLine, Count);
            }
        }

        public static string GetSelectedTextWithoutCharacterPos(PooledList<Line> TotalLines, TextSelection TextSelection, string NewLineCharacter)
        {
            if (TextSelection == null)
                return null;

            int StartLine = Math.Min(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndLine = Math.Max(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);

            //Get the items into the list CurrentItems
            if (EndLine == StartLine)
            {
                return ListHelper.GetLine(TotalLines, StartLine).Content;
            }
            else
            {
                return ListHelper.GetLinesAsString(TotalLines, StartLine, EndLine - StartLine + 1, NewLineCharacter);
            }
        }

        public static string GetSelectedText(PooledList<Line> TotalLines, TextSelection TextSelection, int CurrentLineIndex, string NewLineCharacter)
        {
            //return the current line, if no text is selected:
            if (TextSelection == null)
            {
                return ListHelper.GetLine(TotalLines, CurrentLineIndex).Content + NewLineCharacter;
            }

            int StartLine = Math.Min(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndLine = Math.Max(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndIndex = Math.Max(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);
            int StartIndex = Math.Min(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);

            StringBuilder StringBuilder = new StringBuilder();

            if (StartLine == EndLine) //Singleline
            {
                Line Line = ListHelper.GetLine(TotalLines, StartLine < TotalLines.Count ? StartLine : TotalLines.Count - 1);
                if (StartIndex == 0 && EndIndex != Line.Length)
                    StringBuilder.Append(Line.Content.Remove(EndIndex));
                else if (EndIndex == Line.Length && StartIndex != 0)
                    StringBuilder.Append(Line.Content.Substring(StartIndex));
                else if (StartIndex == 0 && EndIndex == Line.Length)
                    StringBuilder.Append(Line.Content);
                else StringBuilder.Append(Line.Content.Remove(EndIndex).Substring(StartIndex));
            }
            else if (WholeTextSelected(TextSelection, TotalLines))
            {
                StringBuilder.Append(ListHelper.GetLinesAsString(TotalLines, NewLineCharacter));
            }
            else //Multiline
            {
                //StartLine
                StringBuilder.Append(ListHelper.GetLine(TotalLines, StartLine).Content.Substring(StartIndex) + NewLineCharacter);

                //Other lines
                if (EndLine - StartLine > 1)
                    StringBuilder.Append(ListHelper.GetLinesAsString(TotalLines, StartLine + 1, EndLine - StartLine - 1, NewLineCharacter) + NewLineCharacter);

                //Endline
                Line CurrentLine = ListHelper.GetLine(TotalLines, EndLine);
                StringBuilder.Append(EndIndex >= CurrentLine.Length ? CurrentLine.Content : CurrentLine.Content.Remove(EndIndex));
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
        public static void ReplaceLines(PooledList<Line> TotalLines, int Start, int Count, string[] SplittedText)
        {
            if (SplittedText.Length == 0)
            {
                ListHelper.RemoveRange(TotalLines, Start, Count);
                return;
            }

            PooledList<Line> linesToReplace = ListHelper.GetLines(TotalLines, Start, Count);

            //Same line-length -> check for any differences in the individual lines
            if (linesToReplace.Count == SplittedText.Length)
            {
                for (int i = 0; i < linesToReplace.Count; i++)
                {
                    if (!linesToReplace[i].Content.Equals(SplittedText[i], StringComparison.Ordinal))
                    {
                        linesToReplace[i].SetText(SplittedText[i]);
                    }
                }
            }
            //Delete items from Start to Count; Insert SplittedText at Start
            else if (linesToReplace.Count > SplittedText.Length)
            {
                for (int i = 0; i < linesToReplace.Count; i++)
                {
                    if (i < SplittedText.Length)
                    {
                        linesToReplace[i].SetText(SplittedText[i]);
                    }
                    else
                    {
                        ListHelper.RemoveRange(TotalLines, Start + i, linesToReplace.Count - i);
                        break;
                    }
                }
            }
            //Replace all items from Start - Count with existing (add more if out of range)
            else
            {
                for (int i = 0; i < SplittedText.Length; i++)
                {
                    //replace all possible lines
                    if (i < linesToReplace.Count)
                    {
                        linesToReplace[i].SetText(SplittedText[i]);
                    }
                    else //Add new lines
                    {
                        ListHelper.InsertRange(TotalLines, ListHelper.CreateLines(SplittedText, i), Start + i);
                        break;
                    }
                }
            }
        }
    }
}