using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TextControlBox.Helper;

namespace TextControlBox.Text
{
    public class UndoRedo
    {
        private Stack<UndoRedoItem> UndoStack = new Stack<UndoRedoItem>();
        private Stack<UndoRedoItem> RedoStack = new Stack<UndoRedoItem>();

        private bool HasRedone = false;

        private void RecordRedo(UndoRedoItem item)
        {
            RedoStack.Push(item);
        }
        private void RecordUndo(UndoRedoItem item)
        {
            UndoStack.Push(item);
        }

        private void AddUndoItem(TextSelection selection, int startLine, string undoText, string redoText, int undoCount, int redoCount)
        {
            UndoStack.Push(new UndoRedoItem
            {
                RedoText = redoText,
                UndoText = undoText,
                Selection = selection,
                StartLine = startLine,
                UndoCount = undoCount,
                RedoCount = redoCount,
            });
        }

        public void RecordUndoAction(Action action, PooledList<Line> TotalLines, int startline, int undocount, int redoCount, string NewLineCharacter)
        {
            var linesBefore = ListHelper.GetLinesAsString(TotalLines, startline, undocount, NewLineCharacter);
            action.Invoke();
            var linesAfter = ListHelper.GetLinesAsString(TotalLines, startline, redoCount, NewLineCharacter);

            AddUndoItem(null, startline, linesBefore, linesAfter, undocount, redoCount);
        }
        public void RecordUndoAction(Action action, PooledList<Line> TotalLines, TextSelection selection, int NumberOfAddedLines, string NewLineCharacter)
        {
            var orderedSel = Selection.OrderTextSelection(selection);

            //Check whether the selected lines are selected from start to end
            if (!(orderedSel.EndPosition.CharacterPosition == ListHelper.GetLine(TotalLines, orderedSel.EndPosition.LineNumber).Length &&
                orderedSel.StartPosition.CharacterPosition == 0) || Selection.WholeTextSelected(selection, TotalLines))
            {
                NumberOfAddedLines += 1;
            }

            var linesBefore = ListHelper.GetLinesAsString(TotalLines, orderedSel.StartPosition.LineNumber, orderedSel.EndPosition.LineNumber - orderedSel.StartPosition.LineNumber + 1, NewLineCharacter);
            action.Invoke();
            var linesAfter = ListHelper.GetLinesAsString(TotalLines, orderedSel.StartPosition.LineNumber, NumberOfAddedLines, NewLineCharacter);

            AddUndoItem(
                selection,
                orderedSel.StartPosition.LineNumber,
                linesBefore,
                linesAfter,
                orderedSel.EndPosition.LineNumber - orderedSel.StartPosition.LineNumber + 1,
                NumberOfAddedLines
                );
        }

        /// <summary>
        /// Excecutes the undo and apply the changes to the text
        /// </summary>
        /// <param name="TotalLines">A list containing all the lines of the textbox</param>
        /// <param name="NewLineCharacter">The current line-ending character either CR, LF or CRLF</param>
        /// <returns>A class containing the start and end-position of the selection</returns>
        public TextSelection Undo(PooledList<Line> TotalLines, string NewLineCharacter)
        {
            if (UndoStack.Count < 1)
                return null;

            if (HasRedone)
            {
                HasRedone = false;
                RedoStack.Clear();
            }

            UndoRedoItem item = UndoStack.Pop();
            RecordRedo(item);


            ListHelper.RemoveRange(TotalLines, item.StartLine, item.RedoCount);
            if (item.UndoCount > 0)
                ListHelper.InsertRange(TotalLines, ListHelper.GetLinesFromString(item.UndoText, NewLineCharacter), item.StartLine);

            return item.Selection;
        }

        /// <summary>
        /// Excecutes the redo and apply the changes to the text
        /// </summary>
        /// <param name="TotalLines">A list containing all the lines of the textbox</param>
        /// <param name="NewLineCharacter">The current line-ending character either CR, LF or CRLF</param>
        /// <returns>A class containing the start and end-position of the selection</returns>
        public TextSelection Redo(PooledList<Line> TotalLines, string NewLineCharacter)
        {
            if (RedoStack.Count < 1)
                return null;

            UndoRedoItem item = RedoStack.Pop();
            RecordUndo(item);
            HasRedone = true;

            ListHelper.RemoveRange(TotalLines, item.StartLine, item.UndoCount);    
            if (item.RedoCount > 0)
                ListHelper.InsertRange(TotalLines, ListHelper.GetLinesFromString(item.RedoText, NewLineCharacter), item.StartLine);

            return null;
        }

        /// <summary>
        /// Clears all the items in the undo and redo stack
        /// </summary>
        public void ClearAll()
        {
            UndoStack.Clear();
            RedoStack.Clear();
            UndoStack.TrimExcess();
            RedoStack.TrimExcess();
        }

        public void NullAll()
        {
            UndoStack = null;
            RedoStack = null;
        }

        /// <summary>
        /// Gets if the undo stack contains actions
        /// </summary>
        public bool CanUndo { get => UndoStack.Count > 0; }

        /// <summary>
        /// Gets if the redo stack contains actions
        /// </summary>
        public bool CanRedo { get => RedoStack.Count > 0; }
    }

    public struct UndoRedoItem
    {
        public int StartLine { get; set; }
        public string UndoText { get; set; }
        public string RedoText { get; set; }
        public int UndoCount { get; set; }
        public int RedoCount { get; set; }
        public TextSelection Selection { get; set; }
    }
}
