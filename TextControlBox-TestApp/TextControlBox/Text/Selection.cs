using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class Selection
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

        public static bool WholeTextSelected(TextSelection Selection, List<Line> TotalLines)
        {
            if (Selection == null)
                return false;

            return Utils.CursorPositionsAreEqual(Selection.StartPosition, new CursorPosition(0, 0)) &&
                Utils.CursorPositionsAreEqual(Selection.EndPosition, new CursorPosition(ListHelper.GetLine(TotalLines, -1).Length, TotalLines.Count));
        }

        public static CursorPosition GetMax(CursorPosition Pos1, CursorPosition Pos2)
        {
            if (Pos1.LineNumber == Pos2.LineNumber)
                return Pos1.CharacterPosition > Pos2.CharacterPosition ? Pos2 : Pos1;
            return Pos1.LineNumber > Pos2.LineNumber ? Pos1 : Pos2;
        }
        public static CursorPosition GetMin(CursorPosition Pos1, CursorPosition Pos2)
        {
            if (Pos1.LineNumber == Pos2.LineNumber)
                return Pos1.CharacterPosition > Pos2.CharacterPosition ? Pos1 : Pos2;
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

        public static CursorPosition InsertText(TextSelection Selection, CursorPosition CursorPosition, List<Line> TotalLines, string Text, string NewLineCharacter)
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

            //Multiline
            string TextInFrontOfCursor = "";
            try
            {
                TextInFrontOfCursor = CurrentLine.Content.Substring(0, CursorPosition.CharacterPosition < 0 ? 0 : CursorPosition.CharacterPosition);
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.WriteLine("*ArgumentOutOfRangeException* in InsertText");
            }
            string TextBehindCursor =  "";
            try
            {
                TextBehindCursor = CurrentLine.Length > CursorPosition.CharacterPosition ?
                CurrentLine.Content.Remove(0, CursorPosition.CharacterPosition) : "";
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.WriteLine("*ArgumentOutOfRangeException* in InsertText");
            }


            ListHelper.DeleteAt(TotalLines, CursorPosition.LineNumber);

            List<Line> LinesToInsert = new List<Line>(lines.Length);
            //Paste the text
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                    LinesToInsert.Add(new Line(TextInFrontOfCursor + lines[i]));
                else if (i == lines.Length - 1)
                    LinesToInsert.Add(new Line(lines[i] + TextBehindCursor));
                else
                    LinesToInsert.Add(new Line(lines[i]));
            }
            ListHelper.InsertRange(TotalLines, LinesToInsert, CursorPosition.LineNumber);
            LinesToInsert.Clear();
            return new CursorPosition(CursorPosition.CharacterPosition + lines.Length > 0 ? lines[lines.Length - 1].Length : 0, CursorPosition.LineNumber + lines.Length - 1);
        }

        public static CursorPosition Replace(TextSelection Selection, List<Line> TotalLines, string Text, string NewLineCharacter)
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

            string[] SplittedText = Text.Split(NewLineCharacter);
            Line Start_Line = ListHelper.GetLine(TotalLines, StartLine);

            //Selection is singleline and text to paste is also singleline
            if (StartLine == EndLine && SplittedText.Length == 1)
            {
                if (StartPosition == 0 && EndPosition == ListHelper.GetLine(TotalLines, EndLine).Length)
                    Start_Line.Content = "";
                else
                    Start_Line.Remove(StartPosition, EndPosition - StartPosition);

                Start_Line.AddText(Text, StartPosition);
                return new CursorPosition(StartPosition + Text.Length, Selection.StartPosition.LineNumber);
            }
            else if (StartLine == EndLine && SplittedText.Length > 1)
            {
                if (StartPosition == 0 && EndPosition == ListHelper.GetLine(TotalLines, EndLine).Length)
                    Start_Line.Content = "";

                string TextTo = Start_Line.Content == "" ? "" : Start_Line.Content.Substring(0, StartPosition);
                string TextFrom = Start_Line.Content == "" ? "" : Start_Line.Content.Substring(EndPosition);

                List<Line> Lines = new List<Line>(SplittedText.Length);
                for (int i = 0; i < SplittedText.Length; i++)
                {
                    if (i == 0)
                        Start_Line.SetText(TextTo + SplittedText[i]);
                    else if (i == SplittedText.Length - 1)
                        Lines.Add(new Line(SplittedText[i] + TextFrom));
                    else
                        Lines.Add(new Line(SplittedText[i]));
                }
                ListHelper.InsertRange(TotalLines, Lines, StartLine + 1);
                Lines.Clear();

                return new CursorPosition(EndPosition + Text.Length, StartLine + SplittedText.Length - 1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                TotalLines.Clear();
                List<Line> LinesToAdd = new List<Line>();
                for (int i = 0; i < SplittedText.Length; i++)
                    LinesToAdd.Add(new Line(SplittedText[i]));

                TotalLines.AddRange(LinesToAdd);
                LinesToAdd.Clear();

                if (TotalLines.Count == 0)
                    TotalLines.Add(new Line());

                return new CursorPosition(ListHelper.GetLine(TotalLines, - 1).Length, TotalLines.Count - 1);
            }
            else
            {
                List<Line> LinesToInsert = new List<Line>(SplittedText.Length);
                Line End_Line = ListHelper.GetLine(TotalLines, EndLine);
                int InsertPosition = StartLine;
                //all lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine + 1);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        LinesToInsert.Add(new Line(SplittedText[i]));
                    }
                    StartLine -= 1;
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    End_Line.Substring(EndPosition);
                    ListHelper.RemoveRange(TotalLines, StartLine, EndLine - StartLine);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == SplittedText.Length - 1)
                            End_Line.AddToStart(SplittedText[i]);
                        else
                            LinesToInsert.Add(new Line(SplittedText[i]));
                    }
                    StartLine -= 1;
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    Start_Line.Remove(StartPosition);
                    ListHelper.RemoveRange(TotalLines, StartLine + 1, EndLine - StartLine);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == 0)
                            Start_Line.AddToEnd(SplittedText[i]);
                        else
                            LinesToInsert.Add(new Line(SplittedText[i]));
                    }
                }
                else
                {
                    Start_Line.Remove(StartPosition);
                    End_Line.Substring(EndPosition);

                    int Remove = EndLine - StartLine - 1;
                    ListHelper.RemoveRange(TotalLines, StartLine + 1, Remove < 0 ? 0 : Remove);

                    if (SplittedText.Length == 1)
                    {
                        Start_Line.AddToEnd(SplittedText[0] + End_Line.Content);
                        TotalLines.Remove(End_Line);
                    }
                    else
                    {
                        for (int i = 0; i < SplittedText.Length; i++)
                        {
                            if (i == 0)
                                Start_Line.AddToEnd(SplittedText[i]);
                            if (i != 0 && i != SplittedText.Length - 1)
                                LinesToInsert.Add(new Line(SplittedText[i]));
                            if (i == SplittedText.Length - 1)
                            {
                                End_Line.AddToStart(SplittedText[i]);
                            }
                        }
                    }
                }
                if (LinesToInsert.Count > 0)
                {
                    ListHelper.InsertRange(TotalLines, LinesToInsert, StartLine + 1);
                    LinesToInsert.Clear();
                }
                                
                return new CursorPosition(Start_Line.Length + End_Line.Length - 1, InsertPosition + SplittedText.Length - 1);
            }
        }

        public static CursorPosition Remove(TextSelection Selection, List<Line> TotalLines)
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
                TotalLines.Clear();
                TotalLines.Add(new Line());
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

        public static TextSelectionPosition GetIndexOfSelection(List<Line> TotalLines, TextSelection Selection)
        {
            var Sel = OrderTextSelection(Selection);
            int SelStartIndex = Sel.StartPosition.CharacterPosition;
            int SelEndIndex = Sel.EndPosition.CharacterPosition;
            int StartLine = Sel.StartPosition.LineNumber;
            int EndLine = Sel.EndPosition.LineNumber;

            if (StartLine == EndLine)
            {
                for (int i = 0; i < StartLine; i++)
                {
                    int val = ListHelper.GetLine(TotalLines, i).Length + 1;
                    SelEndIndex += val;
                    SelStartIndex += val;
                }
            }
            else
            {
                for (int i = 0; i < StartLine; i++)
                {
                    SelStartIndex += ListHelper.GetLine(TotalLines, i).Length + 1;
                }

                for (int i = StartLine; i < EndLine; i++)
                {
                    SelEndIndex += ListHelper.GetLine(TotalLines, i).Length + 1;
                }
            }

            int SelectionLength;
            if (SelEndIndex > SelStartIndex)
                SelectionLength = SelEndIndex - SelStartIndex;
            else
                SelectionLength = SelStartIndex - SelEndIndex;

            return new TextSelectionPosition(Math.Min(SelStartIndex, SelEndIndex), SelectionLength);
        }

        //Returns the whole lines, without respecting the characterposition of the selection
        public static List<Line> GetPointerToSelectedLines(List<Line> TotalLines, TextSelection Selection)
        {
            if (Selection == null)
                return null;

            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);

            if (EndLine == StartLine)
            {
                return TotalLines.GetRange(StartLine, 1);
            }
            else
            {
                int Count = EndLine - StartLine + (EndLine - StartLine + 1 < TotalLines.Count ? 1 : 0);
                return TotalLines.GetRange(StartLine, Count);
            }
        }
        //Get the selected lines as a new Line without respecting the characterposition
        public static List<Line> GetCopyOfSelectedLines(List<Line> TotalLines, TextSelection Selection)
        {
            if (Selection == null)
                return null;

            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);

            //Get the items into the list CurrentItems
            List<Line> CurrentItems;
            if (EndLine == StartLine)
            {
                CurrentItems = TotalLines.GetRange(StartLine, 1);
            }
            else
            {
                int Count = EndLine - StartLine + 1;
                if (StartLine + Count >= TotalLines.Count)
                    Count = TotalLines.Count - StartLine;

                CurrentItems = TotalLines.GetRange(StartLine, Count);
            }

            //Create new items with same content from CurrentItems into NewItems
            List<Line> NewItems = new List<Line>();
            for (int i = 0; i < CurrentItems.Count; i++)
            {
                NewItems.Add(new Line(CurrentItems[i].Content));
            }
            CurrentItems.Clear();
            return NewItems;
        }

        public static string GetSelectedTextWithoutCharacterPos(List<Line> TotalLines, TextSelection TextSelection, string NewLineCharacter)
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
                int Count = EndLine - StartLine + 1;
                if (StartLine + Count >= TotalLines.Count)
                    Count = TotalLines.Count - StartLine;

                return string.Join(NewLineCharacter, ListHelper.GetLines(TotalLines, StartLine, Count).Select(x => x.Content));
            }
        }

        public static string GetSelectedText(List<Line> TotalLines, TextSelection TextSelection, int CurrentLineIndex, string NewLineCharacter)
        {
            //return the current line, if no text is selected:
            if (TextSelection == null)
            {
                return ListHelper.GetLine(TotalLines, CurrentLineIndex).Content;
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
                return String.Join(NewLineCharacter, TotalLines.Select(x => x.Content));
            }
            else //Multiline
            {
                for (int i = StartLine; i < EndLine + 1; i++)
                {
                    if (i == StartLine)
                        StringBuilder.Append(ListHelper.GetLine(TotalLines, StartLine).Content.Substring(StartIndex) + NewLineCharacter);
                    else if (i == EndLine)
                    {
                        Line CurrentLine = ListHelper.GetLine(TotalLines, EndLine);
                        StringBuilder.Append(EndIndex == CurrentLine.Length ? CurrentLine.Content : CurrentLine.Content.Remove(EndIndex));
                    }
                    else
                        StringBuilder.Append(ListHelper.GetLine(TotalLines, i).Content + NewLineCharacter);
                }
            }
            string text = StringBuilder.ToString();
            StringBuilder.Clear();
            return text;
        }
    }
}