using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IIdentityServiceApiClient
    {
        Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string text);
        Task<NewAdUserAccount> CreateUserAsync(string username, string firstName, string lastName, string displayName, string recoveryEmail, bool isTestUser = false);
        Task DeleteUserAsync(string username);
        Task UpdateUserAsync(string username);
        Task<User> GetUserByUserPrincipalNameAsync(string text);
    }
}