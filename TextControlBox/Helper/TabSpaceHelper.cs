using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextControlBox.Text;

namespace TextControlBox.Helper
{
    public class TabSpaceHelper
    {
        private int _NumberOfSpaces = 4;
        public int NumberOfSpaces 
        {
            get => _NumberOfSpaces; 
            set
            {
                if (value != _NumberOfSpaces)
                    Spaces = new string(' ', NumberOfSpaces);
            }
        }
        public bool UseSpacesInsteadTabs = false;
        public string TabCharacter { get => UseSpacesInsteadTabs ? Spaces : Tab; }
        private string Spaces = "    ";
        private string Tab = "\t";

        public void UpdateTabs(PooledList<Line> TotalLines)
        {
            if (UseSpacesInsteadTabs)
                ReplaceTabsToSpaces(TotalLines);
            else
                ReplaceSpacesToTabs(TotalLines);
        }         
        public string UpdateTabs(string input)
        {
            if (UseSpacesInsteadTabs)
                return Replace(input, Tab, Spaces);
            return Replace(input, Spaces, Tab);
        }

        private void ReplaceSpacesToTabs(PooledList<Line> TotalLines)
        {
            for (int i = 0; i < TotalLines.Count; i++)
            {
                Replace(TotalLines[i], Spaces, Tab);
            }
        }
        private void ReplaceTabsToSpaces(PooledList<Line> TotalLines)
        {
            for (int i = 0; i < TotalLines.Count; i++)
            {
                Replace(TotalLines[i], "\t", Spaces);
            }
        }
        private void Replace(Line line, string find, string replace)
        {
            line.Content = Replace(line.Content, find, replace);
        }
        public string Replace(string input, string find, string replace)
        {
            return input.Replace(find, replace, StringComparison.OrdinalIgnoreCase);
        }
    }
}
