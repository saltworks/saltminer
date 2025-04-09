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

﻿using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.WebInspect
{
    public class Report
    {
        public Report()
        {
            Sessions = new List<SessionDTO>();
        }

        public SourceMetric SourceMetric { get; set; }
        public List<SessionDTO> Sessions { get; set; }
    }

    #region FPR DTO's

    [XmlRoot("Session"), XmlType("Session")]
    public class SessionDTO
    {
        [XmlAttribute("requestId")]
        public string RequestId { get; set; }
        [XmlElement("URL")]
        public string Url { get; set; }

        [XmlElement("Scheme")]
        public string Scheme { get; set; }

        [XmlElement("Host")]
        public string Host { get; set; }

        [XmlElement("Port")]
        public string Port { get; set; }

        [XmlElement("AttackParamDescriptor")]
        public string AttackParamDescriptor { get; set; }

        [XmlArray("Issues")]
        [XmlArrayItem("Issue")]
        public List<SessionIssueDTO> Issues { get; set; }

        [XmlElement("Response")]
        public SessionResponseDTO Response { get; set; }

        public static string NodeName { get => "Session"; }
    }

    public class SessionIssueDTO
    {
        [XmlElement("CheckTypeID")]
        public string CheckTypeID { get; set; }

        [XmlElement("EngineType")]
        public string EngineType { get; set; }

        [XmlElement("VulnerabilityID")]
        public string VulnerabilityID { get; set; }

        [XmlElement("Severity")]
        public string Severity { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlArray("Classifications")]
        [XmlArrayItem("Classification")]
        public List<string> Classifications { get; set; }

        [XmlArray("ReproSteps")]
        [XmlArrayItem("ReproStep")]
        public List<SessionReproStepDTO> ReproSteps { get; set; }

        //[XmlArray("ReportSection")]
        //[XmlArrayItem("ReportSection")]
        //public SessionReportSection ReportSection { get; set; }
    }

    public class SessionReproStepDTO
    {
        [XmlElement("Url")]
        public string Url { get; set; }

        [XmlElement("Source")]
        public string Source { get; set; }
    }

    public class SessionReportSectionDTO
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("SectionText")]
        public string SectionText { get; set; }
    }

    public class SessionResponseDTO
    {

        [XmlArray("Headers")]
        [XmlArrayItem("Header")]
        public List<SessionResponseHeadersDTO> Headers { get; set; }
    }

    public class SessionResponseHeadersDTO
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }
    }

    #endregion 

    #region SCAN DTO's

    public class ScanDTO
    {
        [XmlElement("ScanID")]
        public string ScanId { get; set; }

        [XmlElement("ScanName")]
        public string ScanName { get; set; }

        [XmlElement("AppVersion")]
        public string AppVersion { get; set; }

        [XmlElement("PolicyID")]
        public string PolicyID { get; set; }

        [XmlElement("ParentScanID")]
        public string ParentScanID { get; set; }

        [XmlArray("CounterDataXml")]
        [XmlArrayItem("counter")]
        public List<ScanCountDTO> Counts { get; set; }

        [XmlElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [XmlElement("MachineName")]
        public string MachineName { get; set; }

        [XmlElement("PolicyName")]
        public string PolicyName { get; set; }

        [XmlElement("PolicyUniqueID")]
        public string PolicyUniqueID { get; set; }

        [XmlElement("CreatedRootScanIDDate")]
        public string RootScanID { get; set; }

        [XmlElement("ScanDate")]
        public DateTime ScanDate { get; set; }

        [XmlElement("StartUrl")]
        public string StartUrl { get; set; }

        [XmlElement("Status")]
        public int Status { get; set; }

        [XmlArray("Servers")]
        [XmlArrayItem("Server")]
        public List<ScanCountDTO> Servers { get; set; }

        [XmlArray("Responses")]
        [XmlArrayItem("Response")]
        public List<ScanCountDTO> Responses { get; set; }

        [XmlArray("ServerTypes")]
        [XmlArrayItem("ServerType")]
        public List<ScanServerTypeDTO> ServerTypes { get; set; }

        [XmlArray("SessionServerTypes")]
        [XmlArrayItem("SessionServerType")]
        public List<ScanSessionServerTypeDTO> SessionServerTypes { get; set; }

        [XmlArray("SessionLinks")]
        [XmlArrayItem("SessionLink")]
        public List<ScanSessionLinkDTO> SessionLinks { get; set; }

        [XmlArray("Session_Annotations")]
        [XmlArrayItem("Session_Annotation")]
        public List<ScanSessionAnnotationDTO> SessionAnnotations { get; set; }

        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        public List<ScanPropertyDTO> Properties { get; set; }

        [XmlArray("PropertyImages")]
        [XmlArrayItem("PropertyImage")]
        public List<ScanPropertyImageDTO> PropertyImages { get; set; }

        [XmlArray("PropertyDetail2s")]
        [XmlArrayItem("PropertyDetail2")]
        public List<ScanPropertyDetail2DTO> PropertyDetail2s { get; set; }

        [XmlArray("Session_Properties")]
        [XmlArrayItem("Session_Property")]
        public List<ScanSessionPropertyDTO> SessionProperties { get; set; }

        [XmlArray("Server_Properties")]
        [XmlArrayItem("Server_Property")]
        public List<ScanServerPropertyDTO> ServerProperties { get; set; }

        [XmlArray("ServerType_Properties")]
        [XmlArrayItem("ServerType_Property")]
        public List<ScanServerTypePropertyDTO> ServerTypeProperties { get; set; }

        [XmlArray("Scan_Properties")]
        [XmlArrayItem("Scan_Property")]
        public List<ScanScanPropertyDTO> ScanProperties { get; set; }

        [XmlArray("SessionParameterReflections")]
        [XmlArrayItem("SessionParameterReflection")]
        public List<ScanSessionParameterReflectionDTO> SessionParameterReflections { get; set; }

        [XmlArray("PathHitCounts")]
        [XmlArrayItem("PathHitCount")]
        public List<ScanPathHitCountDTO> PathHitCounts { get; set; }

        [XmlArray("ScanAuditEngines")]
        [XmlArrayItem("ScanAuditEngine")]
        public List<ScanScanAuditEngineDTO> ScanAuditEngines { get; set; }

        [XmlArray("ScanEnabledChecks")]
        [XmlArrayItem("ScanEnabledCheck")]
        public List<ScanScanEnabledCheckDTO> ScanEnabledChecks { get; set; }

        [XmlArray("ScanEnabledCustomChecks")]
        [XmlArrayItem("ScanEnabledCustomCheck")]
        public List<ScanScanEnabledCustomCheckDTO> ScanEnabledCustomChecks { get; set; }

        [XmlArray("ScanEnabledCustomAgents")]
        [XmlArrayItem("ScanEnabledCustomAgent")]
        public List<ScanScanEnabledCustomAgentDTO> ScanEnabledCustomAgents { get; set; }

        [XmlArray("ScanLogs")]
        [XmlArrayItem("ScanLog")]
        public List<ScanScanLogDTO> ScanLogs { get; set; }

        [XmlArray("Hashs")]
        [XmlArrayItem("Hash")]
        public List<ScanHashDTO> Hashs { get; set; }

        [XmlArray("Recommendations")]
        [XmlArrayItem("Recommendation")]
        public List<ScanRecommendationDTO> Recommendations { get; set; }

        [XmlArray("Instrumentations")]
        [XmlArrayItem("Instrumentation")]
        public List<ScanInstrumentationDTO> Instrumentations { get; set; }

        [XmlArray("FortifyServers")]
        [XmlArrayItem("FortifyServer")]
        public List<ScanFortifyServerDTO> FortifyServers { get; set; }

        [XmlArray("SessionCorrelationIDs")]
        [XmlArrayItem("SessionCorrelationID")]
        public List<ScanSessionCorrelationIDDTO> SessionCorrelationIDs { get; set; }

        [XmlArray("SessionParameters")]
        [XmlArrayItem("SessionParameter")]
        public List<ScanSessionParameterDTO> SessionParameters { get; set; }

        [XmlArray("ShadowSessions")]
        [XmlArrayItem("ShadowSession")]
        public List<ScanShadowSessionDTO> ShadowSessions { get; set; }

        [XmlArray("ShadowSessionCheckFounds")]
        [XmlArrayItem("ShadowSessionCheckFound")]
        public List<ScanShadowSessionCheckFoundDTO> ShadowSessionCheckFounds { get; set; }

        [XmlArray("ShadowSessionCorrelationIDs")]
        [XmlArrayItem("ShadowSessionCorrelationID")]
        public List<ScanShadowSessionCorrelationIDDTO> ShadowSessionCorrelationIDs { get; set; }

        [XmlArray("ShadowSessionCheckFoundCorrelationIDs")]
        [XmlArrayItem("ShadowSessionCheckFoundCorrelationID")]
        public List<ScanShadowSessionCheckFoundCorrelationIDDTO> ShadowSessionCheckFoundCorrelationIDs { get; set; }

        [XmlArray("DataStoreOperationsList")]
        [XmlArrayItem("DataStoreOperation")]
        public List<ScanDataStoreOperationDTO> DataStoreOperationsList { get; set; }

        [XmlArray("DataStoreDetailsList")]
        [XmlArrayItem("DataStoreDetail")]
        public List<ScanDataStoreDetailDTO> DataStoreDetailsList { get; set; }

        [XmlArray("ScanStatisticsList")]
        [XmlArrayItem("ScanStatistic")]
        public List<ScanScanStatisticDTO> ScanStatisticsList { get; set; }

        [XmlArray("CSRFParameters")]
        [XmlArrayItem("CSRFParameter")]
        public List<ScanCSRFParameterDTO> CSRFParameters { get; set; }

        [XmlArray("SessionJSLibFingerprints")]
        [XmlArrayItem("SessionJSLibFingerprint")]
        public List<ScanSessionJSLibFingerprintDTO> SessionJSLibFingerprints { get; set; }

        [XmlArray("Graphs")]
        [XmlArrayItem("Graph")]
        public List<ScanGraphDTO> Graphs { get; set; }

        [XmlArray("GraphEdges")]
        [XmlArrayItem("GraphEdge")]
        public List<ScanGraphEdgeDTO> GraphEdges { get; set; }

        [XmlArray("GraphNodes")]
        [XmlArrayItem("GraphNode")]
        public List<ScanGraphNodeDTO> GraphNodes { get; set; }

        public static string NodeName { get => "Scan"; }
        public static string EnclosingNodeName { get => "Scans"; }

        public SourceMetric GetSourceMetric(WebInspectConfig config)
        {
            return new SourceMetric
            {
                LastScan = CreatedDate,
                Instance = config.Instance,
                IsSaltminerSource = WebInspectConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = $"{ScanId}|{AppVersion}",
                VersionId = AppVersion,
                Attributes = new Dictionary<string, string>()
            };
        }
    }

    public class ScanCountDTO
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("secondaryName")]
        public string SecondaryName { get; set; }

        [XmlElement("total")]
        public int Total { get; set; }
    }

    public class ScanServerDTO
    {
        [XmlElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [XmlElement("LastUpdatedDate")]
        public DateTime LastUpdatedDate { get; set; }

        [XmlElement("Sequence")]
        public int Sequence { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("ServerID")]
        public string ServerID { get; set; }

        [XmlElement("Host")]
        public int Host { get; set; }

        [XmlElement("Port")]
        public string Port { get; set; }

        [XmlElement("Scheme")]
        public string Scheme { get; set; }

        [XmlElement("Certificate")]
        public int Certificate { get; set; }

        [XmlElement("RejectReasons")]
        public int RejectReasons { get; set; }
    }

    public class ScanResponseDTO
    {
        [XmlElement("Sequence")]
        public string Sequence { get; set; }

        [XmlElement("ResponseID")]
        public string ResponseID { get; set; }

        [XmlArray("Sessions")]
        [XmlArrayItem("Session")]
        public List<ScanResponseSessionDTO> Sessions { get; set; }
    }

    public class ScanResponseSessionDTO
    {
        [XmlElement("AttackDefinition")]
        public string AttackDefinition { get; set; }

        [XmlElement("AttackDescriptor")]
        public ScanResponseSessionAttackDescriptorDTO AttackDescriptor { get; set; }
    }

    public class ScanResponseSessionAttackDescriptorDTO
    {
        [XmlElement("AttackDescriptor")]
        public string AttackDescriptor { get; set; }
    }

    public class ScanServerTypeDTO
    {
        [XmlElement("ChangedFlag")]
        public string ChangedFlag { get; set; }

        [XmlElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [XmlElement("LastUpdatedDate")]
        public DateTime LastUpdatedDate { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("ServerID")]
        public string ServerID { get; set; }

        [XmlElement("ServerTypeID")]
        public string ServerTypeID { get; set; }

        [XmlElement("Sequence")]
        public string Sequence { get; set; }

        [XmlElement("OriginatingSessionID")]
        public string OriginatingSessionID { get; set; }
    }

    public class ScanSessionLinkDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("ParentSessionID")]
        public string ParentSessionID { get; set; }

        [XmlElement("ChildSessionID")]
        public string ChildSessionID { get; set; }

        [XmlElement("Sequence")]
        public string Sequence { get; set; }

        [XmlElement("RelativeURL")]
        public string RelativeURL { get; set; }

        [XmlElement("SessionSubtype")]
        public int SessionSubtype { get; set; }

        [XmlElement("RejectRequestReasons")]
        public int RejectRequestReasons { get; set; }
    }

    public class ScanPropertyDTO
    {
        [XmlElement("HashValue")]
        public string HashValue { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("PropertyID")]
        public string PropertyID { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }

        [XmlElement("LastUpdatedDate")]
        public DateTime LastUpdatedDate { get; set; }
    }

    public class ScanPropertyDetail2DTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("PropertyDetailID")]
        public string PropertyDetailID { get; set; }

        [XmlElement("PropertyID")]
        public string PropertyID { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; }
    }

    public class ScanScanPropertyDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("PropertyID")]
        public string PropertyID { get; set; }
    }

    public class ScanPathHitCountDTO
    {
        [XmlElement("HitCount")]
        public int HitCount { get; set; }

        [XmlElement("PathHash")]
        public string PathHash { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("Sequence")]
        public string Sequence { get; set; }
    }

    public class ScanScanAuditEngineDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("OriginatingEngineID")]
        public string OriginatingEngineID { get; set; }

        [XmlElement("DisplayName")]
        public string DisplayName { get; set; }
    }

    public class ScanScanEnabledCheckDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("CheckID")]
        public string CheckID { get; set; }
    }

    public class ScanScanLogDTO
    {
        [XmlElement("MessageException")]
        public string MessageException { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("Sequence")]
        public string Sequence { get; set; }

        [XmlElement("DateTime")]
        public DateTime DateTime { get; set; }

        [XmlElement("MessageLevel")]
        public string MessageLevel { get; set; }

        [XmlElement("MessageType")]
        public string MessageType { get; set; }

        [XmlElement("MessageText")]
        public string MessageText { get; set; }
    }

    public class ScanHashDTO
    {
        [XmlElement("Data")]
        public string Data { get; set; }

        [XmlElement("EngineID")]
        public string EngineID { get; set; }

        [XmlElement("HashValue")]
        public string HashValue { get; set; }

        [XmlElement("PendingHashValue")]
        public string PendingHashValue { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("SmartMode")]
        public string SmartMode { get; set; }

        [XmlElement("SubType1")]
        public string SubType1 { get; set; }

        [XmlElement("SubType2")]
        public string SubType2 { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("TypeName")]
        public string TypeName { get; set; }
    }

    public class ScanSessionCorrelationIDDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("SessionID")]
        public string SessionID { get; set; }

        [XmlElement("ProviderID")]
        public string ProviderID { get; set; }

        [XmlElement("CorrelationID")]
        public string CorrelationID { get; set; }

        [XmlElement("HashString")]
        public string HashString { get; set; }
    }

    public class ScanSessionParameterDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("SessionID")]
        public string SessionID { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Location")]
        public string Location { get; set; }

        [XmlElement("ParameterIndex")]
        public string ParameterIndex { get; set; }

        [XmlElement("SubName")]
        public string SubName { get; set; }

        [XmlElement("SubLocation")]
        public string SubLocation { get; set; }

        [XmlElement("ParameterSubIndex")]
        public string ParameterSubIndex { get; set; }

        [XmlElement("IsAttack")]
        public bool IsAttack { get; set; }

        [XmlElement("IsState")]
        public bool IsState { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }
    }

    public class ScanScanStatisticDTO
    {
        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("SubSystem")]
        public string SubSystem { get; set; }

        [XmlElement("SmartMode")]
        public string SmartMode { get; set; }

        [XmlElement("Region")]
        public string Region { get; set; }

        [XmlElement("Service")]
        public string Service { get; set; }

        [XmlElement("ThreadType1")]
        public string ThreadType1 { get; set; }

        [XmlElement("ThreadType2")]
        public string ThreadType2 { get; set; }

        [XmlElement("SessionID1")]
        public string SessionId1 { get; set; }

        [XmlElement("SessionID2")]
        public string SessionId2 { get; set; }

        [XmlElement("EngineID1")]
        public string EngineId1 { get; set; }

        [XmlElement("EngineID2")]
        public string EngineId2 { get; set; }

        [XmlElement("IntID1")]
        public string IntId1 { get; set; }

        [XmlElement("FirstStartTime")]
        public DateTime FirstStartTime { get; set; }

        [XmlElement("LastStopTime")]
        public DateTime LastStopTime { get; set; }

        [XmlElement("Ticks")]
        public int Ticks { get; set; }

        [XmlElement("Count")]
        public int Count { get; set; }

        [XmlElement("ExtraData")]
        public string ExtraData { get; set; }
    }

    public class ScanGraphDTO
    {
        [XmlElement("ID")]
        public string ID { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("ScanID")]
        public string ScanID { get; set; }

        [XmlElement("Sequence")]
        public string Sequence { get; set; }

        [XmlElement("Data")]
        public string Data { get; set; }
    }

    // Need to find structures
    public class ScanSessionServerTypeDTO
    {

    }

    public class ScanSessionAnnotationDTO
    {

    }

    public class ScanPropertyImageDTO
    {

    }
    public class ScanSessionPropertyDTO
    {

    }

    public class ScanServerPropertyDTO
    {

    }

    public class ScanServerTypePropertyDTO
    {

    }

    public class ScanSessionParameterReflectionDTO
    {

    }

    public class ScanScanEnabledCustomCheckDTO
    {

    }

    public class ScanScanEnabledCustomAgentDTO
    {

    }

    public class ScanRecommendationDTO
    {

    }

    public class ScanInstrumentationDTO
    {

    }

    public class ScanFortifyServerDTO
    {

    }

    public class ScanShadowSessionDTO
    {

    }

    public class ScanShadowSessionCheckFoundDTO
    {

    }

    public class ScanShadowSessionCorrelationIDDTO
    {

    }

    public class ScanShadowSessionCheckFoundCorrelationIDDTO
    {

    }

    public class ScanDataStoreOperationDTO
    {

    }

    public class ScanDataStoreDetailDTO
    {

    }

    public class ScanCSRFParameterDTO
    {

    }

    public class ScanSessionJSLibFingerprintDTO
    {

    }

    public class ScanGraphEdgeDTO
    {

    }

    public class ScanGraphNodeDTO
    {

    }

    #endregion
}