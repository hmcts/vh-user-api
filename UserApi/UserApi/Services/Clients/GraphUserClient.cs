using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UserApi.Common;
using UserApi.Services.Interfaces;

namespace UserApi.Services.Clients;

/// <summary>
/// This class is responsible for interfacing with Microsoft Graph API to manage users and groups.
/// Should only be consumed via the Service layer (UserAccountService).
/// </summary>
/// <param name="client">GraphServiceClient</param>
/// <param name="aadConfig">AzureAdConfiguration</param>
[ExcludeFromCodeCoverage]
public class GraphUserClient(GraphServiceClient client, AzureAdConfiguration aadConfig) : IGraphUserClient
{
    private readonly string[] UserSelectArray =
        ["id", "displayName", "userPrincipalName", "givenName", "surname", "otherMails", "mobilePhone"];
    public async Task<User> CreateUserAsync(User user)
    {
        return await client.Users.PostAsync(user);
    }

    public async Task<User> UpdateUserAsync(string userId, User user)
    {
        return await client.Users[userId].PatchAsync(user);
    }

    public async Task DeleteUserAsync(string userPrincipalName)
    {
        await client.Users[userPrincipalName].DeleteAsync();
    }
    
    public async Task<User> GetUserAsync(string identifier)
    {
        return await client.Users[identifier].GetAsync(config =>
        {
            config.QueryParameters.Select = UserSelectArray;
        });
    }
    
    public async Task<List<User>> GetUsersAsync(string filter, CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        var page = await client.Users.GetAsync(config =>
        {
            config.QueryParameters.Filter = filter;
            config.QueryParameters.Top = 999;
            config.QueryParameters.Select = UserSelectArray;
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

    /// <summary>
    /// Retrieves users from a specific Azure AD group using a raw request for performance reasons.
    ///
    /// This implementation intentionally bypasses the standard Graph SDK navigation methods (e.g. client.Groups[groupId].Members)
    /// because it returns a polymorphic collection, that can't use a filter, and the path is capped 100 results per page, regardless of the $top parameter,
    /// which leads to high request volume and worse performance when groups contain thousands of users.
    ///
    /// By using a raw query to the `members/microsoft.graph.user` endpoint and manually handling pagination with $top=999,
    /// The request is still authenticated and executed using the Graph SDK's RequestAdapter for consistency with the rest of the system.
    /// If Microsoft Graph adds support for $top > 100 on the standard SDK navigation builders in the future, this may be revisited, along with using caches and delta queries
    /// for a much more efficient solution.
    /// </summary>
    public async Task<List<User>> GetUsersInGroupAsync(string groupId, string filter = null, CancellationToken cancellationToken = default)
    {
        var users = new List<User>();

        var accessUri = $"{aadConfig.GraphApiBaseUri}/groups/{groupId}/members/microsoft.graph.user" +
                               (filter != null ? $"?$filter={filter}" : string.Empty) +
                               "&$count=true" +
                               "&$select=id,otherMails,userPrincipalName,displayName,givenName,surname" +
                               "&$top=999";

        while (!string.IsNullOrEmpty(accessUri))
        {
            var requestInfo = new RequestInformation { HttpMethod = Method.GET, URI = new Uri(accessUri) };

            requestInfo.Headers.Add("ConsistencyLevel", "eventual");

            var stream = await client.RequestAdapter.SendPrimitiveAsync<Stream>(
                requestInfo,
                errorMapping: null,
                cancellationToken);

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken);

            var jsonObject = JsonConvert.DeserializeObject<JObject>(json);
            var rawUsers = JsonConvert.DeserializeObject<List<User>>(jsonObject["value"]?.ToString() ?? "[]");
            users.AddRange(rawUsers);

            accessUri = jsonObject.TryGetValue("@odata.nextLink", out var nextLink)
                ? nextLink.ToString()
                : null;
        }

        return users;
    }

    public async Task<List<string>> GetDeletedUsernamesAsync(string filter)
    {
        var deletedUsers = new List<string>();

        var page = await client.Directory.DeletedItems.GraphUser.GetAsync(config =>
        {
            config.QueryParameters.Filter = filter;
            config.QueryParameters.Select = ["userPrincipalName"];
        });

        while (page != null)
        {
            if (page.Value != null)
                deletedUsers.AddRange(page.Value
                    .Where(u => !string.IsNullOrWhiteSpace(u.UserPrincipalName))
                    .Select(u => u.UserPrincipalName));

            if (string.IsNullOrEmpty(page.OdataNextLink))
                break;

            page = await client.RequestAdapter.SendAsync(new RequestInformation {
                    HttpMethod = Method.GET,
                    URI = new Uri(page.OdataNextLink)
                },
                UserCollectionResponse.CreateFromDiscriminatorValue);
        }
        return deletedUsers;
    }

    public async Task<List<Group>> GetGroupsForUserAsync(string userId)
    {
        var groups = new List<Group>();
        var response = await client.Users[userId].MemberOf.GetAsync(config =>
        {
            config.QueryParameters.Select = ["id", "displayName"];
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

    public async Task<Group> GetGroupByNameAsync(string displayName)
    {
        var response = await client.Groups.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"displayName eq '{displayName}'";
            config.QueryParameters.Select = ["id", "displayName"];
        });

        return response?.Value?.FirstOrDefault();
    }

    public async Task<Group> GetGroupByIdAsync(string groupId)
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
            OdataId = $"{aadConfig.GraphApiBaseUri}directoryObjects/{userId}"
        };
        await client.Groups[groupId].Members.Ref.PostAsync(reference);
    }
}
