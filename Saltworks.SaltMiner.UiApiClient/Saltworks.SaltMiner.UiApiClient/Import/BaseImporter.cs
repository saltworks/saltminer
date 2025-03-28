using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Import.Upgrade;

namespace Saltworks.SaltMiner.UiApiClient.Import
{
    public class BaseImporter
    {
        protected DataClient.DataClient DataClient { get; set; }
        protected readonly ILogger Logger;
        protected readonly FileHelper FileHelper;
        protected readonly ImportUpgradeTool UpgradeTool;
        protected readonly AttachmentHelper AttachmentHelper;
        protected readonly EngagementHelper EngagementHelper;

        public BaseImporter(DataClient.DataClient dataClient, ILogger logger)
        {
            DataClient = dataClient;
            Logger = logger;
            FileHelper = new FileHelper(DataClient, Logger);
            UpgradeTool = new ImportUpgradeTool(Logger);
            EngagementHelper = new EngagementHelper(DataClient, Logger);
            AttachmentHelper = new AttachmentHelper(DataClient, Logger);
        }


        private List<AttributeDefinition> _attributeDefinitions = null;
        protected List<AttributeDefinition> AttributeDefinitions
        {
            get
            {
                _attributeDefinitions ??= DataClient.AttributeDefinitionSearch(new SearchRequest { }).Data.ToList();
                return _attributeDefinitions;
            }
        }
        protected List<AttributeDefinitionValue> IssueAttributeDefinitions =>
            AttributeDefinitions?.FirstOrDefault(x => x.Type == AttributeDefinitionType.Issue.ToString())?.Values ?? [];
        protected List<AttributeDefinitionValue> AssetAttributeDefinitions =>
           AttributeDefinitions?.FirstOrDefault(x => x.Type == AttributeDefinitionType.Asset.ToString())?.Values ?? [];
        protected List<AttributeDefinitionValue> EngagementAttributeDefinitions =>
            AttributeDefinitions?.FirstOrDefault(x => x.Type == AttributeDefinitionType.Engagement.ToString())?.Values ?? [];

        private List<Lookup> _lookups = null;
        protected List<Lookup> Lookups
        {
            get
            {
                _lookups ??= DataClient.LookupSearch(new SearchRequest { }).Data.ToList();
                return _lookups;
            }
        }
        protected List<LookupValue> TestedDropdowns =>
            Lookups?.FirstOrDefault(x => x.Type == LookupType.TestedDropdown.ToString())?.Values ?? [];

        public static string SourceType => EnumExtensions.GetDescription(SaltMiner.Core.Util.SourceType.Pentest);
        public static string AssetType => AssessmentType.Pen.ToString();
        public static string Instance => "PenTest";
    }
}