using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IIdentityServiceApiClient
    {
        Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string text);
        Task<NewAdUserAccount> CreateUserAsync(string username, string firstName, string lastName, string displayName, string recoveryEmail, bool isTestUser = false);
        Task<User> UpdateUserAccount(string userId, string firstName, string lastName, string newUsername, string contactEmail = null);
        Task DeleteUserAsync(string username);
        Task<string> UpdateUserPasswordAsync(string username);
    }
}