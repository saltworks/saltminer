using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient
{
    public class FieldInfo
    {
        public IEnumerable<FieldDefinition> FieldDefinitions { get; set; } = [];
        public IEnumerable<AttributeDefinitionValue> AttributeDefinitions { get; set; } = [];
        public IEnumerable<ActionDefinition> ActionDefinitions { get; set; } = [];
        public IEnumerable<AppRole> CurrentAppRoles { set; get; } = [];
        public string EntityType { get; set; }

        public IEnumerable<string> GetActionPermissions(bool disabled=true)
        {
            List<string> ap = [];
            foreach(var role in CurrentAppRoles)
                ap.AddRange(role.Actions.Where(a => a.Disable == disabled).Select(a => a.Name));
            return ap;
        }
    }
}
