using System;

namespace UserApi.Security
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class UserServiceException : Exception
    {
        public UserServiceException(string message, string reason) : base($"{message}: {reason}")
        {
            Reason = reason;
        }

        public string Reason { get; set; }

    }
}