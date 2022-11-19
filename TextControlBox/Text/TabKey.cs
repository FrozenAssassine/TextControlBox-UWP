using Collections.Pooled;
using TextControlBox.Extensions;
using TextControlBox.Helper;

namespace TextControlBox.Text
{
    public class TabKey
    {
        public static TextSelection MoveTabBack(PooledList<Line> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter, UndoRedo UndoRedo)
        {
            if (TextSelection == null)
            {
                Line Line = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber);
                if (Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                    CursorPosition.SubtractFromCharacterPos(TabCharacter.Length);

                UndoRedo.RecordUndoAction(() =>
                {
                    Line.Content = Line.Content.RemoveFirstOccurence(TabCharacter);
                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);

                return new TextSelection(CursorPosition, null);
            }
            else
            {
                var OrderedSelection = Selection.OrderTextSelection(TextSelection);
                var Lines = Selection.GetSelectedLines(TotalLines, OrderedSelection);

                TextSelection tempSel = new TextSelection(OrderedSelection);
                tempSel.StartPosition.CharacterPosition = 0;
                tempSel.EndPosition.CharacterPosition = ListHelper.GetLine(TotalLines, TextSelection.EndPosition.LineNumber).Length + TabCharacter.Length;

                UndoRedo.RecordUndoAction(() =>
                {
                    for (int i = 0; i < Lines.Count; i++)
                    {
                        Line Line = Lines[i];
                        if (i == 0 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                            OrderedSelection.StartPosition.SubtractFromCharacterPos(TabCharacter.Length);
                        else if (i == Lines.Count - 1 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal))
                            OrderedSelection.EndPosition.SubtractFromCharacterPos(TabCharacter.Length);

                        Line.SetText(Line.Content.RemoveFirstOccurence(TabCharacter));
                    }
                }, TotalLines, tempSel, Lines.Count, NewLineCharacter);

                return new TextSelection(new CursorPosition(OrderedSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }

        public static TextSelection MoveTab(PooledList<Line> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter, UndoRedo UndoRedo)
        {
            if (TextSelection == null)
            {
                Line Line = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber);

                UndoRedo.RecordUndoAction(() =>
                {
                    Line.AddText(TabCharacter, CursorPosition.CharacterPosition);
                }, TotalLines, CursorPosition.LineNumber, 1, 1, NewLineCharacter);

                CursorPosition.AddToCharacterPos(TabCharacter.Length);
                return new TextSelection(CursorPosition, null);
            }
            else
            {
                var Lines = Selection.GetSelectedLines(TotalLines, TextSelection);
                TextSelection = Selection.OrderTextSelection(TextSelection);

                if (TextSelection.StartPosition.LineNumber == TextSelection.EndPosition.LineNumber) //Singleline
                {
                    TextSelection.StartPosition = Selection.Replace(TextSelection, TotalLines, TabCharacter, NewLineCharacter);
                }
                else
                {
                    TextSelection.EndPosition.AddToCharacterPos(TabCharacter.Length);
                    TextSelection.StartPosition.AddToCharacterPos(TabCharacter.Length);

                    TextSelection tempSel = new TextSelection(TextSelection);
                    tempSel.StartPosition.CharacterPosition = 0;
                    tempSel.EndPosition.CharacterPosition = ListHelper.GetLine(TotalLines, TextSelection.EndPosition.LineNumber).Length + TabCharacter.Length;

                    UndoRedo.RecordUndoAction(() =>
                    {
                        for (int i = 0; i < Lines.Count; i++)
                        {
                            Lines[i].AddText(TabCharacter, 0);
                        }
                    }, TotalLines, tempSel, Lines.Count, NewLineCharacter);
                }
                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }
    }
}