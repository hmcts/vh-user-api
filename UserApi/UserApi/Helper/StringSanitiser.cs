using System.Globalization;
using System.Text;

namespace UserApi.Helper;

public static class StringSanitiser
{
    public static string RemoveAccents(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var sanitizedString = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Custom mapping for special characters
        sanitizedString = sanitizedString
            .Replace("Ð", "D")
            .Replace("ð", "d")
            .Replace('Ħ', 'H')
            .Replace('ħ', 'h')
            .Replace("ı", "i")
            .Replace("Ł", "L")
            .Replace("ł", "l")
            .Replace("Ø", "O")
            .Replace("ø", "o")
            .Replace("Ŧ", "T")
            .Replace("ŧ", "t");

        return sanitizedString;
    }
}