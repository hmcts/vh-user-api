using System;

namespace UserApi.Security
{
    public class UserServiceException : Exception
    {
        public UserServiceException(string message, string reason) : base($"{message}: {reason}")
        {
            Reason = reason;
        }

        public string Reason { get; set; }
    }
}