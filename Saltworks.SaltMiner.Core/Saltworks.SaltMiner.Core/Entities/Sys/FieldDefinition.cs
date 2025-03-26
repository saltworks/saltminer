using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class FieldDefinition : SaltMinerEntity
    {
        private static string _indexEntity = "sys_field_definitions";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets entity type. ie: "Issue", "Asset", "Engagement"
        /// </summary>
        [Required]
        public string Entity { get; set; }
        [JsonIgnore]
        public bool IsValidEntity => Enum.TryParse<EntityType>(Entity, out _);
        [JsonIgnore]
        public EntityType EntityType => Enum.Parse<EntityType>(Entity);
        /// <summary>
        /// Gets or sets field display name.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Gets or sets field name.  This name should match the datastore property name.  For example, issue.vulnerability.found_date would be "found_date".
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the field default value.  System fields (System == true) can have default values.
        /// </summary>
        public string Default { get; set; }
        /// <summary>
        /// Gets or sets flag to require a value.  This does not mean the field is required by SaltMiner (system).
        /// </summary>
        public bool Required { get; set; } = false;
        /// <summary>
        /// Gets or sets flag to hide the field.  A field that is hidden can only be required if it has a default value.
        /// </summary>
        public bool Hidden { get; set; } = false;
        /// <summary>
        /// Gets or sets flag to identify system field.  This means SaltMiner (system) requires the field and its permissions cannot be changed.  System fields are always required.
        /// </summary>
        public bool System { get; set; } = false;
        /// <summary>
        /// Gets or sets flag to identify read only field.  A field that is read only can only be required if it has a default value.
        /// </summary>
        public bool ReadOnly { get; set; } = false;
    }

    public enum EntityType
    {
        Issue = 0, Asset, Engagement, InventoryAsset
    }
}
