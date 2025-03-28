using Saltworks.SaltMiner.Core.Entities;
using System.Globalization;

namespace Saltworks.SaltMiner.UiApiClient
{
    public abstract class Field
    {
        protected Field() { }
        protected Field(FieldInfo fieldInfo, FieldDefinition fieldDefinition) { Initialize(fieldInfo, fieldDefinition); }
        protected Field(FieldInfo fieldInfo, AttributeDefinitionValue attributeDefinition) { Initialize(fieldInfo, attributeDefinition); }
        protected Field(FieldInfo fieldInfo, string fieldName, bool isAttribute = false) { Initialize(fieldInfo, fieldName, isAttribute); }
        protected Field(FieldInfo fieldInfo, string section, string fieldName) { Initialize(fieldInfo, section, fieldName); }
        public string Name { get; set; }
        public string Label { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }
        public bool IsRequired { get; set; }
        public bool IsSystem { get; set; }
        protected FieldDefinition FieldDefinition { get; set; } = null;
        protected AttributeDefinitionValue AttributeDefinition { get; set; } = null;

        protected void Initialize(FieldInfo fieldInfo, string section, string fieldName)
        {
            var definition = fieldInfo.AttributeDefinitions.FirstOrDefault(ad => ad.Section.Equals(section, StringComparison.OrdinalIgnoreCase) && ad.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)) ??
                throw new UiApiClientFieldDefinitionException($"No attribute definition found for name '{fieldName}' and section '{section}'.");
            Initialize(fieldInfo, definition);
        }

        protected void Initialize(FieldInfo fieldInfo, string fieldName, bool isAttribute = false)
        {
            if (!isAttribute)
            {
                var definition = fieldInfo.FieldDefinitions.FirstOrDefault(fd => fd.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)) ?? 
                    throw new UiApiClientFieldDefinitionException($"No field definition found for name '{fieldName}'.");
                Initialize(fieldInfo, definition);  // will set field attribs
            }
            else
            {
                var definition = fieldInfo.AttributeDefinitions.FirstOrDefault(fd => fd.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)) ??
                    throw new UiApiClientAttributeDefinitionException($"No attribute definition found for name '{fieldName}'.");
                Initialize(fieldInfo, definition);
            }
        }

        protected void Initialize(FieldInfo fieldInfo, FieldDefinition definition)
        {
            FieldDefinition = definition;
            Name = definition.Name;
            Label = definition.Label;
            IsReadOnly = definition.ReadOnly;
            IsHidden = definition.Hidden;
            IsRequired = definition.Required;
            IsSystem = definition.System;
            SetFieldAttributes(fieldInfo, FieldScope);
        }

        protected void Initialize(FieldInfo fieldInfo, AttributeDefinitionValue definition)
        {
            AttributeDefinition = definition;
            Name = definition.Name;
            Label = definition.Display;
            IsReadOnly = definition.ReadOnly;
            IsHidden = definition.Hidden;
            IsRequired = definition.Required;
            IsSystem = false;
            SetFieldAttributes(fieldInfo, fieldInfo.EntityType + "Attribute");
        }

        protected void SetFieldAttributes(FieldInfo fieldInfo, string scope)
        {
            if (!fieldInfo.CurrentAppRoles.Any()) return;
            foreach (var role in fieldInfo.CurrentAppRoles)
            {
                foreach (var perm in role.Permissions.Where(p => p.FieldName == Name && p.Scope == scope))
                {
                    IsReadOnly = perm.IsReadOnly;
                    IsHidden = perm.IsHidden;
                }
            }
        }

        protected string FieldScope => FieldDefinition?.Entity + "Standard";
    }

    public class TextField : Field
    {
        public TextField() : base() { }
        /// <summary>
        /// For attributes of inv asset
        /// </summary>
        public TextField(string value, string section, string name, FieldInfo fieldInfo, bool setDefault = false) : base(fieldInfo, section, name)
        {
            DefaultValue = AttributeDefinition.Default;
            Value = setDefault ? DefaultValue : value;
        }
        public TextField(AttributeDefinitionValue def, FieldInfo fieldInfo) : base(fieldInfo, def)
        {
            Value = def.Default;
            DefaultValue = def.Default;
        }
        public TextField(string value, string name, FieldInfo info, bool setDefault = false, bool isAttribute = false) : base(info, name, isAttribute)
        {
            if (IsHidden)
            {
                Value = "";
                return;
            }
            DefaultValue = FieldDefinition?.Default ?? AttributeDefinition?.Default;
            Value = setDefault && string.IsNullOrEmpty(value) ? DefaultValue : value;
        }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }
    public class MarkdownField : Field
    {
        public MarkdownField() : base() { }
        public MarkdownField(string value, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {

            if (IsHidden)
            {
                Value = "";
                return;
            }
            DefaultValue = FieldDefinition.Default;
            Value = setDefault && string.IsNullOrEmpty(value) ? DefaultValue : value;
        }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }
    public class BooleanField : Field
    {
        public BooleanField() : base() { }
        public BooleanField(bool? value, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {
            if (IsHidden)
            {
                Value = null;
                return;
            }
            if (bool.TryParse(FieldDefinition.Default, out var dv))
                DefaultValue = dv;
            Value = setDefault ? (Value ?? DefaultValue ?? value) : value;
        }
        public bool? Value { get; set; }
        public bool? DefaultValue { get; set; }
    }
    public class DateField : Field
    {
        public DateField() : base() { }
        public DateField(DateTime? value, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {
            if (IsHidden)
            {
                Value = null;
                return;
            }
            if (!string.IsNullOrEmpty(FieldDefinition.Default) && DateTime.TryParse(FieldDefinition.Default, new CultureInfo("en-US"), out var val))
                DefaultValue = val;
            Value = setDefault ? value ?? DefaultValue : value;
        }
        public DateTime? Value { get; set; }
        public DateTime? DefaultValue { get; set; }
    }
    public class NumberField : Field
    {
        public NumberField() : base() { }
        public NumberField(float? value, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {
            if (IsHidden)
            {
                Value = null;
                return;
            }
            if (!string.IsNullOrEmpty(FieldDefinition.Default) && float.TryParse(FieldDefinition.Default, out var val))
                DefaultValue = val;
            Value = setDefault ? value ?? DefaultValue : value;
        }

        public float? Value { get; set; }
        public float? DefaultValue { get; set; }
    }
    public class SelectField : Field
    {
        public SelectField() : base() { }
        public SelectField(string value, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {
            if (IsHidden)
            {
                Value = "";
                return;
            }
            DefaultValue = FieldDefinition.Default;
            Value = setDefault && string.IsNullOrEmpty(value) ? DefaultValue : value;
        }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }

    public class MultiSelectField : Field
    {
        public MultiSelectField() : base() { }
        public MultiSelectField(string values, string name, FieldInfo info, bool setDefault = false) : base(info, name)
        {
            if (IsHidden)
            {
                Value = [];
                return;
            }
            if (!string.IsNullOrEmpty(FieldDefinition.Default))
                DefaultValue = FieldDefinition.Default.Split(',');
            Value = setDefault && string.IsNullOrEmpty(values) ? DefaultValue : values.Split(',');
        }
        public string[] Value { get; set; }
        public string[] DefaultValue { get; set; }
    }
}
