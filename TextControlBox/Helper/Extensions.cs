namespace TextControlBox.Helper
{
    public static class Extensions
    {
        public static int NumberOfOccurences(this string text, string character)
        {
            return (text.Length - text.Replace(character, "").Length) / character.Length;
        }

    }
}
