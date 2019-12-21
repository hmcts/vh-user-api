using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IUserAccountService
    {
        Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail);
        Task AddUserToGroupAsync(User user, Group group);
        Task<Group> GetGroupByNameAsync(string groupName);
        Task<Group> GetGroupByIdAsync(string groupId);
        Task<List<Group>> GetGroupsForUserAsync(string userId);
        Task<User> GetUserByFilterAsync(string filter);
        Task<IEnumerable<UserResponse>> GetJudgesAsync();
    }
}