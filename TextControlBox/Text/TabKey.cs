using Collections.Pooled;
using TextControlBox.Extensions;

namespace TextControlBox.Text
{
    internal class TabKey
    {
        public static TextSelection MoveTabBack(PooledList<string> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter, UndoRedo UndoRedo)
        {
            if (TextSelection == null)
            {
                string line = TotalLines.GetLineText(CursorPosition.LineNumber);
                if (line.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                    CursorPosition.SubtractFromCharacterPos(TabCharacter.Length);

                UndoRedo.RecordUndoAction(() =>
                {
                    TotalLines.SetLineText(CursorPosition.LineNumber, line.RemoveFirstOccurence(TabCharacter));
                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);

                return new TextSelection(CursorPosition, null);
            }
            else
            {
                TextSelection = Selection.OrderTextSelection(TextSelection);
                int SelectedLinesCount = TextSelection.EndPosition.LineNumber - TextSelection.StartPosition.LineNumber;

                TextSelection tempSel = new TextSelection(TextSelection);
                tempSel.StartPosition.CharacterPosition = 0;
                tempSel.EndPosition.CharacterPosition = TotalLines.GetLineText(TextSelection.EndPosition.LineNumber).Length + TabCharacter.Length;

                UndoRedo.RecordUndoAction(() =>
                {
                    for (int i = 0; i < SelectedLinesCount + 1; i++)
                    {
                        int lineIndex = i + TextSelection.StartPosition.LineNumber;
                        string currentLine = TotalLines.GetLineText(lineIndex);

                        if (i == 0 && currentLine.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                            TextSelection.StartPosition.CharacterPosition -= TabCharacter.Length;
                        else if (i == SelectedLinesCount && currentLine.Contains(TabCharacter, System.StringComparison.Ordinal))
                        {
                            TextSelection.EndPosition.CharacterPosition -= TabCharacter.Length;
                        }

                        TotalLines.SetLineText(lineIndex, currentLine.RemoveFirstOccurence(TabCharacter));
                    }
                }, TotalLines, tempSel, SelectedLinesCount, NewLineCharacter);

                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }

        public static TextSelection MoveTab(PooledList<string> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter, UndoRedo UndoRedo)
        {
            if (TextSelection == null)
            {
                string line = TotalLines.GetLineText(CursorPosition.LineNumber);

                UndoRedo.RecordUndoAction(() =>
                {
                    TotalLines.SetLineText(CursorPosition.LineNumber, line.AddText(TabCharacter, CursorPosition.CharacterPosition));
                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);

                CursorPosition.AddToCharacterPos(TabCharacter.Length);
                return new TextSelection(CursorPosition, null);
            }
            else
            {
                TextSelection = Selection.OrderTextSelection(TextSelection);
                int SelectedLinesCount = TextSelection.EndPosition.LineNumber - TextSelection.StartPosition.LineNumber;

                if (TextSelection.StartPosition.LineNumber == TextSelection.EndPosition.LineNumber) //Singleline
                {
                    TextSelection.StartPosition = Selection.Replace(TextSelection, TotalLines, TabCharacter, NewLineCharacter);
                }
                else
                {
                    TextSelection tempSel = new TextSelection(TextSelection);
                    tempSel.StartPosition.CharacterPosition = 0;
                    tempSel.EndPosition.CharacterPosition = TotalLines.GetLineText(TextSelection.EndPosition.LineNumber).Length + TabCharacter.Length;

                    TextSelection.EndPosition.CharacterPosition += TabCharacter.Length;
                    TextSelection.StartPosition.CharacterPosition += TabCharacter.Length;

                    UndoRedo.RecordUndoAction(() =>
                    {
                        for (int i = TextSelection.StartPosition.LineNumber; i < SelectedLinesCount + TextSelection.StartPosition.LineNumber + 1; i++)
                        {
                            TotalLines.SetLineText(i, TotalLines.GetLineText(i).AddToStart(TabCharacter));
                        }
                    }, TotalLines, tempSel, SelectedLinesCount + 1, NewLineCharacter);
                }
                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }
    }
}