namespace Saltworks.SaltMiner.UiApiClient.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InputValidationAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class SubtypeValidationAttribute : InputValidationAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class AttributesValidationAttribute : InputValidationAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class SeverityValidationAttribute : InputValidationAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class TestStatusValidationAttribute : InputValidationAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class DateValidationAttribute : InputValidationAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class MarkdownAttribute : Attribute { }
}
