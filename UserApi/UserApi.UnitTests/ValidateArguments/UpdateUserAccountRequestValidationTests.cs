using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using UserApi.Contract.Requests;
using UserApi.Validations;

namespace UserApi.UnitTests.ValidateArguments
{
    public class UpdateUserAccountRequestValidationTests
    {
        private UpdateUserAccountRequestValidation _validator;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _validator = new UpdateUserAccountRequestValidation();
        }
        
        [Test]
        public async Task Should_pass_validation()
        {
            var request = BuildRequest();

            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeTrue();
        }
        
        [Test]
        public async Task Should_return_first_name_error()
        {
            var request = BuildRequest();
            request.FirstName = string.Empty;
            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().Be(1);
            result.Errors.Any(x => x.ErrorMessage == UpdateUserAccountRequestValidation.MissingFirstNameErrorMessage)
                .Should().BeTrue();
        }
        
        [Test]
        public async Task Should_return_last_name_error()
        {
            var request = BuildRequest();
            request.LastName = string.Empty;
            var result = await _validator.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Count.Should().Be(1);
            result.Errors.Any(x => x.ErrorMessage == UpdateUserAccountRequestValidation.MissingLastNameErrorMessage)
                .Should().BeTrue();
        }

        private UpdateUserAccountRequest BuildRequest()
        {
            return Builder<UpdateUserAccountRequest>.CreateNew()
                .With(x => x.FirstName = "John")
                .With(x => x.LastName = "Doe")
                .Build();
        }
    }
}