using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using UserApi.Helper;

namespace UserApi.UnitTests.Helpers
{
    public class PasswordHelperTests
    {

        [Test]
        public void Should_generate_random_password_for_one_hundred_thousand_iterations()
        {
            var uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ".ToCharArray();
            var lowercase = "abcdefghijkmnopqrstuvwxyz".ToCharArray();
            var digits = "0123456789".ToCharArray();
            var nonAlphaNumeric = "!@$?_-".ToCharArray();

            foreach (var index in Enumerable.Range(1, 100000))
            {
                var result = PasswordHelper.GenerateRandomPasswordWithDefaultComplexity();

                result.Should().NotBeNullOrWhiteSpace();
                result.Length.Should().Be(12);
                
                result.Any(x => uppercase.Contains(x)).Should().BeTrue("because the result should contain an uppercase letter");
                result.Any(x => lowercase.Contains(x)).Should().BeTrue("because the result should contain a lowercase letter");
                result.Any(x => digits.Contains(x)).Should().BeTrue("because the result should contain a digit");
                result.Any(x => nonAlphaNumeric.Contains(x)).Should().BeTrue("because the result should contain a non-alphanumeric character");
            }
        }
    }
}