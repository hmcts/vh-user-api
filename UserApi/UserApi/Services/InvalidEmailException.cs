using System;
using System.Runtime.Serialization;

namespace UserApi.Services
{
    [Serializable]
    public class InvalidEmailException : Exception
    {
        protected InvalidEmailException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public InvalidEmailException(string message, string email) : base(message)
        {
            Email = email;
        }

        public string Email { get; }
    }
}
