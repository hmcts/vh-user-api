using System;
using System.Text.RegularExpressions;

namespace UserApi.Validations
{
    /// <summary>Simple validator to check email formats</summary>
    public static class EmailValidation
    {
        private const string RegexPattern = @"^([!#-'*/-9=?A-Z^-~-]+(\.[!#-'*/-9=?A-Z^-~-]+)*)@([!#-'*/-9=?A-Z^-~-]+(\.[!#-'*/-9=?A-Z^-~-]+)+)+$";

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

            var match = Regex.Match(email, RegexPattern);
            return match.Success;
        }
    }
}
