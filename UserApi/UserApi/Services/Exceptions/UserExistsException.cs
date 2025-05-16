using System;

namespace UserApi.Services.Exceptions
{
    public class UserExistsException(string message, string username) : Exception(message)
    {
        public string Username { get; } = username;
    }
}
