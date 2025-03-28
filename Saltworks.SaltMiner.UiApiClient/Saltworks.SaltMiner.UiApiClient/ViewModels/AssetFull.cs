using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly EngagementAssetDto
    public class AssetFull : UiModelBase
    {
        public string AppVersion { get; set; }

        public string AssetId { get; set; }

        [Required]
        public EngagementInfo Engagement { get; set; } = new();

        [Required]
        public string ScanId { get; set; }

        [DateValidation]
        public DateTime Timestamp { get; set; }

        public TextField Name { get; set; }

        public TextField Description { get; set; }

        public TextField VersionId { get; set; }

        public TextField Version { get; set; }

        public string Host { get; set; }

        public string Ip { get; set; }

        public string Scheme { get; set; }

        public int Port { get; set; }

        public bool IsSaltminerSource { get; set; }

        [Required]
        public string SourceId { get; set; }

        public bool IsProduction { get; set; }

        public bool IsRetired { get; set; }

        [Required]
        public string LastScanDaysPolicy { get; set; }

        public string InventoryAssetKey { get; set; }

        public List<TextField> Attributes { get; set; }

        public AssetFull()
        {
        }

        public AssetFull(Engagement engagement, FieldInfo fieldInfo)
        {
            Name = new(default, nameof(Name), fieldInfo, true);
            Description = new(default, nameof(Description), fieldInfo, true);
            VersionId = new(default, nameof(VersionId), fieldInfo, true);
            Version = new(default, nameof(Version), fieldInfo, true);
            Host = default;
            Ip = default;
            Scheme = default;
            Port = default;
            Attributes = fieldInfo.AttributeDefinitions.Select(ad => new TextField(default, ad.Name, fieldInfo, true, true)).ToList();
            SetEngagementInfo(engagement.Saltminer.Engagement);
        }

        /// <summary>
        /// Only for use with import, does not use field permissions
        /// </summary>
        public AssetFull(QueueAsset queueAsset, string appVersion)
        {
            AppVersion = appVersion;
            SetQueueAssetNonFieldInfo(queueAsset);
            Name = new() { Value = queueAsset.Saltminer.Asset.Name, Name = nameof(Name) };
            Description = new() { Value = queueAsset.Saltminer.Asset.Description, Name = nameof(Description) };
            VersionId = new() { Value = queueAsset.Saltminer.Asset.VersionId, Name = nameof(VersionId) };
            Version = new() { Value = queueAsset.Saltminer.Asset.Version, Name = nameof(Version) };
            Attributes = queueAsset.Saltminer.Asset.Attributes.ToAttributeFields();
            SetEngagementInfo(queueAsset.Saltminer.Engagement);
        }

        public AssetFull(QueueAsset queueAsset, string appVersion, FieldInfo fieldInfo)
        {
            AppVersion = appVersion;
            SetQueueAssetNonFieldInfo(queueAsset);
            Name = new(queueAsset.Saltminer.Asset.Name, nameof(Name), fieldInfo);
            Description = new(queueAsset.Saltminer.Asset.Description, nameof(Description), fieldInfo);
            VersionId = new(queueAsset.Saltminer.Asset.VersionId, nameof(VersionId), fieldInfo);
            Version = new(queueAsset.Saltminer.Asset.Version, nameof(Version), fieldInfo);
            Attributes = queueAsset.Saltminer.Asset.Attributes.ToAttributeFields(fieldInfo);
            SetEngagementInfo(queueAsset.Saltminer.Engagement);
        }

        public AssetFull(Asset asset, string appVersion, FieldInfo fieldInfo)
        {
            AppVersion = appVersion;
            Timestamp = asset.Timestamp;
            AssetId = asset.Id;
            SourceId = asset.Saltminer.Asset.SourceId;
            SetEngagementInfo(asset.Saltminer.Engagement);
            Name = new(asset.Saltminer.Asset.Name, nameof(Name), fieldInfo);
            Description = new(asset.Saltminer.Asset.Description, nameof(Description), fieldInfo);
            VersionId = new(asset.Saltminer.Asset.VersionId, nameof(VersionId), fieldInfo);
            Version = new(asset.Saltminer.Asset.Version, nameof(Version), fieldInfo);
            Host = asset.Saltminer.Asset.Host;
            Ip = asset.Saltminer.Asset.Ip;
            Scheme = asset.Saltminer.Asset.Scheme;
            Port = asset.Saltminer.Asset.Port;
            Attributes = asset.Saltminer.Asset.Attributes.ToAttributeFields(fieldInfo);
            IsProduction = asset.Saltminer.Asset.IsProduction;
            IsSaltminerSource = asset.Saltminer.Asset.IsSaltminerSource;
            IsRetired = asset.Saltminer.Asset.IsRetired;
            LastScanDaysPolicy = asset.Saltminer.Asset.LastScanDaysPolicy;
            InventoryAssetKey = asset.Saltminer.InventoryAsset.Key;
        }

        private void SetEngagementInfo(EngagementInfo engagement)
        {
            Engagement.Id = engagement.Id;
            Engagement.Name = engagement.Name;
            Engagement.Attributes = engagement.Attributes;
            Engagement.Customer = engagement.Customer;
            Engagement.PublishDate = engagement.PublishDate;
            Engagement.Subtype = engagement.Subtype;
        }

        private void SetQueueAssetNonFieldInfo(QueueAsset queueAsset)
        {
            Timestamp = queueAsset.Timestamp;
            AssetId = queueAsset.Id;
            ScanId = queueAsset.Saltminer.Internal.QueueScanId;
            SourceId = queueAsset.Saltminer.Asset.SourceId;
            Host = queueAsset.Saltminer.Asset.Host;
            Ip = queueAsset.Saltminer.Asset.Ip;
            Scheme = queueAsset.Saltminer.Asset.Scheme;
            Port = queueAsset.Saltminer.Asset.Port;
            IsProduction = queueAsset.Saltminer.Asset.IsProduction;
            IsSaltminerSource = queueAsset.Saltminer.Asset.IsSaltminerSource;
            IsRetired = queueAsset.Saltminer.Asset.IsRetired;
            LastScanDaysPolicy = queueAsset.Saltminer.Asset.LastScanDaysPolicy;
            InventoryAssetKey = queueAsset.Saltminer.InventoryAsset.Key;
        }

        public QueueAsset CloneRequest(Engagement engagement, string scanId, string assetType, string instance, string sourceType)
        {
            string id = Guid.NewGuid().ToString();
            return new QueueAsset
            {
                Id = id,
                Saltminer = new SaltMinerQueueAssetInfo
                {
                    Asset = new AssetInfoPolicy
                    {
                        Attributes = Attributes.ToDictionary(),
                        Description = Description.Value,
                        IsProduction = IsProduction,
                        IsSaltminerSource = IsSaltminerSource,
                        Name = Name.Value,
                        Instance = instance,
                        SourceType = sourceType,
                        SourceId = SourceId,
                        Version = Version.Value,
                        VersionId = VersionId.Value,
                        AssetType = assetType,
                        IsRetired = IsRetired,
                        LastScanDaysPolicy = LastScanDaysPolicy,
                        Host = Host,
                        Ip = Ip,
                        Port = Port,
                        Scheme = Scheme
                    },
                    Internal = new QueueAssetInternal
                    {
                        QueueScanId = scanId
                    },
                    Engagement = new EngagementInfo
                    {
                        Id = engagement.Id,
                        Name = engagement.Saltminer.Engagement.Name,
                        Subtype = engagement.Saltminer.Engagement.Subtype,
                        Customer = engagement.Saltminer.Engagement.Customer,
                        Attributes = engagement.Saltminer.Engagement.Attributes,
                        PublishDate = engagement.Saltminer.Engagement.PublishDate
                    },
                    InventoryAsset = new InventoryAssetKeyInfo
                    {
                        Key = InventoryAssetKey
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }

        public QueueAsset GetQueueAsset(string assetType, string instance, string sourceType)
        {
            return new QueueAsset
            {
                Id = AssetId,
                Saltminer = new SaltMinerQueueAssetInfo
                {
                    Asset = new AssetInfoPolicy
                    {
                        Attributes = Attributes.ToDictionary(t => t.Name, t => t.Value),
                        Description = Description.Value,
                        IsProduction = IsProduction,
                        IsSaltminerSource = IsSaltminerSource,
                        Name = Name.Value,
                        Instance = instance,
                        SourceType = sourceType,
                        SourceId = SourceId,
                        Version = Version.Value,
                        VersionId = VersionId.Value,
                        AssetType = assetType,
                        IsRetired = IsRetired,
                        LastScanDaysPolicy = LastScanDaysPolicy,
                        Host = Host,
                        Ip = Ip,
                        Port = Port,
                        Scheme = Scheme
                    },
                    Internal = new QueueAssetInternal
                    {
                        QueueScanId = ScanId
                    },
                    Engagement = new EngagementInfo
                    {
                        Id = Engagement.Id,
                        Name = Engagement.Name,
                        Subtype = Engagement.Subtype,
                        Customer = Engagement.Customer,
                        Attributes = Engagement.Attributes,
                        PublishDate = Engagement.PublishDate
                    },
                    InventoryAsset = new InventoryAssetKeyInfo
                    {
                        Key = InventoryAssetKey
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }

        public IssuePrimerAssetItem ToDropdownItem() => new(this);
    }
}
