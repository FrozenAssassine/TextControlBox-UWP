using System.Collections.Generic;
using System.Diagnostics;
using TextControlBox_TestApp.TextControlBox.Renderer;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class CustomStack<T>
    {
        List<T> Items = new List<T>();
        int Limit = -1;

        public T Pop()
        {
            if (Items.Count == 0)
                return default;

            T Item =  Items[Items.Count - 1];
            Items.RemoveAt(Items.Count - 1);
            return Item;
        }
        public void Push(T item)
        {
            //Remove the last item if limit is exceed
            if (Items.Count > Limit && Limit != -1)
                Items.RemoveAt(0);

            Items.Add(item);
        }
        public void Clear()
        {
            Items.Clear();
        }
        public void SetLimit(int Limit)
        {
            this.Limit = Limit;
        }
        public int Count => Items.Count;
    }

    public class UndoRedo
    {
        public string EnteringText { get; set; } = "";
        private CustomStack<UndoRedoClass> UndoStack = new CustomStack<UndoRedoClass>();
        
        public void ClearStacks()
        {
            UndoStack.Clear();
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
        public void RecordMultiLineUndo(int StartLine, List<Line> RemovedLines, int LinesToDelete, TextSelection Selection = null)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.MultilineEdit,
                LineNumber = StartLine,
                RemovedLines = RemovedLines,
                LinesToDelete = LinesToDelete,
                TextSelection = Selection
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

        public void RecordNewLineUndo(List<Line> RemovedLines, int LinesToDelete, int StartLine, TextSelection TextSelection)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.NewLineEdit,
                RemovedLines = RemovedLines,
                LineNumber = StartLine,
                LinesToDelete = LinesToDelete,
                TextSelection = TextSelection
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
                    ListHelper.GetLine(TotalLines, item.LineNumber - 1).SetText(this.EnteringText);
                }
                if (item.LineNumber - 1 >= 0 && item.LineNumber - 1 < TotalLines.Count)
                    ListHelper.GetLine(TotalLines, item.LineNumber - 1).SetText(item.Text);
            }
            else if (item.UndoRedoType == UndoRedoType.NewLineEdit)
            {
                int LineNumber = item.LineNumber < 0 ? 0 : item.LineNumber;
                if (LineNumber + item.LinesToDelete >= TotalLines.Count)
                    item.LinesToDelete = TotalLines.Count - LineNumber < 0 ? 0 : TotalLines.Count - LineNumber;

                ListHelper.InsertRange(TotalLines, item.RemovedLines, LineNumber);
                ListHelper.RemoveRange(TotalLines, LineNumber, item.LinesToDelete);
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
