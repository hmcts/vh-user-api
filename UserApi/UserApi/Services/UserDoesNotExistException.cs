using System;
using System.Runtime.Serialization;

namespace UserApi.Services
{
    [Serializable]
    public class UserDoesNotExistException : Exception
    {
        protected UserDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}

        public UserDoesNotExistException(string username) : base(
            $"Unable to delete user '{username}' since user does not exist")
        {
            Username = username;
        }
        
        public UserDoesNotExistException(Guid userId) : base(
            $"User '{userId}' does not exist")
        {
            UserId = userId;
        }

        public string Username { get; }
        public Guid UserId { get; }
    }
}