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

        protected Mock<ISecureHttpRequest> SecureHttpRequest;
        protected GraphApiSettings GraphApiSettings;
        protected Mock<IIdentityServiceApiClient> IdentityServiceApiClient;
        protected UserApi.Services.UserAccountService Service;
        protected AzureAdGraphUserResponse AzureAdGraphUserResponse;
        protected AzureAdGraphQueryResponse<AzureAdGraphUserResponse> AzureAdGraphQueryResponse;
        
        private Settings _settings;
       
        protected string Filter;

        [SetUp]
        public void Setup()
        {
            SecureHttpRequest = new Mock<ISecureHttpRequest>();

            _settings = new Settings() { IsLive = true, ReformEmail = Domain.Replace("@","") };

            var azureAdConfig = new AzureAdConfiguration() { 
                                        ClientId = "TestClientId", 
                                        ClientSecret = "TestSecret", 
                                        Authority = "https://Test/Authority"
                                        };

            var tokenProvider = new Mock<ITokenProvider>();
            GraphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfig);
            IdentityServiceApiClient = new Mock<IIdentityServiceApiClient>();

            AzureAdGraphUserResponse = new AzureAdGraphUserResponse() { 
                                            ObjectId = "1", 
                                            DisplayName = "T Tester", 
                                            GivenName ="Test", 
                                            Surname = "Tester", 
                                            OtherMails = new List<string>(), 
                                            UserPrincipalName = "TestUser" 
                                        };
            AzureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse> { Value = new List<AzureAdGraphUserResponse> { AzureAdGraphUserResponse } };
            

            Service = new UserApi.Services.UserAccountService(SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings);
        }

        protected string AccessUri => $"{GraphApiSettings.GraphApiUriWindows}{GraphApiSettings.TenantId}/users?$filter={Filter}&api-version=1.6";

        [Test]
        public async Task Should_increment_the_username()
        {
            const string firstName = "Existing";
            const string lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();

            // given api returns
            var existingUsers = new[] {"existing.user", "existing.user1"};
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName);
            
            nextAvailable.Should().Be("existing.user2" + Domain);
            IdentityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        } 

        [Test]
        public async Task Should_delete()
        {
            IdentityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await Service.DeleteUserAsync("User");
            
            IdentityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_update_user_data_in_aad()
        {
            IdentityServiceApiClient.Setup(x => x.UpdateUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask); 

            await Service.UpdateUserAsync("known.user@gmail.com");

            IdentityServiceApiClient.Verify(i => i.UpdateUserAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
