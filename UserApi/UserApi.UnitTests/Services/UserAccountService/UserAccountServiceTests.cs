using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class UserAccountServiceTests
    {
        protected const string Domain = "@hearings.test.server.net";

        protected Mock<ISecureHttpRequest> _secureHttpRequest;
        protected GraphApiSettings _graphApiSettings;
        protected Mock<IIdentityServiceApiClient> _identityServiceApiClient;
        protected UserApi.Services.UserAccountService _service;
        protected AzureAdGraphUserResponse azureAdGraphUserResponse;
        protected AzureAdGraphQueryResponse<AzureAdGraphUserResponse> azureAdGraphQueryResponse;
        
        private Settings settings;
       
        protected string filter;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();

            settings = new Settings() { IsLive = true, ReformEmail = Domain.Replace("@","") };

            var azureAdConfig = new AzureAdConfiguration() { 
                                        ClientId = "TestClientId", 
                                        ClientSecret = "TestSecret", 
                                        Authority = "https://Test/Authority"
                                        };
            var tokenProvider = new Mock<ITokenProvider>();
            _graphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfig);
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
            

            _service = new UserApi.Services.UserAccountService(_secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, settings);
        }

        protected string AccessUri => $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users?$filter={filter}&api-version=1.6";

        [Test]
        public async Task Should_increment_the_username()
        {
            const string firstName = "Existing";
            const string lastName = "User";
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
        public async Task Should_delete()
        {
            _identityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await _service.DeleteUserAsync("User");
            
            _identityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_update_user_data_in_aad()
        {
            _identityServiceApiClient.Setup(x => x.UpdateUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await _service.UpdateUserAsync("known.user@gmail.com");

            _identityServiceApiClient.Verify(i => i.UpdateUserAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
