using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using UserApi.Services.Interfaces;

namespace UserApi.Services.Clients;

[ExcludeFromCodeCoverage]
internal class GraphUserClientStub : IGraphUserClient
{
    public Task<User> CreateUserAsync(User user)
    {
        return Task.FromResult(user);
    }

    public Task<User> UpdateUserAsync(string userId, User user)
    {
        return Task.FromResult(user);
    }

    public Task DeleteUserAsync(string userPrincipalName)
    {
        return Task.CompletedTask;
    }

    public Task<List<User>> GetUsersAsync(string filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<User>
        {
            new (){
                Id = "1",
                DisplayName = "Test User",
                GivenName = "Test",
                Surname = "User"
            }
        });
    }

    public Task<User> GetUserAsync(string identifier)
    {
        return Task.FromResult(new User
        {
            Id = "1",
            DisplayName = "Test User",
            GivenName = "Test",
            Surname = "User",
            UserPrincipalName = identifier,
        });
    }

    public Task<List<string>> GetDeletedUsernamesAsync(string filter)
    {
        return Task.FromResult(new List<string>());
    }

    public Task<List<User>> GetUsersInGroupAsync(string groupId, string filter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<User>
        {
            new User { Id = "1", DisplayName = "User 1" },
            new User { Id = "2", DisplayName = "User 2" }
        });
    }
    
    public Task<List<Group>> GetGroupsForUserAsync(string userId)
    {
        return Task.FromResult(new List<Group>
        {
            new Group { Id = "1", DisplayName = "Group 1" },
            new Group { Id = "2", DisplayName = "Group 2" }
        });
    }

    public Task<Group> GetGroupByNameAsync(string displayName)
    {
        return Task.FromResult(new Group { Id = "1", DisplayName = displayName });
    }

    public Task<Group> GetGroupByIdAsync(string groupId)
    {
        return Task.FromResult(new Group { Id = groupId, DisplayName = "Test Group" });
    }

    public Task<List<UnifiedRoleAssignment>> GetRoleAssignmentsAsync(string principalId)
    {
        return Task.FromResult(new List<UnifiedRoleAssignment>
        {
            new() { Id = "1", PrincipalId = principalId, RoleDefinitionId = "adminRole" }
        });
    }

    public Task<UnifiedRoleDefinition> GetRoleDefinitionAsync(string roleName)
    {
        return Task.FromResult(new UnifiedRoleDefinition { Id = "adminRole", DisplayName = roleName });
    }

    public Task AddUserToGroupAsync(string userId, string groupId)
    {
        return Task.CompletedTask;
    }
}