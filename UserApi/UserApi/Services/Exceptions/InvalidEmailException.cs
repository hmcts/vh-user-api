using System;

namespace UserApi.Services.Exceptions
{
    public class InvalidEmailException(string message, string email) : Exception(message)
    {
        public string Email { get; } = email;
    }
}
