using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum AttributeType
    {
        [Description("Single line text (text)")]
        SingleLine = 0,
        [Description("Multi line text (text)")]
        MultiLine,
        [Description("Markdown (text)")]
        Markdown,
        [Description("Integer (long)")]
        Integer,
        [Description("Number (double)")]
        Number,
        [Description("Date (date)")]
        Date,
        [Description("Single select drop down")]
        Dropdown,
        [Description("Multi select drop down")]
        MultiSelect
    }
}