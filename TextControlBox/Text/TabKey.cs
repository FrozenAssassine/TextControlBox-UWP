using Collections.Pooled;
using System.Collections.Generic;
using System.Diagnostics;
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
                    CursorPosition.SubtractFromCharacterPos(1);

                UndoRedo.RecordUndo(() =>
                {
                    Line.Content = Line.Content.RemoveFirstOccurence(TabCharacter);
                }, TotalLines, CursorPosition.LineNumber, NewLineCharacter, false);

                return new TextSelection(CursorPosition, null);
            }
            else
            {
                var OrderedSelection = Selection.OrderTextSelection(TextSelection);
                var Lines = Selection.GetPointerToSelectedLines(TotalLines, OrderedSelection);

                string UndoText = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, OrderedSelection, NewLineCharacter);

                for (int i = 0; i < Lines.Count; i++)
                {
                    Line Line = Lines[i];
                    if (i == 0 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                        OrderedSelection.StartPosition.SubtractFromCharacterPos(1);
                    else if (i == Lines.Count - 1 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal))
                        OrderedSelection.EndPosition.SubtractFromCharacterPos(1);

                    Line.SetText(Line.Content.RemoveFirstOccurence(TabCharacter));
                }
                string RedoText = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, OrderedSelection, NewLineCharacter);
                TextSelection tempSel = new TextSelection(OrderedSelection);
                tempSel.StartPosition.CharacterPosition = 0;
                tempSel.EndPosition.CharacterPosition = ListHelper.GetLine(TotalLines, TextSelection.EndPosition.LineNumber).Length;

                UndoRedo.RecordMultiLineUndo(OrderedSelection.StartPosition.LineNumber, Lines.Count, UndoText, RedoText, tempSel, false);

                return new TextSelection(new CursorPosition(OrderedSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }

        public static TextSelection MoveTab(PooledList<Line> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter, UndoRedo UndoRedo)
        {
            if (TextSelection == null)
            {
                Line Line = ListHelper.GetLine(TotalLines, CursorPosition.LineNumber);

                UndoRedo.RecordUndo(() =>
                {
                    Line.AddText(TabCharacter, CursorPosition.CharacterPosition);
                }, TotalLines, CursorPosition.LineNumber, NewLineCharacter, false);

                CursorPosition.AddToCharacterPos(1);
                return new TextSelection(CursorPosition, null);
            }
            else
            {
                var Lines = Selection.GetPointerToSelectedLines(TotalLines, TextSelection);
                TextSelection = Selection.OrderTextSelection(TextSelection);

                if (TextSelection.StartPosition.LineNumber == TextSelection.EndPosition.LineNumber) //Singleline
                {
                    TextSelection.StartPosition = Selection.Replace(TextSelection, TotalLines, TabCharacter, NewLineCharacter);
                }
                else
                {
                    string UndoText = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, NewLineCharacter);

                    for (int i = 0; i < Lines.Count; i++)
                    {
                        Lines[i].AddText(TabCharacter, 0);
                    }
                    string RedoText = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, NewLineCharacter);

                    TextSelection.EndPosition.AddToCharacterPos(1);
                    TextSelection.StartPosition.AddToCharacterPos(1);

                    TextSelection tempSel = new TextSelection(TextSelection);
                    tempSel.StartPosition.CharacterPosition = 0;
                    tempSel.EndPosition.CharacterPosition = ListHelper.GetLine(TotalLines, TextSelection.EndPosition.LineNumber).Length;

                    UndoRedo.RecordMultiLineUndo(TextSelection.StartPosition.LineNumber, Lines.Count, UndoText, RedoText, tempSel, false);
                }
                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }
    }
}