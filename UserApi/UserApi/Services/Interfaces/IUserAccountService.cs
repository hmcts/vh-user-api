using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using UserApi.Services.Exceptions;
using UserApi.Services.Models;

namespace UserApi.Services.Interfaces;

public interface IUserAccountService
{
    /// <summary>
    /// Creates a new user with a username based on first and last name
    /// </summary>
    /// <exception cref="UserExistsException">Thrown if a user with the recovery email already exists</exception>
    /// <exception cref="InvalidEmailException">Thrown if the recovery email has an invalid email format</exception>
    Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail, bool isTestUser);
    Task<User> UpdateUserAccountAsync(Guid userId, string firstName, string lastName, string contactEmail = null);
    Task DeleteUserAsync(string username);
    Task<List<Group>> GetGroupsForUserAsync(string userId);
    Task<User> GetUserByFilterAsync(string filter);
    Task<List<User>> GetJudgesAsync(string username = null);
    Task<string> UpdateUserPasswordAsync(string username);
    Task<bool> IsUserAdminAsync(string principalId);
    string GetGroupIdFromSettings(string groupName);
    Task AddUserToGroupAsync(string userId, string groupId);
}