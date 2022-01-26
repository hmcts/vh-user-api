using System;
using System.Runtime.Serialization;

namespace UserApi.Services
{
    [Serializable]
    public class ForbiddenRequestToRemoveUserException : Exception
    {
        protected ForbiddenRequestToRemoveUserException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public ForbiddenRequestToRemoveUserException(string message, string username) : base(
           message)
        {
            Username = username;
        }

        public string Username { get; }

    }
}
