using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class ServiceJobPrimer : UiModelBase
    {
        public List<ServiceJobType> ServiceJobTypes { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
    }
}
