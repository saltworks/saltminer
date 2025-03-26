using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Core.Entities;

[Serializable]
public class AppRole : SaltMinerEntity
{
    private static string _indexEntity = "sys_app_roles";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    /// <summary>
    /// Gets or sets role name.
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets role permissions
    /// </summary>
    /// <seealso cref="FieldPermission"/>
    public List<FieldPermission> Permissions { get; set; } = [];

    public List<ActionPermission> Actions { get; set; } = [];
}

public class ActionPermission
{
    /// <summary>
    /// Name of the action to be restricted
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Custom label for the action - may not be implemented in all cases
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// If set, disable the action.  If not set, action is available.
    /// </summary>
    public bool Disable { get; set; }
}

public class FieldPermission
{
    /// <summary>
    /// Gets or sets permission scope. ie. "IssueStandard", "IssueAttribute", "AssetStandard", "AssetAttribute", "EngagementAttribute, InventoryAssetStandard, InventoryAssetAttribute"
    /// </summary>
    [Required]
    public string Scope { get; set; }

    [JsonIgnore]
    public bool IsValidScope => Enum.TryParse<FieldPermissionScope>(Scope, out _);
    [JsonIgnore]
    public FieldPermissionScope FieldPermissionScope => Enum.Parse<FieldPermissionScope>(Scope);

    /// <summary>
    /// Gets or sets the field name to get the permission.
    /// </summary>
    [Required]
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets permission scope. ie. "h", "r"
    /// </summary>
    [Required]
    public string Permission { get; set; }

    public bool IsHidden => Permission.Equals("h");
    public bool IsReadOnly => Permission.Equals("r");
    public static FieldPermissionScope ScopeStandard(string entityType) => Enum.TryParse<FieldPermissionScope>($"{entityType}Standard", true, out var result) ? 
        result : throw new ArgumentException($"Unknown entity type '{entityType}'", nameof(entityType));
    public static FieldPermissionScope ScopeAttribute(string entityType) => Enum.TryParse<FieldPermissionScope>($"{entityType}Attribute", true, out var result) ?
        result : throw new ArgumentException($"Unknown entity type '{entityType}'", nameof(entityType));
}

public enum FieldPermissionScope
{
    IssueStandard=0, IssueAttribute, AssetStandard, AssetAttribute, EngagementAttribute, InventoryAssetStandard, InventoryAssetAttribute
}
