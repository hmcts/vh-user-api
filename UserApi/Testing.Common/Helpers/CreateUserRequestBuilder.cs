using Faker;
using UserApi.Contract.Requests;

namespace Testing.Common.Helpers
{
    public class CreateUserRequestBuilder
    {
        private string _firstname;
        private string _lastname;
        private string _recoveryEmail;

        public CreateUserRequestBuilder()
        {
            _firstname = Name.First();
            _lastname = Name.Last();
            _recoveryEmail = Internet.Email($"{_firstname} {_lastname}");
        }

        public CreateUserRequestBuilder WithFirstname(string firstname)
        {
            _firstname = firstname;
            return this;
        }

        public CreateUserRequestBuilder WithLastname(string lastname)
        {
            _lastname = lastname;
            return this;
        }

        public CreateUserRequestBuilder WithRecoveryEmail(string recoveryEmail)
        {
            _recoveryEmail = recoveryEmail;
            return this;
        }
        public CreateUserRequest Build()
        {
            return new CreateUserRequest()
            {
                RecoveryEmail = $"Automation_{_recoveryEmail}",
                FirstName = $"Automation_{_firstname}",
                LastName = $"Automation_{_lastname}"
            };
        }
    }
}
