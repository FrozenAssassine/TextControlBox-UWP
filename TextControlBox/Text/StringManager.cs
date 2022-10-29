using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextControlBox.Helper;

namespace TextControlBox.Text
{
    public class StringManager
    {
        private TabSpaceHelper tabSpaceHelper;
        public LineEnding lineEnding;

        public StringManager(TabSpaceHelper tsh)
        {
            tabSpaceHelper = tsh;
        }
        public string CleanUpString(string input)
        {
            //Fix tabs and lineendings
            return tabSpaceHelper.UpdateTabs(LineEndings.CleanLineEndings(input, lineEnding));
        }
    }
}
