using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using UserApi.Contract.Responses;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IUserAccountService
    {
        /// <summary>
        /// Creates a new user with a username based on first and last name
        /// </summary>
        /// <exception cref="UserExistsException">Thrown if a user with the recovery email already exists</exception>
        Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail, bool isTestUser);
        Task<User> UpdateUserAccountAsync(Guid userId, string firstName, string lastName);
        Task DeleteUserAsync(string username);
        Task AddUserToGroupAsync(User user, Group group);
        Task<Group> GetGroupByNameAsync(string groupName);
        Task<Group> GetGroupByIdAsync(string groupId);
        Task<List<Group>> GetGroupsForUserAsync(string userId);
        Task<User> GetUserByFilterAsync(string filter);
        Task<IEnumerable<UserResponse>> GetJudgesAsync();
        Task<string> UpdateUserPasswordAsync(string username);
        Task<bool> IsUserAdminAsync(string principalId);
    }
}