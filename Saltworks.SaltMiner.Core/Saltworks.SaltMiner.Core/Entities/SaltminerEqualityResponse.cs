using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class SaltminerEqualityResponse
    {
        public List<string> Messages;
        public bool IsEqual => !Messages?.Any() ?? false;

        public SaltminerEqualityResponse(List<string> result)
        {
            this.Messages = result;
        }
    }
}