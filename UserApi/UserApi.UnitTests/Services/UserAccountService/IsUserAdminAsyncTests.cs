using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService;

public class IsUserAdminAsyncTests : UserAccountServiceTestsBase
{
    private const string PrincipalId = "55c07278-7109-4a46-ae60-4b644bc83a31";

    [TestCase("ADMIN_ROLE_ID", true)]
    [TestCase("OTHER_ROLE_ID", false)]
    public async Task Should_return_correct_admin_status_based_on_role(string userRoleId, bool expectedResult)
    {
        // Arrange
        var assignedRoles = new List<UnifiedRoleAssignment> { new () {RoleDefinitionId = userRoleId } };
        var adminRoleDefinition = new UnifiedRoleDefinition { Id = "ADMIN_ROLE_ID" };
        
        GraphClient.Setup(client => client.GetRoleAssignmentsAsync(It.IsAny<string>()))
            .ReturnsAsync(assignedRoles);

        GraphClient.Setup(client => client.GetRoleDefinitionAsync(It.IsAny<string>()))
            .ReturnsAsync(adminRoleDefinition);
            
        // Act
        var result = await Service.IsUserAdminAsync(PrincipalId);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Test]
    public async Task Should_throw_UserServiceException_on_unexpected_responses()
    {
        // Arrange
        GraphClient.Setup(client => client.GetRoleAssignmentsAsync(It.IsAny<string>()))
            .ThrowsAsync(new ODataError());

        // Act
        // Assert
        Assert.ThrowsAsync<UserServiceException>(async () => await Service.IsUserAdminAsync(PrincipalId));
    }
}