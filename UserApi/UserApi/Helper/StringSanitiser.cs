using Diacritics.Extensions;

namespace UserApi.Helper;

public static class StringSanitiser
{
    /// <summary>
    /// Replace diacritic characters (eg é) with their closest ASCII equivalent (e)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string RemoveDiacriticCharacters(this string input)
    {
        return input.RemoveDiacritics();
    }
}