using System.Collections.Generic;
using System.Diagnostics;

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

            T Item = Items[Items.Count - 1];
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
        private Stack<UndoRedoItem> UndoStack = new Stack<UndoRedoItem>();

        public void ClearStacks()
        {
            UndoStack.Clear();
        }
    }

    public enum UndoRedoType
    {
        SingleLineEdit, MultilineEdit, NewLineEdit
    }

    public class UndoRedoItem
    {
        public UndoRedoType UndoRedoType { get; set; } = UndoRedoType.SingleLineEdit;
        public int LineNumber { get; set; } = 0;
        public bool ExcecuteNextUndoToo { get; set; } = false;
        public int LinesToDelete { get; set; } = 0;
        public TextSelection TextSelection { get; set; } = null;
        public string Text { get; set; } = null;
    }
}
