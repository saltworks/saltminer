using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class ErrorContext(IServiceProvider services, ILogger<ErrorContext> logger) : ContextBase(services, logger)
    {
        public static List<string> HarvestModelErrors(ModelStateDictionary modelState)
        {
            var errors = new List<string>();

            foreach (var modelKey in modelState.Keys)
            {
                var model = modelState[modelKey];

                foreach (var error in model.Errors)
                {
                    errors.Add(error.ErrorMessage);
                }
            }

            return errors;
        }
    }
}
