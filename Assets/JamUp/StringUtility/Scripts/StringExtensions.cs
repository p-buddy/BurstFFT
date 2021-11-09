using System;

namespace JamUp.StringUtility
{
    public static class StringExtensions
    {
        public static string RemoveSubString(this string fullString, string substring)
        {
            int indexOf = fullString.IndexOf(substring, StringComparison.Ordinal);
            int length = substring.Length;
            return fullString.Remove(indexOf, length);
        }
    }
}