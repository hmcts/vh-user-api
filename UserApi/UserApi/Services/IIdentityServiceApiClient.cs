using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserApi.Services
{
    public interface IIdentityServiceApiClient
    {
        Task<IEnumerable<string>> GetUsernamesStartingWith(string text);
    }
}