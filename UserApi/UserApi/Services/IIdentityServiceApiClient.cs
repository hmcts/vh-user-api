using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserApi.Services
{
    public interface IIdentityServiceApiClient
    {
        Task<IEnumerable<string>> GetUsernamesStartingWith(string text);

        Task CreateUser(string username, string firstName, string lastName, string displayName, string recoveryEmail);
    }
}