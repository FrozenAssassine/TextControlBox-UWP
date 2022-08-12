using System.Collections.Generic;
using System.Diagnostics;
using TextControlBox_TestApp.TextControlBox.Renderer;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class TabKey
    {
        public static TextSelection MoveTabBack(List<Line> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter)
        {
            if (TextSelection == null)
            {
                Line Line = TotalLines[CursorPosition.LineNumber - 1];

                if (Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                    CursorPosition.SubtractFromCharacterPos(1);

                Line.Content = Line.Content.RemoveFirstOccurence(TabCharacter);

                return new TextSelection(CursorPosition, null);
            }
            else
            {
                TextSelection = Selection.OrderTextSelection(TextSelection);
                var Lines = Selection.GetPointerToSelectedLines(TotalLines, TextSelection);
                for (int i = 0; i < Lines.Count; i++)
                {
                    Line Line = Lines[i];
                    if (i == 0 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal) && CursorPosition.CharacterPosition > 0)
                        TextSelection.StartPosition.SubtractFromCharacterPos(1);
                    else if(i == Lines.Count - 1 && Line.Content.Contains(TabCharacter, System.StringComparison.Ordinal))
                        TextSelection.EndPosition.SubtractFromCharacterPos(1);
                    
                    Line.SetText(Line.Content.RemoveFirstOccurence(TabCharacter));
                }
                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));
            }
        }

        public static TextSelection MoveTab(List<Line> TotalLines, TextSelection TextSelection, CursorPosition CursorPosition, string TabCharacter, string NewLineCharacter)
        {
            if (TextSelection == null)
            {
                TotalLines[CursorPosition.LineNumber - 1 < TotalLines.Count ? CursorPosition.LineNumber - 1 : TotalLines.Count - 1].AddText(TabCharacter, CursorPosition.CharacterPosition);
                CursorPosition.AddToCharacterPos(1);
                return new TextSelection(CursorPosition, null);
            }
            else
            {
                var Lines = Selection.GetPointerToSelectedLines(TotalLines, TextSelection);

                TextSelection = Selection.OrderTextSelection(TextSelection);
                int StartLine = TextSelection.StartPosition.LineNumber;
                int EndLine = TextSelection.EndPosition.LineNumber;

                if (StartLine == EndLine) //Singleline
                {
                    TextSelection.StartPosition = Selection.Replace(TextSelection, TotalLines, TabCharacter, NewLineCharacter);
                }
                else
                {
                    for (int i = 0; i < Lines.Count; i++)
                    {
                        Lines[i].AddText(TabCharacter, 0);
                    }
                    TextSelection.StartPosition.AddToCharacterPos(1);
                    TextSelection.EndPosition.AddToCharacterPos(1);
                }
                return new TextSelection(new CursorPosition(TextSelection.StartPosition), new CursorPosition(TextSelection.EndPosition));

            }
        }
    }
}