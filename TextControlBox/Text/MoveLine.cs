using Collections.Pooled;
using System.Diagnostics;

namespace TextControlBox.Text
{
    internal class MoveLine
    {
        public static TextSelection Move(PooledList<string> TotalLines, TextSelection selection, CursorPosition cursorposition, UndoRedo undoredo, string NewLineCharacter, MoveDirection direction)
        {
            TextSelection result = null;
            if (direction == MoveDirection.Down)
            {
                if (selection == null)
                {
                    if (cursorposition.LineNumber >= TotalLines.Count - 1)
                        return null;

                    undoredo.RecordUndoAction(() =>
                    {
                        Selection.MoveLinesDown(TotalLines, selection, cursorposition);
                    
                    }, TotalLines, cursorposition.LineNumber, 2, 2, NewLineCharacter, cursorposition);
                    return result;
                }
            }
            else
            {
                if (selection == null)
                {
                    if (cursorposition.LineNumber <= 0)
                        return null;

                    undoredo.RecordUndoAction(() =>
                    {
                        Selection.MoveLinesUp(TotalLines, selection, cursorposition);

                    }, TotalLines, cursorposition.LineNumber - 1, 2, 2, NewLineCharacter, cursorposition);
                    return result;
                }
            }
            return null;
        }
    }
    internal enum MoveDirection
    {
        Up, Down
    }
}
