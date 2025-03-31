using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        protected readonly ContextBase ContextBase;
        protected readonly ILogger Logger;
        private KibanaUser _CurrentUser = null;
        internal KibanaUser CurrentUser { 
            get
            {
                var kuser = HttpContext.Items[KibanaMiddleware.USER_TAG];
                if (_CurrentUser == null && kuser != null)
                    _CurrentUser = (KibanaUser)kuser;
                return _CurrentUser;
            }
        }

        public ApiControllerBase(ContextBase context, ILogger logger) : base()
        {
            ContextBase = context;
            Logger = logger;
            ContextBase.Controller = this;
        }
    }
}
