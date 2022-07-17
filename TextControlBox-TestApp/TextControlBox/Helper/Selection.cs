using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TextControlBox_TestApp.TextControlBox.Renderer;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using static System.Net.Mime.MediaTypeNames;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class Selection
    {
        public static bool WholeTextSelected(TextSelection Selection, List<Line> TotalLines)
        {
            return Utils.CursorPositionsAreEqual(Selection.StartPosition, new CursorPosition(0, 0)) &&
                Utils.CursorPositionsAreEqual(Selection.EndPosition, new CursorPosition(TotalLines[TotalLines.Count - 1].Content.Length, TotalLines.Count));
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
        public static CursorPosition InsertText(TextSelection Selection, List<Line> TotalLines, string Text, string NewLineCharacter)
        {
            Debug.WriteLine("--Insert text--");

            if (Selection != null)
                return Replace(Selection, TotalLines, Text, NewLineCharacter);

            CursorPosition CursorPosition = Selection.StartPosition;

            string[] lines = Text.Split(NewLineCharacter);
            Line CurrentLine = TotalLines[CursorPosition.LineNumber - 1];
            string TextInFrontOfCursor = CurrentLine.Content.Substring(0, CursorPosition.CharacterPosition);
            string TextBehindCursor =
                CurrentLine.Content.Length > CursorPosition.CharacterPosition ?
                CurrentLine.Content.Remove(0, CursorPosition.CharacterPosition) : "";

            //UndoRedo.RecordMultiLineUndo(GetLineFromIndex(InserStartPoint), GetLineFromIndex(InserStartPoint + lines.Length), CursorPosition);

            if (lines.Length == 1 && Text != string.Empty) //Singleline
            {
                Text = Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                TotalLines[CursorPosition.LineNumber - 1].AddText(Text, CursorPosition.CharacterPosition);
                return CursorPosition.ChangeCharacterPosition(CursorPosition, CursorPosition.CharacterPosition + Text.Length);
            }

            //Multiline
            void RemoveLine(int Index)
            {
                if (Index < TotalLines.Count && Index > 0)
                    TotalLines.RemoveAt(Index);
            }
            RemoveLine(CursorPosition.LineNumber - 1); //Startline
            RemoveLine(CursorPosition.LineNumber - 1 + lines.Length - 1);//Endline

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
            return new CursorPosition(CursorPosition.CharacterPosition + LastLineLength, CursorPosition.LineNumber + lines.Length - 1);
        }
        public static CursorPosition Replace2(TextSelection Selection, List<Line> TotalLines, string Text, string NewLineCharacter)
        {
            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndIndex;
            int StartIndex;

            //Just delete the text if the string is emty
            if (Text == string.Empty)
            {
                return Remove(Selection, TotalLines, NewLineCharacter);
            }

            if (StartLine == EndLine) //Singleline
            {
                EndIndex = Math.Max(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                StartIndex = Math.Min(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                TotalLines[StartLine].ReplaceText(StartIndex, EndIndex, Text);
                return new CursorPosition(StartIndex, StartLine + 1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                Debug.WriteLine("Whole text is selected");
                TotalLines.Clear();

                var SplittedText = Text.Split(NewLineCharacter);
                for (int i = 0; i < SplittedText.Length; i++)
                {
                    TotalLines.Add(new Line(SplittedText[i]));
                }

                //Set the Cursor to the end of the text
                return new CursorPosition(TotalLines[TotalLines.Count - 1].Content.Length, TotalLines.Count);
            }
            else
            {
                if (StartLine == Selection.StartPosition.LineNumber)
                {
                    StartIndex = Selection.StartPosition.CharacterPosition;
                    EndIndex = Selection.EndPosition.CharacterPosition;
                }
                else
                {
                    StartIndex = Selection.EndPosition.CharacterPosition;
                    EndIndex = Selection.StartPosition.CharacterPosition;
                }

                TotalLines[EndLine].Substring(EndIndex);
                TotalLines[StartLine].Remove(StartIndex);

                var SplittedText = Text.Split(NewLineCharacter);
                Debug.WriteLine(StartLine + "::" + EndLine);
                if (EndLine - StartLine > 2)
                {
                    TotalLines.RemoveRange(StartLine, EndLine - StartLine - 1);
                }

                for (int i = 0; i < SplittedText.Length; i++)
                {
                    if (i == 0)
                        TotalLines[StartLine].AddToEnd(SplittedText[i]);
                    else if (i == SplittedText.Length - 1)
                        TotalLines[EndLine].AddToEnd(SplittedText[i]);
                    else
                        TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                }
                return new CursorPosition(StartIndex, StartLine + 1);
            }
        }

        public static CursorPosition Replace(TextSelection Selection, List<Line> TotalLines, string Text, string NewLineCharacter)
        {
            Debug.WriteLine("--Replace text--");

            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int StartIndex;
            int EndIndex;

            //Just delete the text if the string is emty
            if (Text == string.Empty)
            {
                return Remove(Selection, TotalLines, NewLineCharacter);
            }

            if (StartLine == EndLine) //Selection is singleline
            {
                StartIndex = Math.Min(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                EndIndex = Math.Max(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);

                if (StartIndex == 0 && EndIndex == TotalLines[EndLine].Content.Length)
                    TotalLines[EndLine].Content = "";
                else
                    TotalLines[StartLine].Remove(StartIndex, EndIndex - StartIndex);
                return new CursorPosition(StartIndex, StartLine + 1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                TotalLines.Clear();
                TotalLines.Add(new Line());
                return new CursorPosition(TotalLines[TotalLines.Count - 1].Content.Length, TotalLines.Count);
            }
            else
            {
                if (StartLine < EndLine)
                {
                    EndIndex = Selection.StartPosition.CharacterPosition;
                    StartIndex = Selection.EndPosition.CharacterPosition;
                }
                else
                {
                    EndIndex = Selection.EndPosition.CharacterPosition;
                    StartIndex = Selection.StartPosition.CharacterPosition;
                }

                string StartLineContent = TotalLines[StartLine].Remove(StartIndex);
                string EndLineContent = TotalLines[EndLine].Substring(EndIndex);

                TotalLines.RemoveRange(StartLine, EndLine - StartLine + (EndLine - StartIndex > 2 ? 1 : 0));

                var SplittedText = Text.Split(NewLineCharacter);
                if (SplittedText.Length == 1)
                {
                    Debug.WriteLine(SplittedText.Length);
                    Debug.WriteLine(SplittedText[0]);
                    Debug.WriteLine(TotalLines.Count + "::" + StartLine);
                    TotalLines[StartLine].Content = StartLineContent + SplittedText[0] + EndLineContent;
                }
                else
                {
                    for (int i = 0; i < SplittedText.Length; i++)
                    {
                        if (i == 0)
                            TotalLines[StartLine].AddToEnd(SplittedText[i]);
                        else if (i == SplittedText.Length - 1)
                            TotalLines[EndLine].AddToEnd(SplittedText[i]);
                        else
                            TotalLines.Insert(StartLine + i, new Line(SplittedText[i]));
                    }
                }
                //Add a new line if no line exists
                if (TotalLines.Count == 0)
                    TotalLines.Add(new Line());

                return new CursorPosition(StartIndex, StartLine - 1);
            }
        }

        public static CursorPosition Remove(TextSelection Selection, List<Line> TotalLines, string NewLineCharacter)
        {
            Debug.WriteLine("--Remove text--");
            int StartLine = Math.Min(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int EndLine = Math.Max(Selection.StartPosition.LineNumber, Selection.EndPosition.LineNumber);
            int StartIndex;
            int EndIndex;

            if (StartLine == EndLine) //Selection is singleline
            {
                Debug.WriteLine("--> 1");
                StartIndex = Math.Min(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                EndIndex = Math.Max(Selection.StartPosition.CharacterPosition, Selection.EndPosition.CharacterPosition);
                if (StartIndex == 0 && EndIndex == TotalLines[EndLine].Content.Length)
                    TotalLines[EndLine].Content = "";
                else
                    TotalLines[StartLine].Remove(StartIndex, EndIndex - StartIndex);
                return new CursorPosition(StartIndex, StartLine+1);
            }
            else if (WholeTextSelected(Selection, TotalLines))
            {
                Debug.WriteLine("--> 2");
                TotalLines.Clear();
                TotalLines.Add(new Line());
                return new CursorPosition(TotalLines[TotalLines.Count - 1].Content.Length, TotalLines.Count);
            }
            else
            {
                Debug.WriteLine("--> 3");
                if (StartLine < EndLine)
                {
                    EndIndex = Selection.StartPosition.CharacterPosition;
                    StartIndex = Selection.EndPosition.CharacterPosition;
                }
                else
                {
                    EndIndex = Selection.EndPosition.CharacterPosition;
                    StartIndex = Selection.StartPosition.CharacterPosition;
                }

                string StartLineContent = TotalLines[StartLine].Remove(StartIndex);
                string EndLineContent = TotalLines[EndLine].Substring(EndIndex);

                TotalLines[StartLine].Content = StartLineContent + EndLineContent;
                Debug.WriteLine((StartLine) + "::" + (EndLine - StartLine));

                TotalLines.RemoveRange(StartLine + 1, EndLine - StartLine);
                
                //Add a new line if no line exists
                if (TotalLines.Count == 0)
                    TotalLines.Add(new Line());

                return new CursorPosition(StartIndex, StartLine - 1);
            }
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
                    LenghtToLine += TotalLines[i].Content.Length + 2;
                }

                SelStartIndex = CharacterPosStart + LenghtToLine;
                SelEndIndex = CharacterPosEnd + LenghtToLine;
            }
            else
            {
                for (int i = 0; i < StartPos.LineNumber; i++)
                {
                    SelStartIndex += TotalLines[i].Content.Length + 2;
                }
                SelStartIndex += CharacterPosStart;

                for (int i = 0; i < EndPos.LineNumber; i++)
                {
                    SelEndIndex += TotalLines[i].Content.Length + 2;
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
                StringBuilder.Append(TotalLines[StartLine].Content.Remove(EndIndex).Substring(StartIndex));
            }
            else if(WholeTextSelected(TextSelection, TotalLines))
            {
                for(int i = 0; i<TotalLines.Count; i++)
                {
                    StringBuilder.Append(TotalLines[i].Content + NewLineCharacter);
                }
            }
            else //Multiline
            {
                for (int i = StartLine; i < EndLine+1; i++)
                {
                    if (i == StartLine)
                        StringBuilder.Append(TotalLines[StartLine].Content.Substring(StartIndex) + NewLineCharacter);
                    else if (i == EndLine)
                        StringBuilder.Append(EndIndex == TotalLines[EndLine].Content.Length ? TotalLines[EndLine].Content : TotalLines[EndLine].Content.Remove(EndIndex) + NewLineCharacter);
                    else
                        StringBuilder.Append(TotalLines[i].Content + NewLineCharacter);
                    Debug.WriteLine(i + "::" + EndLine);
                }
            }
            string text = StringBuilder.ToString();
            StringBuilder.Clear();
            return text;
        }
    }
}