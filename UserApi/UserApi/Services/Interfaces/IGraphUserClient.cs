using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph.Models;

namespace UserApi.Services.Interfaces;

public interface IGraphUserClient
{
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(string userId, User user);
    Task DeleteUserAsync(string userPrincipalName);
    Task<List<User>> GetUsersAsync(string filter, CancellationToken cancellationToken = default);
    Task<User> GetUserAsync(string identifier);
    Task<List<string>> GetDeletedUsernamesAsync(string filter);
    Task<List<User>> GetUsersInGroupAsync(string groupId, string? filter = null, CancellationToken cancellationToken = default);
    Task<List<Group>> GetGroupsForUserAsync(string userId);
    Task<Group> GetGroupByNameAsync(string displayName);
    Task<Group> GetGroupByIdAsync(string groupId);
    Task<List<UnifiedRoleAssignment>> GetRoleAssignmentsAsync(string principalId);
    Task<UnifiedRoleDefinition> GetRoleDefinitionAsync(string roleName);
    Task AddUserToGroupAsync(string userId, string groupId);
}
