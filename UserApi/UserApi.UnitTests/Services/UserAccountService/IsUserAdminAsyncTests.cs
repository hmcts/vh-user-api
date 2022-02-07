using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class IsUserAdminAsyncTests : UserAccountServiceTests
    {
        private const string PrincipalId = "55c07278-7109-4a46-ae60-4b644bc83a31";
        private string UserRoleEndpoint => $"{GraphApiSettings.GraphApiBaseUri}beta/roleManagement/directory/roleAssignments?$filter=principalId eq '{PrincipalId}'";
        private string UserAdminRoleEndpoint => $"{GraphApiSettings.GraphApiBaseUri}beta/roleManagement/directory/roleDefinitions?$filter=DisplayName eq 'User Administrator'";


        [TestCase("ADMIN_ROLE_ID", true)]
        [TestCase("OTHER_ROLE_ID", false)]
        public async Task Checks_if_user_is_administrator(string userRoleId, bool expectedResult)
        {
            var assignedUserRole = new AzureAdGraphQueryResponse<UserAssignedRole>
            {
                Context = string.Empty,
                Value = new List<UserAssignedRole> { new UserAssignedRole { RoleDefinitionId = userRoleId } }
            };

            var userAdminRole = new AzureAdGraphQueryResponse<RoleDefinition>
            {
                Context = string.Empty,
                Value = new List<RoleDefinition> { new RoleDefinition { Id = "ADMIN_ROLE_ID" } }
            };

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(assignedUserRole, HttpStatusCode.OK));

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserAdminRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(userAdminRole, HttpStatusCode.OK));

            var response = await Service.IsUserAdminAsync(PrincipalId);

            response.Should().Be(expectedResult);
        }

        [TestCase(HttpStatusCode.OK, HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.NotFound, HttpStatusCode.OK)]
        [TestCase(HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
        public async Task Shoulld_return_negative_result_when_graph_api_returns_no_resource(HttpStatusCode userRoleStatusCode, HttpStatusCode adminRoleStatusCode)
        {
            var assignedUserRole = new AzureAdGraphQueryResponse<UserAssignedRole>
            {
                Context = string.Empty,
                Value = new List<UserAssignedRole>()
            };

            var userAdminRole = new AzureAdGraphQueryResponse<RoleDefinition>
            {
                Context = string.Empty,
                Value = new List<RoleDefinition> { new RoleDefinition { Id = "ADMIN_ROLE_ID" } }
            };

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(assignedUserRole, userRoleStatusCode));

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserAdminRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(userAdminRole, adminRoleStatusCode));

            var response = await Service.IsUserAdminAsync(PrincipalId);

            response.Should().BeFalse();
        }

        [Test]
        public void Should_throw_exception_on_other_responses()
        {
            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(default(AzureAdGraphQueryResponse<UserAssignedRole>), HttpStatusCode.BadRequest));

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, UserAdminRoleEndpoint))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(default(AzureAdGraphQueryResponse<RoleDefinition>), HttpStatusCode.InternalServerError));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.IsUserAdminAsync(PrincipalId));

            response.Should().NotBeNull();
            response.Message.Should().NotBeEmpty();
            response.Reason.Should().NotBeEmpty();
        }
        
    }
}
