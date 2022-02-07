using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class IsUserAdminAsyncTests : UserAccountServiceTests
    {
        [TestCase("ADMIN_ROLE_ID", true)]
        [TestCase("OTHER_ROLE_ID", false)]
        public async Task Checks_if_user_is_administrator(string userRoleId,  bool expectedResult)
        {
            const string principalId = "55c07278-7109-4a46-ae60-4b644bc83a31";

            var endpoint1 = $"{GraphApiSettings.GraphApiBaseUri}beta/rolemanagement/directory/roleAssignments?$filter=principalId eq '{principalId}'";

            var endpoint2 = $"{GraphApiSettings.GraphApiBaseUri}beta/roleManagement/directory/roleDefinitions?$filter=DisplayName eq 'User Administrator'";

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

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, endpoint1))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(assignedUserRole, HttpStatusCode.OK));

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, endpoint2))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(userAdminRole, HttpStatusCode.OK));

            var response = await Service.IsUserAdminAsync(principalId);

            response.Should().Be(expectedResult);
        }
    }
}
