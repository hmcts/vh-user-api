using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;
using System.Text.RegularExpressions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using UserApi.Services.Exceptions;
using UserApi.Services.Interfaces;
using UserApi.Validations;
using Group = Microsoft.Graph.Models.Group;
using UserType = UserApi.Services.Models.UserType;

namespace UserApi.Services;

public partial class UserAccountService(IGraphUserClient client, Settings settings) : IUserAccountService
{
    private const string PerformanceTestUserFirstName = "TP";
    
    [GeneratedRegex(@"^\.|\.$")]
    private static partial Regex PeriodRegex();
    
    public async Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail, bool isTestUser)
    {
        if (!recoveryEmail.IsValidEmail())
        {
            throw new InvalidEmailException("Recovery email is not a valid email", recoveryEmail);
        }

        var recoveryEmailText = recoveryEmail.Replace("'", "''");
        var filter = $"otherMails/any(c:c eq '{recoveryEmailText}')";
        var user = await GetUserByFilterAsync(filter);
        if (user != null)
        {
            // Avoid including the exact email to not leak it to logs
            throw new UserExistsException("User with recovery email already exists", user.UserPrincipalName);
        }

        var username = await CheckForNextAvailableUsernameAsync(firstName, lastName, recoveryEmail);
        var displayName = $"{firstName} {lastName}";

        var newPassword = isTestUser 
            ? settings.TestDefaultPassword 
            : PasswordHelper.GenerateRandomPasswordWithDefaultComplexity();

        var periodRegex = PeriodRegex();
        var newUser = new User
        {
            DisplayName = displayName,
            GivenName = firstName,
            Surname = lastName,
            MailNickname = $"{periodRegex.Replace(firstName, string.Empty)}.{periodRegex.Replace(lastName, string.Empty)}".ToLower(),
            UserPrincipalName = username,
            Mail = recoveryEmail,
            AccountEnabled = true,
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = !isTestUser,
                Password = newPassword
            },
            OtherMails = [recoveryEmail],
            UserType = UserType.Guest
        };

        try
        {
            var createdUser = await client.CreateUserAsync(newUser);
   
            if (createdUser == null)
            {
                throw new UserServiceException("Failed to create the user in Microsoft Graph.", "User creation returned null");
            }
            
            return new NewAdUserAccount
            {
                OneTimePassword = newPassword,
                UserId = createdUser.Id,
                Username = createdUser.UserPrincipalName
            };
        }
        catch (ODataError odataError)
        {
            throw new UserServiceException("Failed to create the user in Microsoft Graph.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException("An unexpected error occurred while creating the user.", ex.Message);
        }
    }
    
    public async Task<User> UpdateUserAccountAsync(Guid userId, string firstName, string lastName, string contactEmail = null)
    {
        var filter = $"id  eq '{userId}'";
        var user = await GetUserByFilterAsync(filter);
        
        if (user == null)
            throw new UserDoesNotExistException(userId);
        
        var username = user.UserPrincipalName;
        
        if (user.GivenName!.Equals(firstName, StringComparison.CurrentCultureIgnoreCase) || user.Surname!.Equals(lastName, StringComparison.CurrentCultureIgnoreCase))
            username = await CheckForNextAvailableUsernameAsync(firstName, lastName, contactEmail);
        
        var updatedUser = new User
        {
            GivenName = firstName,
            Surname = lastName,
            DisplayName = $"{firstName} {lastName}",
            UserPrincipalName = username
        };
        
        if (!string.IsNullOrEmpty(contactEmail))
        {
            updatedUser.Mail = contactEmail;
            updatedUser.OtherMails = [contactEmail];
        }
        try
        {
            await client.UpdateUserAsync(user.Id, updatedUser);
            return updatedUser;
        }
        catch (ODataError odataError)
        {
            throw new UserServiceException("Failed to update the user in Microsoft Graph.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException("An unexpected error occurred while updating the user.", ex.Message);
        }
    }

    public async Task DeleteUserAsync(string username)
    {
        try
        {
            await client.DeleteUserAsync(username);
        }
        catch (ODataError odataError) 
        {
            throw new UserServiceException("Failed to delete the user in Microsoft Graph.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"An unexpected error occurred while deleting the user {username}.", ex.Message);
        }
    }

    public async Task AddUserToGroupAsync(string userId, string groupId)
    {
        var existingGroups = await GetGroupsForUserAsync(userId);
        if (existingGroups.Exists(x => x.Id == groupId))
            return;

        try
        {
            await client.AddUserToGroupAsync(userId, groupId);
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"Failed to add user {userId} to group {groupId}.", ex.Message);
        }
    }

    public async Task<User> GetUserByFilterAsync(string filter)
    {
        try
        {
            var users = await client.GetUsersAsync(filter);
            var adUser = users?.FirstOrDefault();

            return adUser;
        }
        catch (ODataError odataError) when (odataError.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"Failed to search user with filter {filter}", ex.Message);
        }
    }
    
    public string GetGroupIdFromSettings(string groupName)
    {
        var prop = settings.AdGroup.GetType().GetProperty(groupName);
        string groupId = string.Empty;

        if (prop != null)
        {
            groupId = (string)prop.GetValue(settings.AdGroup);
        }

        return groupId;
    }

    public async Task<Group> GetGroupByNameAsync(string groupName)
    {
        try
        {
            return await client.GetGroupByNameAsync(groupName);
        }
        catch (ODataError odataError)
        {
            throw new UserServiceException($"Failed to get group by name {groupName}.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"An unexpected error occurred while retrieving the group {groupName}.", ex.Message);
        }
    }

    public async Task<Group> GetGroupByIdAsync(string groupId)
    {
        try
        {
            return await client.GetGroupByIdAsync(groupId);
        }
        catch (ODataError odataError) when (odataError.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"Failed to get group by id {groupId}.", ex.Message);
        }
    }

    public async Task<List<Group>> GetGroupsForUserAsync(string userId)
    {
        try
        {
            return await client.GetGroupsForUserAsync(userId);
        }
        catch (ODataError odataError)
        {
            throw new UserServiceException($"Failed to get groups for user {userId}.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException($"An unexpected error occurred while retrieving groups for user {userId}.", ex.Message);
        }
    }
    
    public async Task<bool> IsUserAdminAsync(string principalId)
    {
        try
        {
            var userAssignedRoles = await client.GetRoleAssignmentsAsync(principalId);
            var adminRole = await client.GetRoleDefinitionAsync("User Administrator");
            
            return userAssignedRoles != null && userAssignedRoles.Any(r => r.RoleDefinitionId == adminRole.Id);
        }
        catch (Exception ex)
        {
            throw new UserServiceException("An unexpected error occurred while checking admin status.", ex.Message);
        }
    }

    /// <summary>
    /// Determine the next available username for a participant based on username format [firstname].[lastname]
    /// </summary>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="contactEmail"></param>
    /// <returns>next available user principal name</returns>
    public async Task<string> CheckForNextAvailableUsernameAsync(string firstName, string lastName, string contactEmail)
    {
        var regex = PeriodRegex();
        var sanitisedFirstName = regex.Replace(firstName, string.Empty);
        var sanitisedLastName = regex.Replace(lastName, string.Empty);

        sanitisedFirstName = sanitisedFirstName.Replace(" ", string.Empty);
        sanitisedLastName = sanitisedLastName.Replace(" ", string.Empty);

        var baseUsername = $"{sanitisedFirstName}.{sanitisedLastName}".ToLowerInvariant();
        var existingUsernames = await GetUsersMatchingNameAsync(baseUsername, contactEmail, firstName, lastName);
        return UsernameGenerator.GetIncrementedUsername(baseUsername, settings.ReformEmail, existingUsernames);
    }
    
    public async Task<List<User>> GetJudgesAsync(string username = null)
    {
        var judges = await client.GetUsersInGroupAsync(settings.AdGroup.VirtualRoomJudge);
        if (settings.IsLive)
        {
            //graph doesn't support inverse filtering of groups so test accounts need to be queried separately
            var testJudges = await client.GetUsersInGroupAsync(settings.AdGroup.TestAccount);
            judges = judges.Except(testJudges).ToList();
        }

        return judges
            .Where(x => !(x.GivenName?.StartsWith(PerformanceTestUserFirstName, StringComparison.OrdinalIgnoreCase) ?? true)
                        && (string.IsNullOrEmpty(username) || x.UserPrincipalName != null && x.UserPrincipalName.Contains(username, StringComparison.CurrentCultureIgnoreCase)))
            .OrderBy(x => x.DisplayName)
            .ToList();
    }
    
    public async Task<string> UpdateUserPasswordAsync(string username)
    {
        var newPassword = PasswordHelper.GenerateRandomPasswordWithDefaultComplexity();
        var userUpdate = new User
        {
            PasswordProfile = new PasswordProfile
            {
                Password = newPassword,
                ForceChangePasswordNextSignIn = true
            }
        };

        try
        {
            await client.UpdateUserAsync(username, userUpdate);
            return newPassword;
        }
        catch (ODataError odataError)
        {
            throw new UserServiceException("Failed to update the user password in Microsoft Graph.", odataError.Message);
        }
        catch (Exception ex)
        {
            throw new UserServiceException("An unexpected error occurred while updating the user password.", ex.Message);
        }
    }
    
    private async Task<IEnumerable<string>> GetUsersMatchingNameAsync(string baseUsername, string contactEmail, string firstName, string lastName)
    {
        var filterText = baseUsername.Replace("'", "''");
        var filter = $"startswith(userPrincipalName,'{filterText}')";
        var users = await client.GetUsersAsync(filter);

        var existingMatchedUsers = users?.Select(user => user.UserPrincipalName).ToList() ?? [];

        if (!string.IsNullOrEmpty(contactEmail))
        {
            var deletedMatchedUsersByContactMail = await GetDeletedUsersWithPersonalMailAsync(contactEmail);
            existingMatchedUsers.AddRange(deletedMatchedUsersByContactMail ?? []);
        }

        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            var deletedMatchedUsersByPrincipal = await GetDeletedUsersWithNameAsync(firstName, lastName);
            existingMatchedUsers.AddRange(deletedMatchedUsersByPrincipal ?? []);
        }

        return existingMatchedUsers;
    }
    
    private async Task<List<string>> GetDeletedUsersWithPersonalMailAsync(string contactMail)
    {
        var filter = $"startswith(mail,'{contactMail}')";
        return await client.GetDeletedUsernamesAsync(filter);
    }

    private async Task<List<string>> GetDeletedUsersWithNameAsync(string firstName, string lastName)
    {
        var filter = $"givenName eq '{firstName}' and surname eq '{lastName}'";
        return await client.GetDeletedUsernamesAsync(filter);
    }
}