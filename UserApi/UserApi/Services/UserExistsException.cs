using System;

namespace UserApi.Services
{
    public class UserExistsException(string message, string username) : Exception(message)
    {
        public string Username { get; } = username;
    }
}
