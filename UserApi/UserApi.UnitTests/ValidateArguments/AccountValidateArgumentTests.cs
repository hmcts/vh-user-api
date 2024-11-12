using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;
using UserApi.Contract.Requests;
using UserApi.Validations;

namespace UserApi.UnitTests.ValidateArguments
{
    public class AccountValidateArgumentTests
    {
        private AddUserToGroupRequestValidation _validator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _validator = new AddUserToGroupRequestValidation();
        }

        [Test]
        public async Task Should_pass_validation()
        {
            var request = BuildRequest();

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task Should_return_missing_userId_error()
        {
            var request = BuildRequest();
            request.UserId = null;
            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().Be(1);
            result.Errors.Exists(x => x.ErrorMessage == AddUserToGroupRequestValidation.MissingUserIdErrorMessage)
                .Should().BeTrue();
        }

        private static AddUserToGroupRequest BuildRequest()
        {
            return Builder<AddUserToGroupRequest>.CreateNew()
                .With(x => x.GroupName = "TestGroup")
                .With(x => x.UserId = "johndoe")
                .Build();
        }
    }
}
