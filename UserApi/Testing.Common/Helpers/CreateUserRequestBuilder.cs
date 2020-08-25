using Faker;
using UserApi.Contract.Requests;

namespace Testing.Common.Helpers
{
    public class CreateUserRequestBuilder
    {
        private readonly CreateUserRequest _request;

        public CreateUserRequestBuilder()
        {
            _request = new CreateUserRequest()
            {
                FirstName = $"Automation_{Name.First()}",
                LastName = $"Automation_{Name.Last()}",
                IsTestUser = false
            };
            _request.RecoveryEmail = Internet.Email($"{_request.FirstName} {_request.LastName}");
        }

        public CreateUserRequestBuilder WithFirstname(string firstname)
        {
            _request.FirstName = firstname;
            return this;
        }

        public CreateUserRequestBuilder WithLastname(string lastname)
        {
            _request.LastName = lastname;
            return this;
        }

        public CreateUserRequestBuilder WithRecoveryEmail(string recoveryEmail)
        {
            _request.RecoveryEmail = recoveryEmail;
            return this;
        }

        public CreateUserRequestBuilder IsTestUser()
        {
            _request.IsTestUser = true;
            return this;
        }

        public CreateUserRequest Build()
        {
            return _request;
        }
    }
}
