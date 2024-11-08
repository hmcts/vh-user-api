using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using UserApi.Contract.Requests;
using UserApi.Validations;

namespace UserApi.UnitTests.ValidateArguments
{
    public class UserValidateArgumentTests
    {
        private CreateUserRequestValidation _validator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _validator = new CreateUserRequestValidation();
        }

        [Test]
        public async Task Should_pass_validation()
        {
            var request = BuildRequest();

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task Should_return_email_error()
        {
            var request = BuildRequest();
            request.RecoveryEmail = "badEmail.@email.com";
            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().Be(1);
            result.Errors.Any(x => x.ErrorMessage == CreateUserRequestValidation.InvalidEmailErrorMessage)
                .Should().BeTrue();
        }

        private static CreateUserRequest BuildRequest()
        {
            return Builder<CreateUserRequest>.CreateNew()
                .With(x => x.FirstName = "John")
                .With(x => x.LastName = "doe")
                .With(x => x.RecoveryEmail = "john.doe@hmcts.net")
                .Build();
        }
    }
}
