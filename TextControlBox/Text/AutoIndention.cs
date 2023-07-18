using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox.Text
{
    internal class AutoIndentation
    {
        private string IndentionCharacterLeft = "{";
        private string IndentionCharacterRight = "}";
        private string IndentionString = "\t";
        private int indentationLevel = 0;

        private void UpdateIndentation(PooledList<string> TotalLines, int index)
        {
            if (TotalLines[index].Contains(IndentionCharacterRight))
                DecreaseIndentation();
            else if (TotalLines[index].Contains(IndentionCharacterLeft))
                IncreaseIndentation();
        }

        public bool IndentCurrent(PooledList<string> TotalLines, int line)
        {
            indentationLevel = 0;
            for (int i = 0; i < TotalLines.Count; i++)
            {
                bool checkedRight = false;
                bool needsCheck = true;

                //Line contains both:
                if (TotalLines[i].Contains(IndentionCharacterRight) && TotalLines[i].Contains(IndentionCharacterLeft))
                {
                    int right = TotalLines[i].Split(IndentionCharacterRight).Length;
                    int left = TotalLines[i].Split(IndentionCharacterLeft).Length;

                    if (right > left)
                        IncreaseIndentation();
                    else if (right < left)
                        DecreaseIndentation();
                    else
                        IncreaseIndentation();
                    needsCheck = false;
                }

                if (TotalLines[i].Contains(IndentionCharacterRight))
                {
                    DecreaseIndentation();
                    checkedRight = true;
                }
                if (i == line)
                {
                    TotalLines[i] = string.Join("", Enumerable.Repeat(IndentionString, indentationLevel)) + TotalLines[i].Trim();
                    return true;
                }

                if (!needsCheck)
                    continue;

                if (!checkedRight && TotalLines[i].Contains(IndentionCharacterRight))
                    DecreaseIndentation();
                else if (TotalLines[i].Contains(IndentionCharacterLeft))
                    IncreaseIndentation();
            }
            return false;
        }

        public void IndentAll(PooledList<string> TotalLines)
        {
            for(int i= 0; i<TotalLines.Count; i++)
            {
                bool checkedRight = false;
                bool needsCheck = true;

                //Line contains both:
                if (TotalLines[i].Contains(IndentionCharacterRight) && TotalLines[i].Contains(IndentionCharacterLeft))
                {
                    int right = TotalLines[i].Split(IndentionCharacterRight).Length;
                    int left = TotalLines[i].Split(IndentionCharacterLeft).Length;

                    if (right > left)
                        IncreaseIndentation();
                    else if (right < left)
                        DecreaseIndentation();
                    else 
                        IncreaseIndentation();
                    needsCheck = false;
                }

                if (TotalLines[i].Contains(IndentionCharacterRight))
                {
                    DecreaseIndentation();
                    checkedRight = true;
                }

                string trimmedLine = TotalLines[i].Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                    TotalLines[i] = string.Join("", Enumerable.Repeat(IndentionString, indentationLevel)) + trimmedLine;

                if (!needsCheck)
                    continue;

                if (!checkedRight && TotalLines[i].Contains(IndentionCharacterRight))
                    DecreaseIndentation();
                else if (TotalLines[i].Contains(IndentionCharacterLeft))
                    IncreaseIndentation();
            }
        }
        public void IncreaseIndentation()
        {
            indentationLevel++;
        }
        public void DecreaseIndentation()
        {
            if (indentationLevel > 0)
            {
                indentationLevel--;
            }
        }
    }
}
