using System.Threading.Tasks;
using UserApi.Contract.Responses;
using UserApi.Mappers;
using UserApi.Services;

namespace UserApi.Helper
{
    public class UserProfileHelper
    {
        private readonly IUserAccountService _userAccountService;
        
        public UserProfileHelper(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        public async Task<UserProfile> GetUserProfileAsync(string filter)
        {
            var user = await _userAccountService.GetUserByFilterAsync(filter);

            if (user == null)
            {
                return null;
            }

            return GraphUserMapper.MapToUserProfile(user);
        }
    }
}
