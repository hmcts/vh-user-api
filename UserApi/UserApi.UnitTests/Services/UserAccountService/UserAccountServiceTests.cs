using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common;
using Testing.Common.Helpers;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class UserAccountServiceTests
    {
        protected const string Domain = "@hearings.reform.hmcts.net";

        protected Mock<ISecureHttpRequest> _secureHttpRequest;
        protected GraphApiSettings _graphApiSettings;
        protected Mock<IIdentityServiceApiClient> _identityServiceApiClient;
        protected UserAccountService _service;
        protected AzureAdGraphUserResponse azureAdGraphUserResponse;
        protected AzureAdGraphQueryResponse<AzureAdGraphUserResponse> azureAdGraphQueryResponse;
        
        private Settings settings;
       
        protected string filter;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();

            settings = TestConfig.Instance.Settings;
            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);
            _graphApiSettings = new GraphApiSettings(tokenProvider, TestConfig.Instance.AzureAd);
            _identityServiceApiClient = new Mock<IIdentityServiceApiClient>();

            azureAdGraphUserResponse = new AzureAdGraphUserResponse() { 
                                            ObjectId = "1", 
                                            DisplayName = "T Tester", 
                                            GivenName ="Test", 
                                            Surname = "Tester", 
                                            OtherMails = new List<string>(), 
                                            UserPrincipalName = "TestUser" 
                                        };
            azureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse> { Value = new List<AzureAdGraphUserResponse> { azureAdGraphUserResponse } };
            

            _service = new UserAccountService(_secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, settings);
        }

        protected string AccessUri
        {
            get
            {
                return $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users?$filter={filter}&api-version=1.6";
            }
        }        

        [Test]
        public async Task should_increment_the_username()
        {
            var firstName = "Existing";
            var lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();

            // given api returns
            var existingUsers = new[] {"existing.user", "existing.user1"};
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync(firstName, lastName);
            
            nextAvailable.Should().Be("existing.user2" + Domain);
            _identityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        } 

        [Test]
        public async Task should_delete()
        {
            _identityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await _service.DeleteUserAsync("User");
            
            _identityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task should_update_user_data_in_aad()
        {
            _identityServiceApiClient.Setup(x => x.UpdateUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await _service.UpdateUserAsync("known.user@gmail.com");

            _identityServiceApiClient.Verify(i => i.UpdateUserAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
