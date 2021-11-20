using System;
using System.IO;

namespace JamUp.StringUtility
{
    public static class StringHelper
    {
        public static string RemoveSubString(this string fullString, string substring)
        {
            int indexOf = fullString.IndexOf(substring, StringComparison.Ordinal);
            if (indexOf < 0 || indexOf >= fullString.Length)
            {
                return fullString;
            }
            
            int length = substring.Length;
            return fullString.Remove(indexOf, length);
        }
        
        public static string SubstringAfter(this string fullString, string matchString)
        {
            int indexOf = fullString.IndexOf(matchString, StringComparison.Ordinal);
            if (indexOf < 0 || indexOf >= fullString.Length)
            {
                return fullString;
            }
            
            return fullString.Remove(0, indexOf);
        }

        public static string NamespaceAsPath(this Type type)
        {
            return Path.Combine(type.Namespace.Split('.'));
        }
    }
}