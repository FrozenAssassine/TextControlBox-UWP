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

        private void RecordRedo(UndoRedoItem item)
        {
            RedoStack.Push(item);
        }
        private void RecordUndo(UndoRedoItem item)
        {
            UndoStack.Push(item);
        }

        private TextSelection DoSingleLineUndo(List<Line> TotalLines, UndoRedoItem item)
        {
            ListHelper.GetLine(TotalLines, item.StartLine).SetText(item.UndoText);
            return null;
        }
        private TextSelection DoMultilineUndo(List<Line> TotalLines, UndoRedoItem item, string NewLineCharacter)
        {
            if (item.Selection != null)
            {
                var sel = Selection.OrderTextSelection(item.Selection);
                if (item.IsDeletion &&  sel.StartPosition.LineNumber < sel.EndPosition.LineNumber)
                {            
                    //If all lines are completely selected Count has to be smaller, because no line will stay after deleting the selection
                    if (sel.StartPosition.CharacterPosition == 0 &&
                        sel.EndPosition.CharacterPosition == ListHelper.GetLine(TotalLines, item.StartLine + item.Count - 2).Length)
                    {
                        item.Count -= 1;
                    }
                }
            }

            var lines = ListHelper.GetLinesFromString(item.UndoText, NewLineCharacter);

            ListHelper.RemoveRange(TotalLines, item.StartLine, item.Count);
            ListHelper.InsertRange(TotalLines, lines, item.StartLine);

            return item.Selection;
        }
        private TextSelection DoNewLineUndo(List<Line> TotalLines, UndoRedoItem item, string NewLineCharacter)
        {
            var lines = ListHelper.GetLinesFromString(item.UndoText, NewLineCharacter);

            if (item.StartLine + item.Count > TotalLines.Count)
                item.Count = TotalLines.Count - item.StartLine;

            ListHelper.RemoveRange(TotalLines, item.StartLine, 3);
            ListHelper.InsertRange(TotalLines, lines, item.StartLine);

            return item.Selection;
        }
        
        private TextSelection DoSingleLineRedo(List<Line> TotalLines, UndoRedoItem item)
        {
            ListHelper.GetLine(TotalLines, item.StartLine).SetText(item.UndoText);
            return null;
        }
        private TextSelection DoMultilineRedo(List<Line> TotalLines, UndoRedoItem item, string NewLineCharacter)
        {
            if (item.Selection == null)
            {
                var lines = ListHelper.GetLinesFromString(item.RedoText, NewLineCharacter);

                if (item.IsDeletion)
                    ListHelper.RemoveRange(TotalLines, item.StartLine, item.Count);
                else
                {
                    ListHelper.RemoveRange(TotalLines, item.StartLine, 1);
                    ListHelper.InsertRange(TotalLines, lines, item.StartLine);
                }
                return null;
            }
            else
            {
                if (item.IsDeletion)
                    Selection.Remove(item.Selection, TotalLines);
                else
                    Selection.Replace(item.Selection, TotalLines, item.RedoText == "" ? item.UndoText : item.RedoText, NewLineCharacter);
                return null;
            }
        }
        private TextSelection DoNewLineRedo(List<Line> TotalLines, UndoRedoItem item, string NewLineCharacter)
        {
            if (item.Selection == null)
            {
                //Delete always two lines as default
                ListHelper.RemoveRange(TotalLines, item.StartLine, 1);
                if (!item.IsDeletion)
                {
                    var lines = ListHelper.GetLinesFromString(item.RedoText, NewLineCharacter);
                    ListHelper.InsertRange(TotalLines, lines, item.StartLine);
                }
            }
            else
            {
                return DoMultilineRedo(TotalLines, item, NewLineCharacter);
            }
            return null;
        }

        public void RecordSingleLineUndo(string LineText, int StartLine, bool IsDeletion)
        {
            UndoStack.Push(new UndoRedoItem
            {
                Count = LineText.Length,
                StartLine = StartLine,
                IsDeletion = IsDeletion,
                UndoText = LineText,
                Selection = null,
                UndoRedoType = UndoRedoType.SingleLineEdit
            });
        }
        public void RecordMultiLineUndo(List<Line> TotalLines, int StartLine, int Count, string RedoText, TextSelection TextSelection, string NewLineCharacter, bool IsDeletion, bool ChangeCount = true)
        {
            string Text;
            if (TextSelection == null)
                Text = ListHelper.GetLinesAsString(TotalLines, StartLine, Count, NewLineCharacter);
            else
                Text = Selection.GetSelectedTextWithoutCharacterPos(TotalLines, TextSelection, NewLineCharacter);

            RecordMultiLineUndo(TotalLines, StartLine, Count, Text, RedoText, TextSelection, NewLineCharacter, IsDeletion, ChangeCount);
        }
        public void RecordMultiLineUndo(List<Line> TotalLines, int StartLine, int Count, string UndoText, string RedoText, TextSelection TextSelection, string NewLineCharacter, bool IsDeletion, bool ChangeCount = true, bool ExcecutePrevUndoToo = false)
        {
            UndoStack.Push(new UndoRedoItem
            {
                StartLine = TextSelection == null ? StartLine : Selection.GetMin(TextSelection).LineNumber,
                Count = ChangeCount ? TextSelection == null ? 1 : Count : Count,
                UndoText = UndoText,
                Selection = TextSelection,
                UndoRedoType = UndoRedoType.MultilineEdit,
                IsDeletion = IsDeletion,
                RedoText = RedoText,
                ExcecutePrevUndoToo = ExcecutePrevUndoToo,
            });
        }
        public void RecordNewLineUndo(List<Line> TotalLines, int StartLine, int Count, string UndoText, string RedoText, TextSelection TextSelection, string NewLineCharacter)
        {
            UndoStack.Push(new UndoRedoItem
            {
                StartLine = TextSelection == null ? StartLine : Selection.GetMin(TextSelection).LineNumber,
                Count = Count,
                UndoText = UndoText,
                Selection = TextSelection,
                UndoRedoType = UndoRedoType.NewLineEdit,
                IsDeletion = false,
                RedoText = RedoText
            });
        }

        /// <summary>
        /// Excecutes the undo and apply the changes to the text
        /// </summary>
        /// <param name="TotalLines">A list containing all the lines of the textbox</param>
        /// <param name="NewLineCharacter">The current line-ending character either CR, LF or CRLF</param>
        /// <returns>A class containing the start and end-position of the selection</returns>
        public TextSelection Undo(List<Line> TotalLines, string NewLineCharacter)
        {
            if (UndoStack.Count < 1)
                return null;

            UndoRedoItem item = UndoStack.Pop();
            if(!item.ExcecutePrevUndoToo)
                RecordRedo(item);
            
            if (item.UndoRedoType == UndoRedoType.SingleLineEdit)
                return DoSingleLineUndo(TotalLines, item);
            else if (item.UndoRedoType == UndoRedoType.NewLineEdit)
                return DoNewLineUndo(TotalLines, item, NewLineCharacter);
            else
            {
                var res = DoMultilineUndo(TotalLines, item, NewLineCharacter);
                if (item.ExcecutePrevUndoToo)
                {
                    var NewItem = UndoStack.Pop();
                    RecordRedo(NewItem);
                    RecordRedo(item);
                    return DoMultilineUndo(TotalLines, NewItem, NewLineCharacter);
                }
                return res;
            }
        }
        
        /// <summary>
        /// Excecutes the redo and apply the changes to the text
        /// </summary>
        /// <param name="TotalLines">A list containing all the lines of the textbox</param>
        /// <param name="NewLineCharacter">The current line-ending character either CR, LF or CRLF</param>
        /// <returns>A class containing the start and end-position of the selection</returns>
        public TextSelection Redo(List<Line> TotalLines, string NewLineCharacter)
        {
            if (RedoStack.Count < 1)
                return null;

            UndoRedoItem item = RedoStack.Pop();
            if (!item.ExcecutePrevUndoToo)
                RecordUndo(item);
            
            if (item.UndoRedoType == UndoRedoType.SingleLineEdit)  
                return DoSingleLineRedo(TotalLines, item);
            else if(item.UndoRedoType == UndoRedoType.NewLineEdit)
                return DoNewLineRedo(TotalLines, item, NewLineCharacter);
            else
            {
                var res = DoMultilineRedo(TotalLines, item, NewLineCharacter);
                if (item.ExcecutePrevUndoToo)
                {
                    var NewItem = RedoStack.Pop();
                    RecordUndo(NewItem);
                    RecordUndo(item);
                    return DoMultilineRedo(TotalLines, NewItem, NewLineCharacter);
                }
                return res;
            }
        }

        /// <summary>
        /// Clears all the items in the undo and redo stack
        /// </summary>
        public void ClearAll()
        {
            UndoStack.Clear();
            RedoStack.Clear();
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

    public enum UndoRedoType
    {
        SingleLineEdit, MultilineEdit, NewLineEdit
    }
    public struct UndoRedoItem
    {
        public bool IsDeletion { get; set; }
        public int StartLine { get; set; }
        public int Count { get; set; }
        public string UndoText { get; set; }
        public string RedoText { get; set; }
        public TextSelection Selection { get; set; }
        public UndoRedoType UndoRedoType { get; set; }
        public bool ExcecutePrevUndoToo { get; set; }
    }
}
