﻿using NUnit.Framework;
using System;
using UserApi.Validations;
using FluentAssertions;
using Bogus;

namespace UserApi.UnitTests.ValidateArguments
{
    public class EmailValidationTests
    {
        private static readonly Faker Faker = new();
        [Test]
        public void Should_pass_validation_with_good_email()
        {
            var email = $"{Faker.Random.Number(0, 9999999)}@hmcts.net";
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
