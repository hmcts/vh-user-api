using System;

namespace UserApi.Services.Exceptions
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class IdentityServiceApiException : Exception
    {
        public IdentityServiceApiException(string message): base(message) {}
    }
}