using System;

namespace UserApi.Services
{
    public class IdentityServiceApiException : Exception
    {
        public IdentityServiceApiException(string message): base(message) {}
    }
}