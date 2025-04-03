using Microsoft.AspNetCore.Mvc.Filters;

namespace Saltworks.SaltMiner.Ui.Api.Authentication
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllowAnonymousAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
        }
    }
}