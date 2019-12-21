using Microsoft.AspNetCore.Authorization;
using UserApi.Authorization;

namespace UserApi.Extensions
{
    public static class AuthorizationPolicyBuilderExtensions
    {
        public static void RequirePermissions(
            this AuthorizationPolicyBuilder builder,
            string[] delegated,
            string[] application = null,
            string[] userRoles = null)
        {
            builder.RequireAuthenticatedUser();
            builder.Requirements.Add(new PermissionRequirement
            {
                DelegatedPermissions = delegated,
                ApplicationPermissions = application ?? new string[0],
                UserRoles = userRoles ?? new string[0]
            });
        }
    }
}