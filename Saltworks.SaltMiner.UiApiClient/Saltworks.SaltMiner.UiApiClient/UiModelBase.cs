using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.UiApiClient
{
    public abstract class UiModelBase
    {
        #region Validation

        public virtual void IsModelValid(string regex, string splatReplace = null, bool replaceMarkdown = false, List<LookupValue> subtypeOptions = null, List<AttributeDefinitionValue> attributeOptions = null, List<LookupValue> testedOptions = null, bool leaveOffFieldinError = false)
        {
            Type type = GetType();
            Regex rx = new Regex(regex);
            List<string> list = [];
            List<string> list2 = [];
            foreach (PropertyInfo pi in from p in type.GetProperties()
                                        where p.PropertyType == typeof(string) || p.PropertyType == typeof(Dictionary<string, string>) || p.PropertyType == typeof(Nullable<DateTime>)
                                        select p)
            {
                Attribute[] customAttributes = Attribute.GetCustomAttributes(pi);
                if (customAttributes == null || customAttributes.Length == 0 || customAttributes.All((x) => x is not MarkdownAttribute))
                {
                    if (pi.PropertyType == typeof(Nullable<DateTime>))
                    {
                        if (pi.GetValue(this) != null)
                        {
                            AddDateErrors((DateTime?)pi.GetValue(this), pi.Name, list2);
                        }
                    }
                    else if (pi.PropertyType == typeof(string))
                    {
                        if (pi.GetValue(this) != null)
                        {
                            AddErrors(replaceMarkdown, leaveOffFieldinError, rx, list, pi.Name, pi.GetValue(this).ToString());
                        }

                        if (replaceMarkdown)
                        {
                            foreach (string item in list)
                            {
                                string text = pi.GetValue(this).ToString().Replace(item, splatReplace);
                                pi.SetValue(this, text?.Trim(), null);
                            }

                            list = [];
                        }
                    }
                    else if (pi.GetValue(this) != null)
                    {
                        List<string> list3 = [];
                        foreach (KeyValuePair<string, string> kvp2 in pi.GetValue(this) as Dictionary<string, string>)
                        {
                            AttributeDefinitionValue attributeDefinitionValue = attributeOptions.FirstOrDefault((x) => x.Name == kvp2.Key);
                            if (attributeDefinitionValue != null && attributeDefinitionValue.Type.Contains("markdown", StringComparison.OrdinalIgnoreCase))
                            {
                                list3.Add(kvp2.Key);
                            }
                            else if (attributeDefinitionValue != null && attributeDefinitionValue.Type.Contains("multi select", StringComparison.OrdinalIgnoreCase) && kvp2.Value != null)
                            {
                                foreach (string item2 in JsonSerializer.Deserialize<List<string>>(kvp2.Value))
                                {
                                    AddErrors(replaceMarkdown, leaveOffFieldinError, rx, list, pi.Name + "." + kvp2.Key, item2);
                                }
                            }
                            else if (attributeDefinitionValue != null && !string.IsNullOrEmpty(kvp2.Value))
                            {
                                AddErrors(replaceMarkdown, leaveOffFieldinError, rx, list, pi.Name + "." + kvp2.Key, kvp2.Value.ToString());
                            }
                        }

                        if (replaceMarkdown)
                        {
                            Dictionary<string, string> dictionary = [];
                            foreach (KeyValuePair<string, string> item3 in (Dictionary<string, string>)pi.GetValue(this, null))
                            {
                                string newKey = item3.Key;
                                string text2 = item3.Value;
                                if (list3.All((x) => x != newKey))
                                {
                                    foreach (string item4 in list)
                                    {
                                        if (!string.IsNullOrEmpty(text2))
                                        {
                                            text2 = text2.Replace(item4, splatReplace);
                                        }
                                    }
                                }

                                dictionary[newKey] = text2;
                            }

                            list = [];
                            pi.SetValue(this, dictionary, null);
                        }
                    }
                }

                if (list.Count == 0 || customAttributes == null || customAttributes.Length == 0 || !customAttributes.Any((x) => x is InputValidationAttribute) || pi.GetValue(this) == null)
                {
                    continue;
                }

                if (customAttributes.Any((x) => x is SeverityValidationAttribute) && !Enum.IsDefined(typeof(Severity), pi.GetValue(this)))
                {
                    if (leaveOffFieldinError)
                    {
                        list2.Add($"'{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                    else
                    {
                        list2.Add($"{pi.Name}: '{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                }

                if (customAttributes.Any((x) => x is TestStatusValidationAttribute) && testedOptions.All((x) => x.Value != pi.GetValue(this).ToString()))
                {
                    if (leaveOffFieldinError)
                    {
                        list2.Add($"'{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                    else
                    {
                        list2.Add($"{pi.Name}: '{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                }

                if (customAttributes.Any((x) => x is AttributesValidationAttribute))
                {
                    foreach (KeyValuePair<string, string> kvp in pi.GetValue(this) as Dictionary<string, string>)
                    {
                        AttributeDefinitionValue attributeDefinitionValue2 = attributeOptions.FirstOrDefault((x) => x.Name == kvp.Key);
                        if (attributeDefinitionValue2 == null || attributeDefinitionValue2.Options == null || attributeDefinitionValue2.Options.Count <= 0)
                        {
                            continue;
                        }

                        if (attributeDefinitionValue2.Type.Contains("single", StringComparison.OrdinalIgnoreCase))
                        {
                            if (attributeDefinitionValue2.Options.All((x) => x != kvp.Value))
                            {
                                if (leaveOffFieldinError)
                                {
                                    list2.Add("'" + kvp.Value + "' has invalid values. Please use provided values only.");
                                    continue;
                                }

                                list2.Add($"{pi.Name}.{kvp.Key}: '{kvp.Value}' has invalid values. Please use provided values only.");
                            }

                            continue;
                        }

                        List<string> list4 = JsonSerializer.Deserialize<List<string>>(kvp.Value);
                        if (list4 != null && list4.Count > 0 && list4.Intersect(attributeDefinitionValue2.Options).Count() != list4.Count)
                        {
                            if (leaveOffFieldinError)
                            {
                                list2.Add("'" + kvp.Value + "' has invalid values. Please use provided values only.");
                                continue;
                            }

                            list2.Add($"{pi.Name}.{kvp.Key}: '{kvp.Value}' has invalid values. Please use provided values only.");
                        }
                    }
                }

                if (customAttributes.Any((x) => x is SubtypeValidationAttribute) && subtypeOptions.All((x) => x.Value != pi.GetValue(this).ToString()))
                {
                    if (leaveOffFieldinError)
                    {
                        list2.Add($"'{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                    else
                    {
                        list2.Add($"{pi.Name}: '{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                }

                if (!customAttributes.Any((x) => x is DateValidationAttribute))
                {
                    continue;
                }

                DateTime? dateTime = pi.GetValue(this) as DateTime?;
                if (!dateTime.HasValue || dateTime.Value.Year <= 1990)
                {
                    if (leaveOffFieldinError)
                    {
                        list2.Add($"'{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                    else
                    {
                        list2.Add($"{pi.Name}: '{pi.GetValue(this)}' is not a valid value. Please use provided values only.");
                    }
                }
            }

            if (list.Count > 0 && !replaceMarkdown)
            {
                list.Add("Please use Markdown Fields for more detailed information.");
                list2.AddRange(list);
            }

            if (list2.Count > 0)
            {
                throw new UiApiClientValidationException(list2);
            }
        }

        private static void AddErrors(bool replaceMarkdown, bool leaveOffFieldinError, Regex rx, List<string> mdErrors, string name, string value)
        {
            MatchCollection source = rx.Matches(value);
            if (source.Count == 0)
            {
                return;
            }

            if (replaceMarkdown)
            {
                mdErrors.AddRange(source.Select((x) => x.Value));
                return;
            }

            string value2 = string.Join(",", source.Select((x) => CheckForWhiteSpace(x.Value)).ToList());
            if (leaveOffFieldinError)
            {
                mdErrors.Add($"'{value}' has invalid characters({value2}).");
            }
            else
            {
                mdErrors.Add($"{name}: '{value}' has invalid characters({value2}).");
            }
        }

        private static void AddDateErrors(DateTime? dateValue, string dateFieldName, List<string> errors)
        {
            if (dateFieldName == "RemovedDate" && dateValue > DateTime.Today)
            {
                errors.Add($"The date with value '{dateValue?.ToString("MM/dd/yyyy")}' cannot be greater than today's date");
            }
        }

        private static string CheckForWhiteSpace(string value)
        {
            var c = value.FirstOrDefault(ch => char.IsWhiteSpace(ch));
            if (c != default)
                return "whitespace (U+" + ((int)c).ToString("X4") + ")";
            return value;
        }

        #endregion

        #region Role Permissions

        public void ApplyRoles<T>(FieldInfo fieldInfo, Func<T> getOriginal)
        {
            var scopeStd = FieldPermission.ScopeStandard(fieldInfo.EntityType);
            var scopeAtt = FieldPermission.ScopeAttribute(fieldInfo.EntityType);
            var roles = fieldInfo.CurrentAppRoles
                .Where(r => r.Permissions.Exists(p => p.FieldPermissionScope.Equals(scopeStd) || p.FieldPermissionScope.Equals(scopeAtt)));
            if (!roles.Any())
                return;  // if no updates needed we can skip pulling the original
            var original = getOriginal.Invoke();
            var orgAttr = original.GetType().GetProperties().FirstOrDefault(p => p.Name == "Attributes");
            var thisAttr = GetType().GetProperties().FirstOrDefault(p => p.Name == "Attributes");
            var perms = ExtractPermissions(roles, FieldPermissionScope.EngagementAttribute, FieldPermissionScope.EngagementAttribute);
            if (orgAttr != null && thisAttr != null)
                ResetFields(thisAttr, orgAttr, perms.Item1);
            ResetFields(this, original, perms.Item2);
        }

        private static void ResetFields(object update, object org, IEnumerable<FieldPermission> perms)
        {
            // Don't need to check for r/o or h, since both don't allow updates.
            // Reset fields that are present in both org and update objects.
            var updProps = update.GetType().GetProperties().Where(p => p.CanWrite) ?? [];
            var orgProps = org?.GetType().GetProperties().Where(p => p.CanWrite) ?? [];
            foreach (var prop in updProps)
            {
                foreach (var perm in perms.Where(p => p.FieldName == prop.Name))
                    if (orgProps.Any(p => p.Name == prop.Name))
                        prop.SetValue(update, orgProps.First(p => p.Name == prop.Name).GetValue(org));  // set the original value
                    else
                        prop.SetValue(update, default);  // no original value available (might be new object), set default (null/blank)
            }
        }

        private static Tuple<IEnumerable<FieldPermission>, IEnumerable<FieldPermission>> ExtractPermissions(IEnumerable<AppRole> roles, FieldPermissionScope scope1, FieldPermissionScope scope2)
        {
            List<FieldPermission> onePermissions = [];
            List<FieldPermission> twoPermissions = [];
            foreach (var role in roles)
            {
                foreach (var perm in role.Permissions)
                {
                    if (perm.FieldPermissionScope == scope1)
                        onePermissions.Add(perm);
                    if (perm.FieldPermissionScope == scope2)
                        twoPermissions.Add(perm);
                }
            }
            return Tuple.Create(onePermissions.AsEnumerable(), twoPermissions.AsEnumerable());
        }

        #endregion
    }
}
