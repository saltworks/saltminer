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

﻿using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Qualys
{
    #region General

    public interface IQualysDto { }

    public class AttributeDto
    {
        [XmlElement("NAME")]
        public string Name { get; set; }
        [XmlElement("VALUE")]
        public string Value { get; set; }
    }

    public class ApiCallWarningDto
    {
        [XmlElement("CODE")]
        public string Code { get; set; }
        [XmlElement("TEXT")]
        public string Text { get; set; }
        [XmlElement("URL")]
        public string Url { get; set; }
        public string NextCallMinId()
        {
            var pat = "&id_min=(\\d*)";
            if (Regex.IsMatch(Url, pat))
                return Regex.Match(Url, pat).Groups[0].Value;
            return "";
        }
    }

    #endregion

    #region Host List DTOs

    [XmlRoot("HOST_LIST_OUTPUT"), XmlType("HOST_LIST_OUTPUT")]
    public class HostListOutputDto : IQualysDto
    {
        [XmlElement("RESPONSE")]
        public HostListResponseDto Response { get; set; }
        [XmlElement("WARNING")]
        public ApiCallWarningDto Warning { get; set; }
        public string NextCallMinId => Warning?.NextCallMinId();
    }

    public class HostListResponseDto
    {
        [XmlElement("DATETIME")]
        public DateTime Timestamp { get; set; }
        [XmlArray("HOST_LIST"), XmlArrayItem("HOST", typeof(HostDto))]
        public List<HostDto> Hosts { get; set; }
    }

    public class HostBaseDto
    {
        [XmlElement("ID")]
        public string Id { get; set; }
        [XmlElement("ASSET_ID")]
        public string AssetId { get; set; }
        [XmlElement("IP")]
        public string Ip { get; set; }
        [XmlElement("TRACKING_METHOD")]
        public string TrackingMethod { get; set; }
        [XmlElement("NETWORK_ID")]
        public string NetworkId { get; set; }
        [XmlElement("DNS")]
        public string Dns { get; set; }
        [XmlElement("DNS_DATA")]
        public HostDnsDataDto DnsData { get; set; }
        [XmlElement("CLOUD_PROVIDER")]
        public string CloudProvider { get; set; }
        [XmlElement("CLOUD_SERVICE")]
        public string CloudService { get; set; }
        [XmlElement("CLOUD_RESOURCE_ID")]
        public string CloudResourceId { get; set; }
        [XmlElement("OS")]
        public string Os { get; set; }
        [XmlElement("QG_HOST_ID")]
        public string QgHostId { get; set; }
        [XmlElement("METADATA")]
        public HostMetadataDto Metadata { get; set; }
        [XmlElement("LAST_VM_SCANNED_DATE")]
        public DateTime? LastVmScan { get; set; }
        [XmlElement("LAST_VM_AUTH_SCANNED_DATE")]
        public DateTime? LastVmAuthScan { get; set; }
    }

    public class HostDto : HostBaseDto
    {
        [XmlElement("LAST_BOOT")]
        public DateTime? LastBoot { get; set; }
        [XmlElement("SERIAL_NUMBER")]
        public string SerialNumber { get; set; }
        [XmlElement("HARDWARE_UUID")]
        public string HardwareUuid { get; set; }
        [XmlElement("OWNER")]
        public string Owner { get; set; }
        [XmlElement("FIRST_FOUND_DATE")]
        public DateTime? FirstFoundDate { get; set; }
        [XmlElement("LAST_ACTIVITY")]
        public DateTime? LastActivity { get; set; }
        [XmlElement("AGENT_STATUS")]
        public string AgentStatus { get; set; }
        [XmlElement("CLOUD_AGENT_RUNNING_ON")]
        public string CloudAgentRunningOn { get; set; }
        [XmlElement("LAST_VULN_SCAN_DATETIME")]
        public DateTime? LastVulnScan { get; set; }
    }

    public class HostDnsDataDto
    {
        [XmlElement("HOSTNAME")]
        public string Hostname { get; set; }
        [XmlElement("DOMAIN")]
        public string Domain { get; set; }
        [XmlElement("FQDN")]
        public string Fqdn { get; set; }
    }

    public class HostMetadataDto
    {
        [XmlArray("AZURE"), XmlArrayItem("ATTRIBUTE")]
        public List<AttributeDto> AzureAttributes { get; set; }
        [XmlArray("EC2"), XmlArrayItem("ATTRIBUTE")]
        public List<AttributeDto> Ec2Attributes { get; set; }
        [XmlArray("GOOGLE"), XmlArrayItem("ATTRIBUTE")]
        public List<AttributeDto> GoogleAttributes { get; set; }
    }

    #endregion

    #region Scan List DTOs

    [XmlRoot("SCAN_LIST_OUTPUT"), XmlType("SCAN_LIST_OUTPUT")]
    public class ScanListOutputDto : IQualysDto
    {
        [XmlElement("RESPONSE")]
        public ScanListResponseDto Response { get; set; }
    }

    public class ScanListResponseDto
    {
        [XmlElement("DATETIME")]
        public DateTime ScanReportDate { get; set; }
        [XmlArray("SCAN_LIST"), XmlArrayItem("SCAN", typeof(ScanDto))]
        public List<ScanDto> Scans { get; set; }
    }

    public class ScanDto
    {
        [XmlElement("REF")]
        public string ReferenceId { get; set; }
        [XmlElement("TYPE")]
        public string Type { get; set; }
        [XmlElement("TITLE")]
        public string Title { get; set; }
        [XmlElement("USER_LOGIN")]
        public string User { get; set; }
        [XmlElement("LAUNCH_DATETIME")]
        public DateTime LaunchDate { get; set; }
        [XmlElement("DURATION")]
        public string Duration { get; set; }
        [XmlElement("PROCESSING_PRIORITY")]
        public string ProcessingPriority { get; set; }
        [XmlElement("PROCESSED")]
        public bool Processed { get; set; }
        [XmlElement("STATUS")]
        public StatusDto Status { get; set; }
        [XmlElement("TARGET")]
        public string Target { get; set; }
    }

    public class StatusDto
    {
        [XmlElement("STATE")]
        public string State { get; set; }
        [XmlElement("SUB_STATE")]
        public string SubState { get; set; }

    }

    #endregion

    #region Detection List DTOs

    [XmlRoot("HOST_LIST_VM_DETECTION_OUTPUT"), XmlType("HOST_LIST_VM_DETECTION_OUTPUT")]
    public class HostListVmDetectionDto : IQualysDto
    {
        [XmlElement("RESPONSE")]
        public HostListVmDetectionResponseDto Response { get; set; }
        [XmlElement("WARNING")]
        public ApiCallWarningDto Warning { get; set; }
        public string NextCallMinId => Warning?.NextCallMinId();
    }

    public class HostListVmDetectionResponseDto
    {
        [XmlElement("DATETIME")]
        public DateTime? Timestamp { get; set; }
        [XmlArray("HOST_LIST"), XmlArrayItem("HOST")]
        public List<HostDetectDto> Hosts { get; set; }
    }

    public class HostDetectDto : HostBaseDto
    {
        [XmlElement("LAST_SCAN_DATETIME")]
        public DateTime? LastScan { get; set; }
        [XmlArray("DETECTION_LIST"), XmlArrayItem("DETECTION")]
        public List<DetectionDto> Detections { get; set; }
    }

    public class DetectionDto
    {
        [XmlElement("UNIQUE_VULN_ID")]
        public string Id { get; set; }
        [XmlElement("QID")]
        public string Qid { get; set; }
        [XmlElement("TYPE")]
        public string Type { get; set; }
        [XmlElement("SEVERITY")]
        public int Severity { get; set; }
        [XmlElement("SSL")]
        public string Ssl { get; set; }
        [XmlElement("RESULTS")]
        public string Results { get; set; }
        /// <summary>
        /// Active/New/Re-Opened,Fixed
        /// </summary>
        [XmlElement("STATUS")]
        public string Status { get; set; }
        [XmlElement("FIRST_FOUND_DATETIME")]
        public DateTime? FirstFound { get; set; }
        [XmlElement("LAST_FOUND_DATETIME")]
        public DateTime? LastFound { get; set; }
        [XmlElement("TIMES_FOUND")]
        public int TimesFound { get; set; }
        [XmlElement("LAST_TEST_DATETIME")]
        public DateTime? LastTest { get; set; }
        [XmlElement("LAST_UPDATE_DATETIME")]
        public DateTime? LastUpdate { get; set; }
        [XmlElement("LAST_FIXED_DATETIME")]
        public DateTime? LastFixed { get; set; }
        [XmlElement("IS_IGNORED")]
        public int Ignored { get; set; }
        [XmlElement("IS_DISABLED")]
        public int Disabled { get; set; }
        [XmlElement("LAST_PROCESSED_DATETIME")]
        public DateTime? LastProcessed { get; set; }
    }

    #endregion

    #region Knowledge Base DTOs

    [XmlRoot("KNOWLEDGE_BASE_VULN_LIST_OUTPUT"), XmlType("KNOWLEDGE_BASE_VULN_LIST_OUTPUT")]
    public class KnowledgeBaseOuputDto : IQualysDto
    {
        [XmlElement("RESPONSE")]
        public KnowledgeBaseOutputResponseDto Response { get; set; }
    }

    public class KnowledgeBaseOutputResponseDto
    {
        [XmlElement("DATETIME")]
        public DateTime? Timestamp { get; set; }
        [XmlArray("VULN_LIST"), XmlArrayItem("VULN")]
        public List<KnowledgeBaseDto> Vulnerabilities { get; set; }
    }
    
    
    public class KnowledgeBaseDtoBase
    {
        [XmlElement("QID")]
        public int Qid { get; set; }
        [XmlElement("VULN_TYPE")]
        public string VulnType { get; set; }
        [XmlElement("SEVERITY_LEVEL")]
        public int Severity { get; set; }
        [XmlElement("TITLE")]
        public string Title { get; set; }
        [XmlElement("CATEGORY")]
        public string Category { get; set; }
        [XmlElement("PCI_FLAG")]
        public bool PciFlag { get; set; }
        [XmlElement("DIAGNOSIS")]
        public string Diagnosis { get; set; }
        [XmlElement("SOLUTION")]
        public string Solution { get; set; }
    }

    public class KnowledgeBaseMiniDto : KnowledgeBaseDtoBase
    {
        public List<string> CveIdList { get; set; }
    }

    public class KnowledgeBaseDto : KnowledgeBaseDtoBase
    {
        [XmlElement("LAST_SERVICE_MODIFICATION_DATETIME")]
        public DateTime LastServiceModification { get; set; }
        [XmlElement("PUBLISHED_DATETIME")]
        public DateTime Published { get; set; }
        [XmlElement("CODE_MODIFIED_DATETIME")]
        public DateTime CodeModified { get; set; }
        [XmlArray("BUGTRAQ_ID_LIST"), XmlArrayItem("BUGTRAQ_ID")]
        public List<BugtraqIdDto> BugtraqIdList { get; set; }
        [XmlArray("CVE_LIST"), XmlArrayItem("CVE")]
        public List<CveIdDto> CveIdList { get; set; }
        [XmlElement("PATCHABLE")]
        public string Patchable { get; set; }
        public static string NodeName { get => "VULN_DETAILS"; }
        public static string EnclosingNodeName { get => "VULN_DETAILS_LIST"; }
        public KnowledgeBaseMiniDto ToMiniDto()
        {
            return new KnowledgeBaseMiniDto()
            {
                Category = Category,
                CveIdList = CveIdList.Select(cve => cve.Id).ToList(),
                PciFlag = PciFlag,
                Qid = Qid,
                Severity = Severity,
                Title = Title,
                VulnType = VulnType,
                Solution = Solution,
                Diagnosis = Diagnosis
            };
        }
    }

    public class BugtraqIdDto
    {
        [XmlElement("ID")]
        public string Id { get; set; }
        [XmlElement("URL")]
        public string Url { get; set; }
    }

    public class CveIdDto
    {
        [XmlElement("ID")]
        public string Id { get; set; }
        [XmlElement("URL")]
        public string Url { get; set; }
    }
        
    #endregion

    #region LaunchReportSimpleReturnDTO's

    [XmlRoot("SIMPLE_RETURN")]
    public class SimpleReturnDto
    {
        [XmlElement("RESPONSE", typeof(SimpleReturnResponseDto))]
        public SimpleReturnResponseDto Response { get; set; }
    }

    public class SimpleReturnResponseDto
    {
        [XmlElement("DATETIME")]
        public DateTime Timestamp { get; set; }
        [XmlElement("TEXT")]
        public string Text { get; set; }
        [XmlArray("ITEM_LIST")]
        [XmlArrayItem("ITEM", typeof(ItemDto))]
        public List<ItemDto> ItemList { get; set; }
        [XmlElement("CODE")]
        public int Code { get; set; }
    }

    public class ItemDto
    {
        [XmlElement("KEY")]
        public string Key { get; set; }
        [XmlElement("VALUE")]
        public string ItemValue { get; set; }
    }

    #endregion

    /// <summary>
    /// Represents the final dictionary entry for a scan
    /// </summary>
    public class ScanListItem
    {
        public DateTime ScanDate { get; set; }
        public string ScanId { get; set; }
        public List<string> Targets { get; set; }
        /// <summary>
        /// Used by adapter, not pulled from source
        /// </summary>
        public string Id { get; set; } = null;

        public static ScanListItem FromScan(ScanDto scan)
        {
            return new ScanListItem()
            {
                ScanId = scan.ReferenceId,
                ScanDate = scan.LaunchDate,
                Targets = scan.Target.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }
    }
}