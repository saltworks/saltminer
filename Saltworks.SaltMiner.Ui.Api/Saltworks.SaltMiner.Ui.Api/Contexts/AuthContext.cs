using Saltworks.SaltMiner.Core.Email;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Extensions;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class AuthContext(IServiceProvider services, ILogger<AuthContext> logger, ApiClientFactory<AuthContext> kibanaClientFactory) : ContextBase(services, logger)
    {
        private ApiClient _kibanaClient = null;
        private string _kibanaBaseUrl;
        private readonly ApiClientFactory<AuthContext> KibanaClientFactory = kibanaClientFactory;

        private ApiClient GetApiClient()
        {
            if (_kibanaClient == null)
            {
                if (Controller == null)
                {
                    throw new UiApiAuthException("Controller missing.  Try calling PrimeContext() if no controller available.");
                }

                KibanaClientFactory.Options.BaseAddress = KibanaBaseUrl;
                _kibanaBaseUrl = KibanaBaseUrl;
                _kibanaClient = KibanaClientFactory.CreateApiClient();
            }
            return _kibanaClient;
        }

        /// <summary>
        /// This is a way to get around the problem of having no Controller or other HttpContext intrinsically available, while still supporting DI.
        /// </summary>
        /// <param name="context"></param>
        private void PrimeContext(HttpContext context)
        {
            if (string.IsNullOrEmpty(Config.KibanaBaseUrl))
            {
                // We expect the base address to be empty here usually, so we'll use the request to figure out our base URL
                _kibanaBaseUrl = context.GetBaseUrl(Logger);
                Logger.LogDebug("Kibana base URL derived from context: {Url}", _kibanaBaseUrl);
            }
            else
            {
                _kibanaBaseUrl = Config.KibanaBaseUrl;
                Logger.LogDebug("Kibana base URL derived from config: {Url}", _kibanaBaseUrl);
            }
            KibanaClientFactory.Options.BaseAddress = _kibanaBaseUrl;
            _kibanaClient = KibanaClientFactory.CreateApiClient();
        }

        public UiNoDataResponse Logout(HttpContext context = null)
        {
            if (context != null)
            {
                PrimeContext(context);
            }

            Logger.LogInformation("Logout User initiated");
            GetApiClient().GetAsync<string>("api/security/logout");

            return new UiNoDataResponse(0);
        }

        public UiDataItemResponse<string> GetCookie(string contextCookie)
        {
            return new UiDataItemResponse<string>(string.IsNullOrEmpty(contextCookie) ? Config.BypassCookie ?? string.Empty : contextCookie);
        }

        public UiDataItemResponse<KibanaUser> SetByPassCookie(string cookie)
        {
            Config.BypassCookie = cookie;
            return GetMe(cookie);
        }

        public UiDataItemResponse<KibanaUser> RemoveBypassCookie()
        {
            Config.BypassCookie = null;
            return GetMe(null);
        }

        public UiDataItemResponse<KibanaUser> GetMe(string cookie, HttpContext context = null)
        {
            Logger.LogInformation("Get Me for cookie '{Cookie}' initiated", cookie);
            
            if (string.IsNullOrEmpty(cookie))
            {
                if (Config.BypassCookie == null)
                {
                    return new UiDataItemResponse<KibanaUser>
                    {
                        Message = "Cookie is empty",
                        ErrorMessages = [ "Cookie is empty" ],
                        Data = null,
                        Affected = 0
                    };
                }

                cookie = Config.BypassCookie;
            }
            if (context != null)
            {
                PrimeContext(context);
            }

            // Do not output full cookie
            Logger.LogInformation("Set Cookie '...{Cookie}'", cookie[^8..]);
            GetApiClient().SetCookie(new Uri(_kibanaBaseUrl), KibanaUser.CookieTag, cookie);

            var url = "internal/security/me";
            Logger.LogDebug("Attempting call to Kibana profile at '{Url}'", $"{_kibanaBaseUrl}/{url}");
            KibanaUser result = null;
            try
            {
                result = GetApiClient().GetAsync<KibanaUser>(url).Result.Content;
            }
            catch (AggregateException ex)
            {
                var inex = ex.InnerException?.InnerException;
                if (inex is System.Security.Authentication.AuthenticationException && ex.InnerException.Source == "System.Net.Http" && ex.InnerException.Message.Contains("SSL"))
                {
                    throw new UiApiSslException($"SSL error when attempting to call Kibana: {inex.Message}", ex.InnerException);
                }
            }
            if (result == null)
            {
                Logger.LogError("Authentication failure - invalid response received from Kibana.");
                return new UiDataItemResponse<KibanaUser>(result);
            }
                
            // Remove unknown/invalid roles
            result.Roles ??= [];
            result.Roles.RemoveAll(r => Core.Extensions.EnumExtensions.GetValueFromDescription<SysRole>(r) == SysRole.None && !r.StartsWith(Config.AppRolePrefix));
            result.Roles = result.Roles.Select(r => r.StartsWith(Config.AppRolePrefix) ? r[(Config.AppRolePrefix.Length)..] : r).ToList();
                
            Logger.LogDebug("Kibana username: {Username}, role(s): {Roles}", result.UserName ?? "[Unknown/missing]", string.Join(',', result.Roles));

            if (result.UserName == null)
            {
                result = null;
            }
            else
            {
                result.Cookie = cookie;
                result.DateFormat = Config.DisplayDateFormat;
                result.MaxImportFileSize = Config.MaxImportFileSize;
            }

            return new UiDataItemResponse<KibanaUser>(result);
        }

        public UiDataItemResponse<KibanaUser> GetUser(string cookie, string userName, HttpContext context = null)
        {
            Logger.LogInformation("GetUser for User '{User}' initiated", userName);


            if (string.IsNullOrEmpty(cookie))
            {
                if (Config.BypassCookie == null)
                {
                    return new UiDataItemResponse<KibanaUser>
                    {
                        Message = "Cookie is empty",
                        Data = null,
                        Affected = 0
                    };
                }

                cookie = Config.BypassCookie;
            }
            
            if (context != null)
            {
                PrimeContext(context);
            }

            // Do not output full cookie to log
            Logger.LogInformation("Set Cookie '...{Cookie}'", cookie[^8..]);
            GetApiClient().SetCookie(new Uri(_kibanaBaseUrl), KibanaUser.CookieTag, cookie);

            var url = "internal/security/users/" + userName;
            Logger.LogDebug("Attempting call to Kibana profile at '{Url}'", $"{_kibanaBaseUrl}/{url}");
            var result = GetApiClient().GetAsync<KibanaUser>(url);

            var user = result.Result.Content;

            if (string.IsNullOrEmpty(user.UserName))
            {
                return new UiDataItemResponse<KibanaUser>
                {
                    Message = "User not found",
                    Data = null,
                    Affected = 0
                };
            }
            else 
            {
                Logger.LogDebug("Kibana username: {Username}, role(s): {Roles}", user.UserName ?? "[Unknown/missing]", string.Join(',', user.Roles));
                return new UiDataItemResponse<KibanaUser>(user);
            }
            
        }

        public void RequestAccess(KibanaUser kibanaUser, string message)
        {
            if (kibanaUser == null)
            {
                throw new UiApiNotFoundException("Kibana User not found");
            }

            Logger.LogInformation("RequestAccess for User '{User}' initiated", kibanaUser.UserName);
           
            if (Config.EmailFromAddress == null || Config.EmailHost == null || Config.EmailUserName == null || Config.EmailPassword == null)
            {
                throw new UiApiClientValidationMissingValueException("Email Configuration not set up, request was not sent.");
            }

            var request = new EmailRequest(Config.EmailFromAddress, Config.EmailFromDisplay, Config.RequestAccessEmail, Config.RequestAccessEmailName)
            {
                Body = $"{kibanaUser.UserName} is requesting PenTest access.",
                Subject = $"{kibanaUser.UserName} Requesting Access",
                Host = Config.EmailHost,
                Port = Config.EmailPort,
                UserName = Config.EmailUserName,
                Password = Config.EmailPassword
            };

            if (message != null)
            {
                request.Body = $"{request.Body} Message: {message}";
            }

            Logger.LogInformation("Request Sent to '{Address}'", Config.RequestAccessEmail);
            Email.Send(request);
        }
    }
}
