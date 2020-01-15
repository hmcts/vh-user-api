using System;

namespace UserApi.Services
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class IdentityServiceApiException : Exception
    {
        public IdentityServiceApiException(string message): base(message) {}
    }
}