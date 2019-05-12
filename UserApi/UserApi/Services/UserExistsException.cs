using System;

namespace UserApi.Services
{
    public class UserExistsException : Exception
    {
        public UserExistsException(string message, string username) : base(message)
        {
            Username = username;
        }

        public string Username { get; set; }
    }
}