/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿namespace Saltworks.SaltMiner.UiApiClient;

public static class FieldExtensions
{
    /// <summary>
    /// For use with import only - no field information assigned.
    /// </summary>
    public static List<TextField> ToAttributeFields(this Dictionary<string, string> attributes)
    {
        var ret = new List<TextField>();
        var attrs = attributes ?? [];
        foreach (var key in attrs.Keys)
            ret.Add(new() { Value = attrs[key], Name = key });
        return ret;
    }

    public static List<TextField> ToAttributeFields(this Dictionary<string, string> attributes, FieldInfo fieldInfo, bool setDefaults = false)
    {
        var ret = new List<TextField>();
        var attrs = attributes ?? [];
        foreach (var key in attrs.Keys)
        {
            try
            {
                ret.Add(new(attrs[key], key, fieldInfo, setDefaults, true));
            }
            catch (UiApiClientAttributeDefinitionException)
            {
                // ignore this exception, if "unmapped" attribute then no further action needed
            }
        }
        // If one or more attr defs weren't used, insert them with default values
        foreach (var ad in fieldInfo.AttributeDefinitions)
        {
            if (!attrs.ContainsKey(ad.Name))
                ret.Add(new(ad, fieldInfo));
        }
        return ret;
    }
    public static Dictionary<string, List<TextField>> ToAttributeFields(this Dictionary<string, Dictionary<string, string>> attributes, FieldInfo fieldInfo, bool setDefaults = false)
    {
        var ret = new Dictionary<string, List<TextField>>();
        var attrs = attributes ?? [];
        foreach (var section in attrs.Keys)
        {
            var dict = new List<TextField>();
            foreach (var key in attrs[section].Keys)
            {
                try
                {
                    dict.Add(new(attrs[section][key], section, key, fieldInfo, setDefaults));
                }
                catch (UiApiClientAttributeDefinitionException)
                {
                    // ignore this exception, if "unmapped" attribute then no further action needed
                }
            }
            // if one or more attr defs weren't used in this section, insert them with default values
            foreach (var ad in fieldInfo.AttributeDefinitions.Where(d => d.Section.Equals(section, StringComparison.OrdinalIgnoreCase)))
                if (!dict.Any(t => t.Name.Equals(ad.Name, StringComparison.OrdinalIgnoreCase)))
                    dict.Add(new(ad, fieldInfo));
            ret[section] = dict;
        }
        return ret;
    }
}
