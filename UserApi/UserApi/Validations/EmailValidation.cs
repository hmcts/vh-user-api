using System.Text.RegularExpressions;

namespace UserApi.Validations
{
    /// <summary>Simple validator to check email formats</summary>
    public static partial class EmailValidation
    {
        [GeneratedRegex(@"^([!#-'*/-9=?A-Z^-~-]+(\.[!#-'*/-9=?A-Z^-~-]+)*)@([!#-'*/-9=?A-Z^-~-]+(\.[!#-'*/-9=?A-Z^-~-]+)+)$")]
        private static partial Regex EmailRegex();

        /// <summary>
        /// Test if the given string is specified and a valid email address
        /// </summary>
        /// <remarks>
        /// This was recommended one of the simplest way to manage email validation.
        /// </remarks>
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var match = EmailRegex().Match(email);
            return match.Success;
        }
    }
}
