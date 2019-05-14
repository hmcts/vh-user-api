using System;
using System.Runtime.Serialization;

namespace UserApi.Services
{
    [Serializable]
    public class UserExistsException : Exception
    {
        protected UserExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public UserExistsException(string message, string username) : base(message)
        {
            Username = username;
        }

        public string Username { get; }
    }
}