using System;
using Diacritics.Extensions;

namespace UserApi.Extensions
{
    public static class StringExtensions
    {
        public static string ExtractBasePrincipalName(this string text, string baseText)
        {
            // deleted users have a GUID prefixed to their original username
            var startIndex = text.IndexOf(baseText, StringComparison.Ordinal);
            if (startIndex != -1)
            {
                return text.Substring(startIndex);
            }
            return null;
        }
        
        /// <summary>
        /// Replace diacritic characters (eg é) with their closest ASCII equivalent (e)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ReplaceDiacriticCharacters(this string input)
        {
            return input.RemoveDiacritics();
        }
    }
}