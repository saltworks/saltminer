using System;

namespace Saltworks.SaltMiner.Core.Entities;

[Serializable]
public class ActionDefinition : SaltMinerEntity
{
    private static string _indexEntity = "sys_action_definitions";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    /// <summary>
    /// Name of the action
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets Description - describe the action (and its location).
    /// </summary>
    public string Description { get; set; }
}