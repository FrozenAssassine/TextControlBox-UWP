using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextControlBox_TestApp.TextControlBox.Helper
{
    public class DebugHelper
    {
        public static void DebugList(List<Line> List, string DebugTitle = "--DebugList--")
        {
            Debug.WriteLine(DebugTitle);
            for(int i = 0; i<List.Count; i++)
            {
                Debug.WriteLine("\t" + List[i].Content);
            }
        }
    }
}
