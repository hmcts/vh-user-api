using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IIdentityServiceApiClient
    {
        Task<IEnumerable<string>> GetUsernamesStartingWith(string text);

        Task<NewAdUserAccount> CreateUser(string username, string firstName, string lastName, string displayName, string recoveryEmail, string userRole);

        Task AddUserToGroupAsync(User user, Group group);

        Task<Group> GetGroupByNameAsync(string groupName);
    }
}