using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextControlBox_TestApp.TextControlBox.Renderer;
using Windows.UI.Text;
using static System.Net.Mime.MediaTypeNames;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class UndoRedo
    {
        private Stack<UndoRedoClass> UndoStack = new Stack<UndoRedoClass>();
        private Stack<UndoRedoClass> RedoStack = new Stack<UndoRedoClass>();

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
                CharacterPosition = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
            });
        }

        public void RecordNewLineUndo(List<Line> RemovedLines, Line NewLine, CursorPosition CursorPosition)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.NewLineEdit,
                LineToRemove = NewLine,
                RemovedLines = RemovedLines,
                CharacterPosition = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
            });
        }

        public TextSelection Undo(List<Line> TotalLines, TextControlBox Textbox, string NewLineCharacter)
        {
            if (UndoStack.Count < 1)
                return null;

            UndoRedoClass item = UndoStack.Pop();
            if(item.UndoRedoType == UndoRedoType.SingleLineEdit)
            {
                if(item.LineNumber - 1 >= 0 && item.LineNumber - 1 < TotalLines.Count)
                    TotalLines[item.LineNumber - 1].SetText(item.Text);
            }
            else if(item.UndoRedoType == UndoRedoType.NewLineEdit)
            {
                TotalLines.RemoveRange(item.LineNumber-1, 2);
                TotalLines.InsertRange(item.LineNumber-1, item.RemovedLines);
            }
            else if(item.UndoRedoType == UndoRedoType.MultilineEdit)
            {
                Selection.ReplaceUndo(item.LineNumber, TotalLines, item.RemovedLines, item.LinesToDelete);
                return item.TextSelection;
            }
            return null;

            //Textbox.CursorPosition = new CursorPosition(item.CharacterPosition, item.LineNumber);
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
        public int CharacterPosition { get; set; } = 0;
        public string Text { get; set; } = "";

        public Line LineToRemove { get; set; } = null; //Used for NewLineEdit

        public int LinesToDelete { get; set; } = 0; //Mutliline
        public TextSelection TextSelection { get; set; } = null; //Multiline
        public List<Line> RemovedLines { get; set; } = null; //Multiline
    }
}
