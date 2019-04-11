﻿using System;
using System.Linq;
using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using UserApi.Security.CustomToken;

namespace UserApi.UnitTests.CustomJwtToken
{
    public class CustomJwtTokenProviderTests
    {
        private ICustomJwtTokenProvider _customJwtTokenProvider;
        private ICustomJwtTokenHandler _customJwtTokenHandler;

        [SetUp]
        public void Setup()
        {
            //Convert.ToBase64String(new HMACSHA256().Key); to generate a new key
            var secretKey = "W2gEmBn2H7b2FCMIQl6l9rggbJU1qR7luIeAf1uuaY+ik6TP5rN0NEsPVg0TGkroiel0SoCQT7w3cbk7hFrBtA==";
            var customJwtTokenConfigSettings = new CustomJwtTokenConfigSettings(1, secretKey, "test.video.enpoint");
            _customJwtTokenProvider = new CustomJwtTokenProvider(customJwtTokenConfigSettings);
            _customJwtTokenHandler = new CustomJwtTokenHandler(customJwtTokenConfigSettings);
        }

        [Test]
        public void should_generate_jwt_token_when_generate_token_is_called()
        {
            var generateToken = _customJwtTokenProvider.GenerateToken("Test User", 30);
            generateToken.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void should_be_valid_token_when_generated_by_custom_token_provider()
        {
            var token = _customJwtTokenProvider.GenerateToken("Test User", 30);

            var isValidToken = _customJwtTokenHandler.IsValidToken(token);
            isValidToken.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_token_when_token_is_random_string()
        {
            var token = "ey1221213121";
            var isValidToken = _customJwtTokenHandler.IsValidToken(token);

            isValidToken.Should().BeFalse();
        }

        [Test]
        public void should_get_principal_when_get_principal_is_called_with_valid_tokne()
        {
            var testUser = "Test User";
            var token = _customJwtTokenProvider.GenerateToken(testUser, 30);

            var claimsPrincipal = _customJwtTokenHandler.GetPrincipal(token);
            claimsPrincipal.Claims.First().Value.Should().Be(testUser);
        }

        [Test]
        public void should_not_be_able_to_get_principal_when_get_principal_is_called_with_invalid_token()
        {
            var token = "ey1221213121";

            var claimsPrincipal = _customJwtTokenHandler.GetPrincipal(token);
            claimsPrincipal.Should().BeNull();
        }

        [Test]
        public void should_be_invalid_token_when_token_generated_with_different_secret()
        {
            var secretKey = "F8pf/zwOgm/kASEFs+BKRDdyq+RhHCQ9i9tPjeaPjUebm6HvzXKIsr/nX28wpwAZoWRG0FQK9LVf6nrkW/vg4w==";
            var customJwtTokenConfigSettings = new CustomJwtTokenConfigSettings(1, secretKey, "test.video.enpoint");
            _customJwtTokenProvider = new CustomJwtTokenProvider(customJwtTokenConfigSettings);

            var token = "ey1221213121";

            var claimsPrincipal = _customJwtTokenHandler.IsValidToken(token);
            claimsPrincipal.Should().BeFalse();
        }
    }
}