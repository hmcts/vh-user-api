using System;

namespace UserApi.Services
{
    public class ForbiddenRequestToRemoveUserException : Exception
    {
        public ForbiddenRequestToRemoveUserException(string message) : base(
           message)
        {
        }

    }
}
