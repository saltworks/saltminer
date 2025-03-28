using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class EngagementNew : UiModelBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Summary { get; set; }

        [Required]
        public string Customer { get; set; }

        [Required]
        [SubtypeValidation]
        public string Subtype { get; set; }

        public string GroupId { get; set; }

        public Engagement TransformNewEngagement()
        {
            return new Engagement
            {
                Timestamp = DateTime.UtcNow,
                Saltminer = new SaltMinerEngagementWrapper
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = Name,
                        Customer = Customer,
                        Summary = Summary,
                        PublishDate = null,
                        Subtype = Subtype,
                        Attributes = null,
                        GroupId = (string.IsNullOrEmpty(GroupId) ? Guid.NewGuid().ToString() : GroupId),
                        Status = EngagementStatus.Draft.ToString("g")
                    }
                }
            };
        }
    }
}