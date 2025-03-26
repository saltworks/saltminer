using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class CustomIssue : SaltMinerEntity
    {
        private static string _indexEntity = "sys_custom_issues";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Fields
        /// </summary>
        /// <seealso cref="CustomIssueField"/>
        public List<CustomIssueField> Fields { get; set; }

        public static bool Validate(CustomIssueField field)
        {
            var valid = true;
            if (field.Hidden && string.IsNullOrEmpty(field.Default))
            {
                valid = false;
            }

            return valid;
        }
    }

    public class CustomIssueField
    {
        public string Field { get; set; }
        public string Display => Regex.Replace(Field, @"(\B[A-Z])", @" $1");
        public bool Hidden { get; set; }
        public bool Required { get; set; }
        public string Default { get; set; }
    }
}