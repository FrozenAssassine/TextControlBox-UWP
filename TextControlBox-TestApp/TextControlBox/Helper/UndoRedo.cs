using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class UndoRedo
    {
        private Stack<UndoRedoClass> UndoStack = new Stack<UndoRedoClass>();
        private Stack<UndoRedoClass> RedoStack = new Stack<UndoRedoClass>();

        //Multiline undo is used to undo longer textinserts using paste and stuff like that
        public void RecordMultiLineUndo(Line StartLine, Line EndLine, CursorPosition CursorPosition)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.MultilineEdit,
                CharacterPosition = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
                StartLine = StartLine,
                EndLine = EndLine
            });
        }
        
        //Singleline undo is used for textchanges in a line
        public void RecordSingleLineUndo(Line CurrentLine, CursorPosition CursorPosition)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.SingleLineEdit,
                SingleLineText = CurrentLine.Content,
                CharacterPosition = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
            });
        }

        public void RecordNewLineUndo(Line NewLine, CursorPosition CursorPosition)
        {
            UndoStack.Push(new UndoRedoClass
            {
                UndoRedoType = UndoRedoType.NewLineEdit,
                LineToRemove = NewLine,
                CharacterPosition = CursorPosition.CharacterPosition + 1,
                LineNumber = CursorPosition.LineNumber,
            });
        }

        public void Undo(List<Line> TotalLines, TextControlBox Textbox)
        {
            if (UndoStack.Count < 1)
                return;

            UndoRedoClass item = UndoStack.Pop();
            if(item.UndoRedoType == UndoRedoType.SingleLineEdit)
            {
                if(item.LineNumber - 1 >= 0 && item.LineNumber - 1 < TotalLines.Count)
                TotalLines[item.LineNumber - 1].SetText(item.SingleLineText);
            }
            else if(item.UndoRedoType == UndoRedoType.NewLineEdit)
            {
                TotalLines.Remove(item.LineToRemove);
            }
            else if(item.UndoRedoType == UndoRedoType.MultilineEdit)
            {
                int StartLineIndex = item.StartLine != null ? TotalLines.IndexOf(item.StartLine) : 0;
                int EndLineIndex = item.StartLine != null ? TotalLines.IndexOf(item.EndLine) : 0;

                for (int i = StartLineIndex; i < EndLineIndex; i++)
                {
                    if (i == 0)
                        TotalLines[i].SetText(item.StartLine.Content);
                    else if (i == EndLineIndex - 1)
                        TotalLines[i].SetText(item.EndLine.Content);
                    else
                        TotalLines.RemoveAt(i);
                }
            }

            Textbox.CursorPosition = new CursorPosition(item.CharacterPosition, item.LineNumber);
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

        public string SingleLineText { get; set; } = ""; //Used for SingleLineEdit
        public Line LineToRemove { get; set; } = null; //Used for NewLineEdit
        public Line StartLine { get; set; } = null; //Used for MultiLineEdit
        public Line EndLine { get; set; } = null; //Used for MultiLineEdit
    }

}
