using System;

namespace TextControlBox.Extensions
{
    internal static class StringExtension
    {
        public static int IndexOfWholeWord(string Text, string Word, int StartIndex)
        {
            for (int j = StartIndex; j < Text.Length &&
                (j = Text.IndexOf(Word, j, StringComparison.Ordinal)) >= 0; j++)
            {
                if ((j == 0 || !char.IsLetterOrDigit(Text, j - 1)) &&
                    (j + Word.Length == Text.Length || !char.IsLetterOrDigit(Text, j + Word.Length)))
                {
                    return j;
                }
            }

            return -1;
        }
        public static int LastIndexOfWholeWord(string Text, string Word)
        {
            int StartIndex = Text.Length - 1;
            while (StartIndex >= 0 && (StartIndex = Text.LastIndexOf(Word, StartIndex, StringComparison.Ordinal)) != -1)
            {
                if (StartIndex > 0)
                {
                    if (!char.IsLetterOrDigit(Text[StartIndex - 1]))
                    {
                        return StartIndex;
                    }
                }

                if (StartIndex + Text.Length < Text.Length)
                {
                    if (!char.IsLetterOrDigit(Text[StartIndex + Word.Length]))
                    {
                        return StartIndex;
                    }
                }

                StartIndex--;
            }
            return -1;
        }
        public static string RemoveFirstOccurence(this string value, string removeString)
        {
            int index = value.IndexOf(removeString, StringComparison.Ordinal);
            return index < 0 ? value : value.Remove(index, removeString.Length);
        }
    }
}
