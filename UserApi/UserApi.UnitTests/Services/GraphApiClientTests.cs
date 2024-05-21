using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Testing.Common.Helpers;
using UserApi.Helper;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class GraphApiClientTests
    {
        private Mock<IGraphApiSettings> _graphApiSettings;
        private Mock<ISecureHttpRequest> _secureHttpRequest;
        private Mock<IPasswordService> _passwordService;
        private GraphApiClient _client;
        private string _baseUrl;
        private string _queryUrl;
        private string _defaultPassword;
        private const string UserName = "bob";

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();
            _graphApiSettings = new Mock<IGraphApiSettings>();
            _passwordService = new Mock<IPasswordService>();
            var settings = new Settings() { DefaultPassword = "TestPwd" };
            _defaultPassword = settings.DefaultPassword;
            _baseUrl = $"{_graphApiSettings.Object.GraphApiBaseUri}/v1.0/{_graphApiSettings.Object.TenantId}";
            _queryUrl = $"{_baseUrl}/users";
            _client = new GraphApiClient(_secureHttpRequest.Object, _graphApiSettings.Object, _passwordService.Object, settings);
        }

        public async Task Should_create_user_successfully_with_sspr_and_return_NewAdUserAccount()
        {
            var settings = new Settings() { DefaultPassword = "TestPwd" };
            var client = new GraphApiClient(_secureHttpRequest.Object, _graphApiSettings.Object, _passwordService.Object, settings);
            
            var periodRegexString = "^\\.|\\.$";
            var username = ".TestTester.";
            var firstName = ".Test.";
            var lastName = "Tester";
            var recoveryEmail = "test'tester@hmcts.net";
            var displayName = $"{firstName} {lastName}";
            var user = new
            {
                displayName,
                givenName = firstName,
                surname = lastName,
                mailNickname = $"{Regex.Replace(firstName, periodRegexString, string.Empty)}.{Regex.Replace(lastName, periodRegexString, string.Empty)}"
                    .ToLower(),
                mail = recoveryEmail,
                otherMails = new List<string> { recoveryEmail },
                accountEnabled = true,
                userPrincipalName = username,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = _defaultPassword
                },
                userType = "Guest"
            };

            var json = JsonConvert.SerializeObject(user);

            _secureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(),It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(new Microsoft.Graph.User(), HttpStatusCode.OK));
            _passwordService.Setup(x => x.GenerateRandomPasswordWithDefaultComplexity()).Returns(_defaultPassword);

            var response = await client.CreateUserAsync(username, firstName, lastName, displayName, recoveryEmail);

            response.Should().NotBeNull();
            response.OneTimePassword.Should().Be(_defaultPassword);
            _secureHttpRequest.Verify(x => x.PostAsync(_graphApiSettings.Object.AccessToken, It.Is<StringContent>(s => s.ReadAsStringAsync().Result == json), _queryUrl), Times.Once);
        }

        [Test]
        public async Task Should_return_user_names_starting_with_text()
        {
            var text = "test'user";
            var filter = $"startswith(userPrincipalName,'{text.Replace("'", "''")}')";
            _queryUrl += $"?$filter={filter}";

            var user = new Microsoft.Graph.User() { UserPrincipalName = "TestUser" };
            var azureAdGraphQueryResponse = new GraphQueryResponse<Microsoft.Graph.User>() { Value = new List<Microsoft.Graph.User> { user } };
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponse, HttpStatusCode.OK));

            var response = (await _client.GetUsernamesStartingWithAsync(text)).ToList();

            response.Should().NotBeNull();
            var users = response.ToList();
            users.Count.Should().Be(1);
            users[0].Should().Be("TestUser");
            _secureHttpRequest.Verify(x => x.GetAsync(_graphApiSettings.Object.AccessToken, _queryUrl), Times.Once);
        }
        
        [Test]
        public async Task GetUsernamesStartingWithAsync_WithContactEmail_ReturnsExpectedUsernames()
        {
            // Arrange
            // original call
            var usernameBase = "test.user";
            var contactEmail = "test@test.com";
            var expectedUsernames = new List<string> { "test.user@domain.com", "test.user1@domain.com", "test.user2@domain.com" };
            var user1 = new Microsoft.Graph.User { UserPrincipalName = $"{Guid.NewGuid()}test.user1@domain.com", Mail = contactEmail};
            var user2 = new Microsoft.Graph.User { UserPrincipalName = $"{Guid.NewGuid()}test.user2@domain.com", Mail = contactEmail};
            var azureAdGraphQueryContactMailResponse = new GraphQueryResponse<Microsoft.Graph.User>() { Value = new List<Microsoft.Graph.User> { user1, user2 } };
            
            var user = new Microsoft.Graph.User { UserPrincipalName = "test.user@domain.com" };
            var azureAdGraphQueryResponseOriginal = new GraphQueryResponse<Microsoft.Graph.User>() { Value = new List<Microsoft.Graph.User> { user } };
            
            _secureHttpRequest.SetupSequence(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponseOriginal, HttpStatusCode.OK))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryContactMailResponse, HttpStatusCode.OK));

            // Act
            var result = await _client.GetUsernamesStartingWithAsync(usernameBase, contactEmail);

            // Assert
            ClassicAssert.AreEqual(expectedUsernames, result.ToList());
        }
        
        [Test]
        public async Task GetUsernamesStartingWithAsync_WithFirstNameAndLastName_ReturnsExpectedUsernames()
        {
            // Arrange
            // original call
            var usernameBase = "test.user";
            var firstName = "Test";
            var lastName = "User";
            var contactEmail = "test@test.com";
            var expectedUsernames = new List<string> { "test.user@domain.com", "test.user1@domain.com", "test.user2@domain.com" };
            
            var user = new Microsoft.Graph.User { UserPrincipalName = "test.user@domain.com" };
            var azureAdGraphQueryResponseOriginal = new GraphQueryResponse<Microsoft.Graph.User>() { Value = new List<Microsoft.Graph.User> { user } };
            
            
            var user1 = new Microsoft.Graph.User
            {
                GivenName = firstName,
                Surname = lastName,
                DisplayName = $"{firstName} {lastName}", 
                UserPrincipalName = $"{Guid.NewGuid()}test.user1@domain.com",
                Mail = contactEmail,
                Id = Guid.NewGuid().ToString()
            };
            var user2 = new Microsoft.Graph.User
            {
                GivenName = firstName,
                Surname = lastName,
                DisplayName = $"{firstName} {lastName}",
                UserPrincipalName = $"{Guid.NewGuid()}test.user2@domain.com",
                Mail = contactEmail,
                Id = Guid.NewGuid().ToString()
            };
            var azureAdGraphQueryContactMailResponse = new GraphQueryResponse<Microsoft.Graph.User>()
            {
                Value = new List<Microsoft.Graph.User> { user1, user2 }
            };
            
            _secureHttpRequest.SetupSequence(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponseOriginal, HttpStatusCode.OK))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryContactMailResponse, HttpStatusCode.OK));
            
            // Act
            var result = await _client.GetUsernamesStartingWithAsync(usernameBase, null, firstName, lastName);

            // Assert
            ClassicAssert.AreEqual(expectedUsernames, result.ToList());
        }
     

        /// <summary>
        /// Since the <see cref="ISecureHttpRequest"/> does not capture invalid response codes we need to raise this.
        /// Going forwards this test ought to be removed as the API should raise generic http response exceptions on failure.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void Should_raise_exception_on_unsuccessful_response()
        {
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test", HttpStatusCode.Unauthorized));

            Assert.ThrowsAsync<IdentityServiceApiException>(() => _client.GetUsernamesStartingWithAsync(UserName));
        }
        
        [Test]
        public void Should_raise_IdentityServiceApiException_on_unsuccessful_response_on_delete()
        {            
            _secureHttpRequest.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test", HttpStatusCode.Unauthorized));

           var exception = Assert.ThrowsAsync<IdentityServiceApiException>(async () => await _client.DeleteUserAsync(UserName));

            exception.Should().NotBeNull();
            exception!.Message.Should().Be("Failed to call API: Unauthorized\r\ntest");
        }
        
        [Test]
        public void Should_be_successful_response_on_delete()
        {
            _queryUrl += $"/{UserName}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
            
            _secureHttpRequest.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            Assert.DoesNotThrowAsync(() => _client.DeleteUserAsync(UserName));
            _secureHttpRequest.Verify(x => x.DeleteAsync(_graphApiSettings.Object.AccessToken, _queryUrl), Times.Once);
        }

        [Test]
        public void Should_be_successful_response_on_update()
        {
            var user = new
            {
                userPrincipalName = UserName,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = _defaultPassword
                }
            };

             _queryUrl += $"/{UserName}";
            var json = JsonConvert.SerializeObject(user);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            _passwordService.Setup(x => x.GenerateRandomPasswordWithDefaultComplexity()).Returns("TestPwd");

            Assert.DoesNotThrowAsync(() => _client.UpdateUserPasswordAsync(UserName));

            _secureHttpRequest.Verify(x => x.PatchAsync(_graphApiSettings.Object.AccessToken, It.Is<StringContent>(s => s.ReadAsStringAsync().Result == json), _queryUrl), Times.Once);
        }

        [Test]
        public async Task Should_be_successful_response_with_new_password_on_update()
        {
            const string password = "password";
            var user = new
            {
                userPrincipalName = UserName,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = password
                }
            };

            _queryUrl += $"/{UserName}";
            var json = JsonConvert.SerializeObject(user);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            _passwordService.Setup(x => x.GenerateRandomPasswordWithDefaultComplexity()).Returns(password);

            var result = await _client.UpdateUserPasswordAsync(UserName);

            result.Should().Be(password);

            _secureHttpRequest.Verify(x => x.PatchAsync(_graphApiSettings.Object.AccessToken, It.Is<StringContent>(s => s.ReadAsStringAsync().Result == json), _queryUrl), Times.Once);
        }

        [Test]
        public void Should_raise_IdentityServiceApiException_on_unsuccessful_response_on_update()
        {
            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test",HttpStatusCode.BadRequest));

            _passwordService.Setup(x => x.GenerateRandomPasswordWithDefaultComplexity()).Returns("TestPwd");

            Assert.ThrowsAsync<IdentityServiceApiException>(() => _client.UpdateUserPasswordAsync(UserName));
        }

        [Test]
        public void Should_get_object_conflict_code_when_user_exists_for_mail()
        {
            var settings = new Settings() { DefaultPassword = "TestPwd" };
            var client = new GraphApiClient(_secureHttpRequest.Object, _graphApiSettings.Object, _passwordService.Object, settings);

            var username = ".TestTester.";
            var firstName = ".Test.";
            var lastName = "Tester";
            var recoveryEmail = "test'tester@hmcts.net";
            var displayName = $"{firstName} {lastName}";
            

            _secureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("ObjectConflict", HttpStatusCode.BadRequest));
            _passwordService.Setup(x => x.GenerateRandomPasswordWithDefaultComplexity()).Returns(_defaultPassword);

            Assert.ThrowsAsync<UserExistsException>(() => client.CreateUserAsync(username, firstName, lastName, displayName, recoveryEmail));
        }

        [TestCase("contactEmail@email.com")]
        [TestCase("")]
        public async Task Should_update_user_account(string contactEmail)
        {
            var userId = Guid.NewGuid().ToString();
            var firstName = "FirstName";
            var lastName = "LastName";
            var newUsername = "username@email.com";
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            var result = await _client.UpdateUserAccount(userId,
                firstName, lastName, newUsername,
                contactEmail: contactEmail);

            result.Id.Should().Be(userId);
            result.GivenName.Should().Be(firstName);
            result.Surname.Should().Be(lastName);
            result.DisplayName.Should().Be($"{firstName} {lastName}");
            result.UserPrincipalName.Should().Be(newUsername);
            if (!string.IsNullOrEmpty(contactEmail))
            {
                result.Mail.Should().Be(contactEmail);
                result.OtherMails!.Count().Should().Be(1);
                result.OtherMails.Should().Contain(contactEmail);
            }
            else
            {
                result.Mail.Should().BeNull();
                result.OtherMails.Should().BeNull();
            }
        }
    }
}