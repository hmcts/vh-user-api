using System;

namespace UserApi.Security
{
    public class UserServiceException : Exception
    {
        public string Reason { get; set; }
        public UserServiceException(string message, string reason) : base($"{message}: {reason}")
        {
            Reason = reason;
        }
    }
}