using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using UserApi.Services.Interfaces;

namespace UserApi.Services.Clients;

/// <summary>
/// This class is responsible for interfacing with Microsoft Graph API to manage users and groups.
/// Should only be consumed via the Service layer (UserAccountService).
/// </summary>
/// <param name="client"></param>
public class GraphUserClient(GraphServiceClient client) : IGraphUserClient
{
    public async Task<User> CreateUserAsync(User user)
    {
        return await client.Users.PostAsync(user);
    }

    public async Task UpdateUserAsync(string userId, User user)
    {
        await client.Users[userId].PatchAsync(user);
    }

    public async Task DeleteUserAsync(string userPrincipalName)
    {
        await client.Users[userPrincipalName].DeleteAsync();
    }

    public async Task<List<User>> GetUsersAsync(string filter, CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        var page = await client.Users.GetAsync(config =>
        {
            config.QueryParameters.Filter = filter;
            config.QueryParameters.Top = 999;
            config.QueryParameters.Select = ["id", "displayName", "userPrincipalName", "givenName", "surname", "otherMails", "mobilePhone"];
        }, cancellationToken);

        while (page != null)
        {
            if (page.Value != null)
                users.AddRange(page.Value);

            if (string.IsNullOrEmpty(page.OdataNextLink))
                break;

            page = await client.RequestAdapter.SendAsync(
                new RequestInformation
                {
                    HttpMethod = Method.GET,
                    URI = new Uri(page.OdataNextLink)
                },
                UserCollectionResponse.CreateFromDiscriminatorValue,
                null,
                cancellationToken);
        }

        return users;
    }

    public async Task<List<string>> GetDeletedUsernamesAsync(string filter)
    {
        var deletedUsers = new List<string>();

        var page = await client.Directory.DeletedItems.GraphUser.GetAsync(config =>
        {
            config.QueryParameters.Filter = filter;
            config.QueryParameters.Select = new[] { "userPrincipalName" };
        });

        while (page != null)
        {
            if (page.Value != null)
                deletedUsers.AddRange(page.Value
                    .Where(u => !string.IsNullOrWhiteSpace(u.UserPrincipalName))
                    .Select(u => u.UserPrincipalName));

            if (string.IsNullOrEmpty(page.OdataNextLink))
                break;

            page = await client.RequestAdapter.SendAsync(
                new RequestInformation
                {
                    HttpMethod = Method.GET,
                    URI = new Uri(page.OdataNextLink)
                },
                UserCollectionResponse.CreateFromDiscriminatorValue);
        }
        return deletedUsers;
    }

    public async Task<List<User>> GetUsersInGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        var nextLink = string.Empty;

        do
        {
            var response = string.IsNullOrEmpty(nextLink)
                ? await client.Groups[groupId].Members.GetAsync(config =>
                {
                    config.QueryParameters.Select = ["id", "displayName", "userPrincipalName", "givenName", "surname", "otherMails"];
                    config.QueryParameters.Count = true;
                    config.QueryParameters.Top = 999;
                }, cancellationToken)
                : await client.RequestAdapter.SendAsync(
                    new RequestInformation
                    {
                        HttpMethod = Method.GET,
                        URI = new Uri(nextLink)
                    },
                    DirectoryObjectCollectionResponse.CreateFromDiscriminatorValue,
                    null,
                    cancellationToken);

            if (response?.Value != null)
                users.AddRange(response.Value.OfType<User>());

            nextLink = response?.OdataNextLink;
        } while (!string.IsNullOrEmpty(nextLink));

        return users;
    }

    public async Task<List<Group>> GetGroupsForUserAsync(string userId)
    {
        var groups = new List<Group>();
        var response = await client.Users[userId].MemberOf.GetAsync(config =>
        {
            config.QueryParameters.Select = ["id", "displayName", "@odata.type"];
        });

        while (response != null)
        {
            if (response.Value != null)
                groups.AddRange(response.Value.OfType<Group>().Where(g => g.OdataType == "#microsoft.graph.group"));

            if (string.IsNullOrEmpty(response.OdataNextLink))
                break;

            response = await client.RequestAdapter.SendAsync(
                new RequestInformation
                {
                    HttpMethod = Method.GET,
                    URI = new Uri(response.OdataNextLink)
                },
                DirectoryObjectCollectionResponse.CreateFromDiscriminatorValue);
        }

        return groups;
    }

    public async Task<Group?> GetGroupByNameAsync(string displayName)
    {
        var response = await client.Groups.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"displayName eq '{displayName}'";
            config.QueryParameters.Select = ["id", "displayName"];
        });

        return response?.Value?.FirstOrDefault();
    }

    public async Task<Group?> GetGroupByIdAsync(string groupId)
    {
        return await client.Groups[groupId].GetAsync(config =>
        {
            config.QueryParameters.Select = ["id", "displayName"];
        });
    }

    public async Task<List<UnifiedRoleAssignment>> GetRoleAssignmentsAsync(string principalId)
    {
        var assignments = await client.RoleManagement.Directory.RoleAssignments.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"principalId eq '{principalId}'";
        });
        return assignments?.Value;
    }
    
    public async Task<UnifiedRoleDefinition> GetRoleDefinitionAsync(string roleName)
    {
       var definitions = await client.RoleManagement.Directory.RoleDefinitions.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"displayName eq '{roleName}'";
        });
        return definitions?.Value?.FirstOrDefault();
    }

    public async Task AddUserToGroupAsync(string userId, string groupId)
    {
        var reference = new ReferenceCreate
        {
            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{userId}"
        };
        await client.Groups[groupId].Members.Ref.PostAsync(reference);
    }
}
