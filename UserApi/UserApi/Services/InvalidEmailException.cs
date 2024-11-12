using System;

namespace UserApi.Services
{
    public class InvalidEmailException(string message, string email) : Exception(message)
    {
        public string Email { get; } = email;
    }
}
