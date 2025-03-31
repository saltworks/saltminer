using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.Ui.Api.Models
{
    internal class FieldInfoCache
    {
        private List<FieldDefinition> FieldDefinitions = [];
        private DateTime? FieldTimestamp { get; set; } = null;
        private List<AttributeDefinition> AttributeDefinitions = [];
        private List<ActionDefinition> ActionDefinitions = [];
        private DateTime? ActionTimestamp { get; set; } = null;
        private DateTime? AttrTimestamp { get; set; } = null;
        private List<AppRole> AppRoles = [];
        private DateTime? ArTimestamp { get; set; } = null;
        private DateTime? AcTimestamp { get; set; } = null;

        private static int RandomSecs => Random.Shared.Next(120, 240);

        /// <summary>
        /// Returns field info (FieldDefinitions, AttributeDefinitionValues, and AppRoles) for the given types
        /// </summary>
        /// <param name="type">Type of info to return (i.e. asset, engagement, issue)</param>
        /// <param name="userRoles">Roles granted to current user.  Only populates result with matching AppRoles (none if empty)</param>
        /// <param name="data">DataClient reference to use to update cache if needed</param>
        internal FieldInfo GetFieldInfo(FieldInfoEntityType type, IEnumerable<string> userRoles, DataClient.DataClient data = null)
        {
            var t = type.ToString("g");
            if (!Enum.TryParse<EntityType>(t, out var _) || !Enum.TryParse<AttributeDefinitionType>(t, out var _))
                throw new ArgumentException("Unsupported type " + t);
            UpdateFieldDefinitions(data);
            UpdateAttributeDefinitions(data);
            UpdateAppRoles(data);
            UpdateActionDefinitions(data);
            return new() { 
                FieldDefinitions = FieldDefinitions.Where(ad => ad.Entity.Equals(t, StringComparison.OrdinalIgnoreCase)), 
                AttributeDefinitions = AttributeDefinitions.FirstOrDefault(ad => ad.Type.Equals(t, StringComparison.OrdinalIgnoreCase))?.Values ?? [], 
                ActionDefinitions = ActionDefinitions,
                CurrentAppRoles = AppRoles.Where(r => userRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase)),
                EntityType = type.ToString("g")
            };
        }

        internal List<AttributeDefinition> GetAttributeDefinitions(DataClient.DataClient data = null)
        {
            UpdateAttributeDefinitions(data);
            return AttributeDefinitions;
        }

        internal List<ActionDefinition> GetActionDefinitions(DataClient.DataClient data = null)
        {
            UpdateActionDefinitions(data);
            return ActionDefinitions;
        }

        internal List<AppRole> GetAppRoles(DataClient.DataClient data = null)
        {
            UpdateAppRoles(data);
            return AppRoles;
        }

        private void UpdateFieldDefinitions(DataClient.DataClient data = null)
        {
            try
            {
                if (data != null && (FieldDefinitions.Count == 0 || FieldTimestamp == null || DateTime.UtcNow.Subtract(FieldTimestamp.Value).Seconds > RandomSecs))
                    FieldDefinitions = data.FieldDefinitionSearch(new()).Data?.ToList() ?? [];
            }
            catch (DataClientResponseException ex)
            {
                throw new UiApiException("Field definitions failed to update.  Make sure they exist in data.", ex);
            }
        }

        private void UpdateAttributeDefinitions(DataClient.DataClient data)
        {
            try
            {
                if (data != null && (AttributeDefinitions.Count == 0 || AttrTimestamp == null || DateTime.UtcNow.Subtract(AttrTimestamp.Value).Minutes > RandomSecs))
                    AttributeDefinitions = data.AttributeDefinitionSearch(new()).Data?.ToList() ?? [];
            }
            catch (DataClientResponseException ex)
            {
                throw new UiApiException("Attribute definitions failed to update.  Make sure they exist in data.", ex);
            }
        }

        private void UpdateAppRoles(DataClient.DataClient data = null)
        {
            try
            {
                if (data != null && (AppRoles.Count == 0 || ArTimestamp == null || DateTime.UtcNow.Subtract(ArTimestamp.Value).Minutes > RandomSecs))
                    AppRoles = data.RoleSearch(new()).Data?.ToList() ?? [];
            }
            catch (DataClientResponseException ex)
            {
                throw new UiApiException("App roles failed to update.  Make sure they exist in data.", ex);
            }
        }

        private void UpdateActionDefinitions(DataClient.DataClient data = null)
        {
            try
            {
                if (data != null && (ActionDefinitions.Count == 0 || AcTimestamp == null || DateTime.UtcNow.Subtract(AcTimestamp.Value).Minutes > RandomSecs))
                    ActionDefinitions = data.ActionDefinitionSearch(new()).Data?.ToList() ?? [];
            }
            catch (DataClientResponseException ex)
            {
                throw new UiApiException("Action definitions failed to update.  Make sure they exist in data.", ex);
            }
        }
    }

    public enum FieldInfoEntityType { Asset, Engagement, Issue, InventoryAsset }
}
