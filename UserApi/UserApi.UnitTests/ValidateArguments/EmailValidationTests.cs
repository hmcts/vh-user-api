using NUnit.Framework;
using System;
using UserApi.Validations;
using FluentAssertions;

namespace UserApi.UnitTests.ValidateArguments
{
    public class EmailValidationTests
    {
        [TestCase("100@hmcts.net")]
        [TestCase("josé.köln@email.com")]
        [TestCase("Áá@créâtïvéàççénts.com")]
        [TestCase("用户@例子.公司")]
        public void Should_pass_validation_with_good_email(string email)
        {
            email.IsValidEmail().Should().BeTrue();
        }

        [Test]
        public void Should_fail_validation_when_empty()
        {
            var email = string.Empty;
            email.IsValidEmail().Should().BeFalse();
        }

        [Test]
        public void Should_fail_validation_when_format_is_invalid()
        {
            const string firstName = "Automatically";
            const string lastName = "Created";
            var unique = DateTime.Now.ToString("yyyyMMddhmmss");
            var email = $"{firstName}.{lastName}.{unique}.@hearings.reform.hmcts.net";

            email.IsValidEmail().Should().BeFalse();
        }
    }
}
