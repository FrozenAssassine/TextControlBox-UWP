using System.Collections.Generic;
using TextControlBox_TestApp.TextControlBox.Renderer;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class UndoRedo
    {
        public string EnteringText { get; set; } = "";
        private Stack<UndoRedoClass> UndoStack = new Stack<UndoRedoClass>();
        private Stack<UndoRedoClass> RedoStack = new Stack<UndoRedoClass>();

        public void ClearStacks()
        {
            UndoStack.Clear();
            RedoStack.Clear();
        }

        public void RecordUndoOnPress(Line CurrentLine, CursorPosition CursorPosition)
        {
            if (EnteringText.Length > 0)
            {
                EnteringText = "";
                RecordSingleLineUndo(CurrentLine, CursorPosition);
            }
        }

        //Multiline undo is used to undo longer textinserts using paste and stuff like that
        public void RecordMultiLineUndo(int StartLine, List<Line> RemovedLines, int LinesToDelete)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.MultilineEdit,
                LineNumber = StartLine,
                RemovedLines = RemovedLines,
                LinesToDelete = LinesToDelete
            });
        }

        //Singleline undo is used for textchanges in a line
        public void RecordSingleLineUndo(Line CurrentLine, CursorPosition CursorPosition)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.SingleLineEdit,
                Text = CurrentLine.Content,
                LineNumber = CursorPosition.LineNumber,
            });
        }

        public void RecordNewLineUndo(List<Line> RemovedLines, int LinesToDelete, int StartLine)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.NewLineEdit,
                RemovedLines = RemovedLines,
                LineNumber = StartLine,
                LinesToDelete = LinesToDelete
            });
        }

        public TextSelection Undo(List<Line> TotalLines, TextControlBox Textbox, string NewLineCharacter)
        {
            if (UndoStack.Count < 1)
                return null;

            UndoRedoClass item = UndoStack.Pop();
            if (item.UndoRedoType == UndoRedoType.SingleLineEdit)
            {
                if (this.EnteringText.Length > 0 && UndoStack.Count == 0)
                {
                    TotalLines[item.LineNumber - 1].SetText(this.EnteringText);
                }
                if (item.LineNumber - 1 >= 0 && item.LineNumber - 1 < TotalLines.Count)
                    TotalLines[item.LineNumber - 1].SetText(item.Text);
            }
            else if (item.UndoRedoType == UndoRedoType.NewLineEdit)
            {
                int LineNumber = item.LineNumber < 0 ? 0 : item.LineNumber;
                if (LineNumber + item.LinesToDelete >= TotalLines.Count)
                    item.LinesToDelete = TotalLines.Count - LineNumber < 0 ? 0 : TotalLines.Count - LineNumber;

                TotalLines.RemoveRange(LineNumber, item.LinesToDelete);
                TotalLines.InsertRange(LineNumber, item.RemovedLines);
            }
            else if (item.UndoRedoType == UndoRedoType.MultilineEdit)
            {
                Selection.ReplaceUndo(item.LineNumber, TotalLines, item.RemovedLines, item.LinesToDelete);
                return item.TextSelection;
            }
            return null;
        }

        public void Redo(List<Line> TotalLines, TextControlBox Textbox)
        {

        }
    }

    public enum UndoRedoType
    {
        SingleLineEdit, MultilineEdit, NewLineEdit
    }

    public class UndoRedoClass
    {
        public UndoRedoType UndoRedoType { get; set; } = UndoRedoType.SingleLineEdit;
        public int LineNumber { get; set; } = 0;
        public string Text { get; set; } = "";

        public int LinesToDelete { get; set; } = 0; //Mutliline
        public TextSelection TextSelection { get; set; } = null; //Multiline
        public List<Line> RemovedLines { get; set; } = null; //Multiline
    }
}
