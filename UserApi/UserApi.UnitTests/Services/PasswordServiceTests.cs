using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using UserApi.Services;

namespace UserApi.UnitTests.Services
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _passwordService;

        public PasswordServiceTests()
        {
            _passwordService = new PasswordService();
        }

        [Test]
        public void Should_generate_random_password_for_one_hundred_thousand_iterations()
        {
            var uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ".ToCharArray();
            var lowercase = "abcdefghijkmnopqrstuvwxyz".ToCharArray();
            var digits = "0123456789".ToCharArray();
            var nonAlphaNumeric = "!@$?_-".ToCharArray();

            foreach (var index in Enumerable.Range(1, 100000))
            {
                var result = _passwordService.GenerateRandomPasswordWithDefaultComplexity();

                result.Should().NotBeNullOrWhiteSpace();
                result.Length.Should().Be(12);
                Assert.True(result.Any(x => uppercase.Contains(x)), $"Did not contain uppercase: Result: {result}");
                Assert.True(result.Any(x => lowercase.Contains(x)), $"Did not contain lowercase: Result: {result}");
                Assert.True(result.Any(x => digits.Contains(x)), $"Did not contain digits: Result: {result}");
                Assert.True(result.Any(x => nonAlphaNumeric.Contains(x)), $"Did not contain nonAlphaNumeric: Result: {result}"); 
            }
        }
    }
}