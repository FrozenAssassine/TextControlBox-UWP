using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TextControlBox_TestApp.TextControlBox.Renderer;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class Selection
    {
        //Oder the selection so StartPosition is always smaller than EndPosition
        public static TextSelection OrderTextSelection(TextSelection Selection)
        {
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
                Utils.CursorPositionsAreEqual(Selection.EndPosition, new CursorPosition(TotalLines[TotalLines.Count - 1].Length, TotalLines.Count));
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
            Debug.WriteLine("--Insert text--");

            if (Selection != null)
                return Replace(Selection, TotalLines, Text, NewLineCharacter);

            string[] lines = Text.Split(NewLineCharacter);
            Line CurrentLine = TotalLines[CursorPosition.LineNumber - 1];
            string TextInFrontOfCursor = CurrentLine.Content.Substring(0, CursorPosition.CharacterPosition);
            string TextBehindCursor =
                CurrentLine.Length > CursorPosition.CharacterPosition ?
                CurrentLine.Content.Remove(0, CursorPosition.CharacterPosition) : "";

            if (lines.Length == 1 && Text != string.Empty) //Singleline
            {
                Text = Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                TotalLines[CursorPosition.LineNumber - 1].AddText(Text, CursorPosition.CharacterPosition);
                CursorPosition.AddToCharacterPos(Text.Length);
                return CursorPosition;
            }

            //Multiline
            void RemoveLine(int Index)
            {
                if (Index < TotalLines.Count && Index >= 0)
                    TotalLines.RemoveAt(Index);
            }
            RemoveLine(CursorPosition.LineNumber - 1); //Startline

            //Paste the text
            int LastLineLength = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                Line line;
                if (i == 0)
                    line = new Line(TextInFrontOfCursor + lines[i]);
                else if (i == lines.Length - 1)
                {
                    line = new Line(lines[i] + TextBehindCursor);
                    LastLineLength = lines[i].Length;
                }
                else
                    line = new Line(lines[i]);

                TotalLines.Insert(CursorPosition.LineNumber - 1 + i, line);
            }

            return new CursorPosition(CursorPosition.CharacterPosition + LastLineLength - 1, CursorPosition.LineNumber + lines.Length - 1);
        }

        public static CursorPosition ReplaceUndo(int StartLine, List<Line> TotalLines, List<Line> Replace, int LinesToDelete)
        {
            StartLine -= 1;

            int Count = LinesToDelete;
            if (StartLine + Count >= TotalLines.Count)
                Count = TotalLines.Count - StartLine;
            if (Count < 0)
                Count = 0;

            TotalLines.RemoveRange(StartLine, Count);

            //Either add or insert to the List
            if (StartLine >= TotalLines.Count)
                TotalLines.AddRange(Replace);
            else
                TotalLines.InsertRange(StartLine, Replace);

            return new CursorPosition(Replace[Replace.Count - 1].Length - 1, StartLine);
        }

        public static CursorPosition Replace(TextSelection Selection, List<Line> TotalLines, string Text, string NewLineCharacter)
        {
            Debug.WriteLine("--Replace text--");

            Selection = OrderTextSelection(Selection);
            int StartLine = Selection.StartPosition.LineNumber;
            int EndLine = Selection.EndPosition.LineNumber;
            int StartPosition = Selection.StartPosition.CharacterPosition;
            int EndPosition = Selection.EndPosition.CharacterPosition;

            string[] SplittedText = Text.Split(NewLineCharacter);

            //Just delete the text if the string is emty
            if (Text == string.Empty)
            {
                return Remove(Selection, TotalLines, NewLineCharacter);
            }
            //Selection is singleline and Text to paste is only a singleline
            if (StartLine == EndLine && SplittedText.Length == 1)
            {
                if (StartPosition == 0 && EndPosition == TotalLines[EndLine].Length)
                    TotalLines[StartLine].Content = "";
                else
                    TotalLines[StartLine].Remove(StartPosition, EndPosition - StartPosition);

                TotalLines[StartLine].AddText(Text, StartPosition);

                return new CursorPosition(EndPosition, StartLine + 1);
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
                return new CursorPosition(TotalLines[TotalLines.Count - 1 > 0 ? TotalLines.Count - 1 : 0].Length, TotalLines.Count);
            }
            else
            {
                Line Start_Line = TotalLines[StartLine];
                if (EndLine > TotalLines.Count)
                    EndLine = TotalLines.Count - 1;
                Line End_Line = TotalLines[EndLine];

                //all lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.RemoveRange(StartLine, EndLine - StartLine + 1);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                    }
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    End_Line.Substring(EndPosition);
                    TotalLines.RemoveRange(StartLine, EndLine - StartLine);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == SplittedText.Length - 1)
                            End_Line.AddToStart(SplittedText[i]);
                        else
                            TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                    }
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    Start_Line.Remove(StartPosition);
                    TotalLines.RemoveRange(StartLine + 1, EndLine - StartLine);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == 0)
                            Start_Line.AddToEnd(SplittedText[i]);
                        else
                            TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                    }
                }
                else
                {
                    Start_Line.Remove(StartPosition);
                    End_Line.Substring(EndPosition);

                    int Remove = EndLine - StartLine - 1;
                    TotalLines.RemoveRange(StartLine + 1, Remove < 0 ? 0 : Remove);

                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == 0)
                            Start_Line.AddToEnd(SplittedText[i]);
                        if (i != 0 && i != SplittedText.Length - 1)
                            TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                        if (i == SplittedText.Length - 1)
                        {
                            Start_Line.AddToEnd(End_Line.Content);
                        }
                    }
                    TotalLines.Remove(End_Line);
                }
                return new CursorPosition(EndPosition, EndLine);
            }
        }

        public static CursorPosition Remove(TextSelection Selection, List<Line> TotalLines, string NewLineCharacter)
        {
            Debug.WriteLine("--Remove text--");
            Selection = OrderTextSelection(Selection);
            int StartLine = Selection.StartPosition.LineNumber;
            int EndLine = Selection.EndPosition.LineNumber;
            int StartPosition = Selection.StartPosition.CharacterPosition;
            int EndPosition = Selection.EndPosition.CharacterPosition;

            Line Start_Line = TotalLines[StartLine];
            Line End_Line = TotalLines[EndLine < TotalLines.Count ? EndLine : TotalLines.Count - 1];

            if (StartLine == EndLine)
            {
                if (StartPosition == 0 && EndPosition == TotalLines[EndLine].Length)
                    End_Line.Content = "";
                else
                    Start_Line.Remove(StartPosition, EndPosition - StartPosition);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                TotalLines.Clear();
                TotalLines.Add(new Line());
                return new CursorPosition(TotalLines[TotalLines.Count - 1].Length, TotalLines.Count);
            }
            else
            {
                //Whole lines are selected from start to finish
                if (StartPosition == 0 && EndPosition == End_Line.Length)
                {
                    TotalLines.RemoveRange(StartLine, EndLine - StartLine + 1);
                }
                //Only the startline is completely selected
                else if (StartPosition == 0 && EndPosition != End_Line.Length)
                {
                    End_Line.Substring(EndPosition);
                    TotalLines.RemoveRange(StartLine, EndLine - StartLine);
                }
                //Only the endline is completely selected
                else if (StartPosition != 0 && EndPosition == End_Line.Length)
                {
                    Start_Line.Remove(StartPosition);
                    TotalLines.RemoveRange(StartLine + 1, EndLine - StartLine);
                }
                //Both startline and endline are not completely selected
                else
                {
                    Start_Line.Remove(StartPosition);
                    Start_Line.Content += End_Line.Content.Substring(EndPosition);
                    TotalLines.RemoveRange(StartLine + 1, EndLine - StartLine);
                }
            }

            if (TotalLines.Count == 0)
                TotalLines.Add(new Line());

            return new CursorPosition(StartPosition, StartLine + 1);
        }

        public static TextSelectionPosition GetIndexOfSelection(List<Line> TotalLines, CursorPosition StartPos, CursorPosition EndPos)
        {
            int SelStartIndex = 0;
            int SelEndIndex = 0;
            int CharacterPosStart = StartPos.CharacterPosition;
            int CharacterPosEnd = EndPos.CharacterPosition;

            if (StartPos.LineNumber == EndPos.LineNumber)
            {
                int LenghtToLine = 0;
                for (int i = 0; i < StartPos.LineNumber; i++)
                {
                    LenghtToLine += TotalLines[i].Length + 2;
                }

                SelStartIndex = CharacterPosStart + LenghtToLine;
                SelEndIndex = CharacterPosEnd + LenghtToLine;
            }
            else
            {
                for (int i = 0; i < StartPos.LineNumber; i++)
                {
                    SelStartIndex += TotalLines[i].Length + 2;
                }
                SelStartIndex += CharacterPosStart;

                for (int i = 0; i < EndPos.LineNumber; i++)
                {
                    SelEndIndex += TotalLines[i].Length + 2;
                }
                SelEndIndex += CharacterPosEnd;
            }

            int SelectionStart = Math.Min(SelStartIndex, SelEndIndex);
            int SelectionLength = 0;
            if (SelEndIndex > SelStartIndex)
                SelectionLength = SelEndIndex - SelStartIndex;
            else
                SelectionLength = SelStartIndex - SelEndIndex;

            return new TextSelectionPosition(SelectionStart, SelectionLength);
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
        public static List<Line> GetCopyOfSelectedLines(List<Line> TotalLines, TextSelection Selection, string NewLineCharacter)
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

        public static string GetSelectedText(TextSelection TextSelection, List<Line> TotalLines, string NewLineCharacter)
        {
            //return the current line, if no text is selected:
            if (TextSelection == null)
            {
                return TotalLines[TextSelection.StartPosition.LineNumber - 1].Content + NewLineCharacter;
            }

            int StartLine = Math.Min(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndLine = Math.Max(TextSelection.StartPosition.LineNumber, TextSelection.EndPosition.LineNumber);
            int EndIndex = Math.Max(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);
            int StartIndex = Math.Min(TextSelection.StartPosition.CharacterPosition, TextSelection.EndPosition.CharacterPosition);

            StringBuilder StringBuilder = new StringBuilder();

            if (StartLine == EndLine) //Singleline
            {
                Line Line = TotalLines[StartLine < TotalLines.Count ? StartLine : TotalLines.Count - 1];
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
                for (int i = 0; i < TotalLines.Count; i++)
                {
                    StringBuilder.Append(TotalLines[i].Content + NewLineCharacter);
                }
            }
            else //Multiline
            {
                for (int i = StartLine; i < EndLine + 1; i++)
                {
                    if (i == StartLine)
                        StringBuilder.Append(TotalLines[StartLine].Content.Substring(StartIndex) + NewLineCharacter);
                    else if (i == EndLine)
                        StringBuilder.Append(EndIndex == TotalLines[EndLine].Length ? TotalLines[EndLine].Content : TotalLines[EndLine].Content.Remove(EndIndex) + NewLineCharacter);
                    else
                        StringBuilder.Append(TotalLines[i].Content + NewLineCharacter);
                }
            }
            string text = StringBuilder.ToString();
            StringBuilder.Clear();
            return text;
        }
    }
}