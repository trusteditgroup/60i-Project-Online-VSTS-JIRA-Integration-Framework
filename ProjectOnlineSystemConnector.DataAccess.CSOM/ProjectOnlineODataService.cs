#region License

/* 
 * Copyright © 2017 Yaroslav Boychenko.
 * Copyright © 2017 TrustedIt Group. Contacts: mailto:60i@trusteditgroup.com
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by Trusted IT Group Inc.
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program.
 * If not, see https://www.trusteditgroup.com/60ilicense
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;
using NLog;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.DataModel.OData;

namespace ProjectOnlineSystemConnector.DataAccess.CSOM
{
    public class ProjectOnlineODataService
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const int GetODataTaskMaxQueryLength = 1500;
        private const int GetODataTaskMax = 100;
        private const int RequestTimeout = 2700 * 1000; //Timeout.Infinite;

        private readonly SecureString securePassword;
        public bool IsOnline { get; }
        public string ProjectOnlineUrl { get; }
        public string ProjectOnlineUserName { get; }
        public string ProjectOnlinePassword { get; }

        public ProjectOnlineODataService(string projectOnlineUrl, string projectOnlineUserName,
            string projectOnlinePassword, bool isOnline)
        {
            ProjectOnlineUrl = projectOnlineUrl;
            ProjectOnlineUserName = projectOnlineUserName;
            ProjectOnlinePassword = projectOnlinePassword;
            IsOnline = isOnline;
            securePassword = new SecureString();
            foreach (char c in projectOnlinePassword)
            {
                securePassword.AppendChar(c);
            }
        }

        #region Common OData

        public List<TODataEntity> GetOdataList<TODataEntity>(string odataUrl,
            Func<IEnumerable<XElement>, TODataEntity, bool> fillValues) where TODataEntity : class, new()
        {
            List<TODataEntity> resultList = new List<TODataEntity>();
            int skip = 0;
            while (true)
            {
                string concatSymbol = odataUrl.Contains("?") ? "&" : "?";
                string tempOdataUrl = $"{odataUrl}{concatSymbol}$skip={skip}&$top={GetODataTaskMax}";
                logger.Info($"GetOdataList {tempOdataUrl}");
                string response = ExecuteRequest(tempOdataUrl);
                List<TODataEntity> tempList = ParseOdataListResponse(response, fillValues);
                logger.Info($"GetOdataList tempList: {tempList.Count};");
                if (tempList.Count == 0)
                {
                    break;
                }
                resultList.AddRange(tempList);
                skip += GetODataTaskMax;
            }
            return resultList;
        }

        private string ExecuteRequest(string odataUrl)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(odataUrl);

            if (IsOnline)
            {
                httpWebRequest.Credentials = new SharePointOnlineCredentials(ProjectOnlineUserName, securePassword);
            }
            else
            {
                httpWebRequest.Credentials = new NetworkCredential(ProjectOnlineUserName, ProjectOnlinePassword);
            }
            //httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
            httpWebRequest.Timeout = RequestTimeout;
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream responseStream = httpWebResponse.GetResponseStream();
            if (responseStream != null)
            {
                StreamReader streamReader = new StreamReader(responseStream);
                string response = streamReader.ReadToEnd();
                return response;
            }
            return null;
        }

        private List<TODataEntity> ParseOdataListResponse<TODataEntity>(string response,
            Func<IEnumerable<XElement>, TODataEntity, bool> fillValues) where TODataEntity : class, new()
        {
            logger.Info("ParseOdataListResponse");
            List<TODataEntity> oDataList = new List<TODataEntity>();
            if (!String.IsNullOrEmpty(response))
            {
                XDocument document = XDocument.Parse(response);
                if (document.Root != null)
                {
                    IEnumerable<XElement> entryElements = document.Root.Elements()
                        .Where(x => x.Name.LocalName == "entry");
                    foreach (XElement entryElement in entryElements)
                    {
                        XElement contentElement = entryElement.Elements()
                            .FirstOrDefault(x => x.Name.LocalName == "content");
                        XElement propertiesElement = contentElement?.Elements()
                            .FirstOrDefault(x => x.Name.LocalName == "properties");
                        if (propertiesElement != null)
                        {
                            IEnumerable<XElement> propertiesElements = propertiesElement.Elements();

                            var oDataEntity = new TODataEntity();
                            fillValues(propertiesElements, oDataEntity);
                            oDataList.Add(oDataEntity);
                        }
                    }
                }
            }
            return oDataList;
        }

        private TODataEntity GetOdataEntity<TODataEntity>(string odataUrl,
            Func<IEnumerable<XElement>, TODataEntity, bool> fillValues) where TODataEntity : class, new()
        {
            logger.Info($"GetOdataEntity {odataUrl}");
            List<TODataEntity> list = GetOdataList(odataUrl, fillValues);
            if (list.Count == 0)
            {
                return new TODataEntity();
            }
            return list[0];
        }

        private List<string> GetFilterQueries(int systemId, List<string> issueKeys)
        {
            logger.Info($"GetFilterQueries systemId: {systemId}; issueKeys: {issueKeys.Count};");

            List<string> resultQueries = new List<string>();
            int index = 0;
            resultQueries.Add(String.Empty);
            foreach (string key in issueKeys)
            {
                string tempQuery = resultQueries[index] + "," + key;
                if (tempQuery.Length <= GetODataTaskMaxQueryLength)
                {
                    resultQueries[index] = tempQuery;
                }
                else
                {
                    index++;
                    resultQueries.Add(String.Empty);
                    resultQueries[index] = resultQueries[index] + "," + key;
                }
            }
            return resultQueries;
        }

        #endregion

        #region ODataTask

        private readonly string getODataTaskSelectQuery =
            $"ProjectId,TaskId,ParentTaskId,TaskName,{ProjectServerConstants.SystemId}," +
            $"{ProjectServerConstants.IssueKey},{ProjectServerConstants.IssueId}," +
            $"{ProjectServerConstants.ParentEpicKey}," +
            $"{ProjectServerConstants.ParentVersionId},{ProjectServerConstants.IssueTypeName}";

        public List<ODataTask> GetODataTasks(Guid projectId)
        {
            string odataUrl = ProjectOnlineUrl + $"/_api/ProjectData/Tasks?$select={getODataTaskSelectQuery}&$filter=not(ParentTaskId eq null) and ProjectId eq guid'{projectId}'";
            return GetOdataList<ODataTask>(odataUrl, FillODataEntityValues);
        }

        public List<ODataTask> GetODataTasks(Guid projectId, int systemId, List<string> issueKeys)
        {
            logger.Info($"GetODataTasks projectId: {projectId}; systemId: {systemId}; issueKeys: {issueKeys.Count};");

            List<string> resultQueries = GetFilterQueries(systemId, issueKeys);
            List<ODataTask> resultList = GetODataTasks(resultQueries, projectId, systemId);
            return resultList;
        }

        private List<ODataTask> GetODataTasks(List<string> resultQueries, Guid projectId, int systemId)
        {
            List<ODataTask> resultList = new List<ODataTask>();

            foreach (string resultQuery in resultQueries)
            {
                string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Tasks?$select={getODataTaskSelectQuery}" +
                                  $"&$filter=not(ParentTaskId eq null) and ProjectId eq guid'{projectId}' " +
                                  $"and {ProjectServerConstants.SystemId} eq {systemId} " +
                                  $"and substringof({ProjectServerConstants.IssueKey},'{resultQuery}') eq true";
                resultList.AddRange(GetOdataList<ODataTask>(odataUrl, FillODataEntityValues));
            }
            return resultList;
        }

        #endregion

        #region ODataAssignment

        private readonly string getODataAssignmentSelectQuery =
            $"ProjectId,TaskId,AssignmentId,ResourceId,TaskName,{ProjectServerConstants.SystemIdT}," +
            $"{ProjectServerConstants.IssueKeyT},{ProjectServerConstants.IssueIdT}," +
            $"{ProjectServerConstants.ParentEpicKeyT},{ProjectServerConstants.ParentVersionIdT}," +
            $"{ProjectServerConstants.IssueTypeNameT}";

        public List<ODataAssignment> GetODataAssignments(int systemId, List<string> issueKeys)
        {
            logger.Info($"GetODataAssignments systemId: {systemId}; issueKeys: {issueKeys.Count};");

            List<string> resultQueries = GetFilterQueries(systemId, issueKeys);
            List<ODataAssignment> resultList = GetODataAssignments(resultQueries, systemId);
            return resultList;
        }

        public List<ODataAssignment> GetODataAssignments(int systemId)
        {
            logger.Info($"GetODataAssignments systemId: {systemId};");

            string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Assignments?" +
                              $"$select={getODataAssignmentSelectQuery}" +
                              $"&$filter={ProjectServerConstants.SystemIdT} eq {systemId}";
            logger.Info($"GetODataAssignments {odataUrl}");
            return GetOdataList<ODataAssignment>(odataUrl, FillODataEntityValues);
        }

        private List<ODataAssignment> GetODataAssignments(List<string> resultQueries, int systemId)
        {
            List<ODataAssignment> resultList = new List<ODataAssignment>();

            foreach (string resultQuery in resultQueries)
            {
                string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Assignments?$select={getODataAssignmentSelectQuery}" +
                                  $"&$filter={ProjectServerConstants.SystemIdT} eq {systemId} " +
                                  $"and substringof({ProjectServerConstants.IssueKeyT},'{resultQuery}') eq true";
                resultList.AddRange(GetOdataList<ODataAssignment>(odataUrl, FillODataEntityValues));
            }
            return resultList;
        }

        public List<ODataAssignment> GetODataAssignments(Guid projectId)
        {
            string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Assignments?" +
                              $"$select={getODataAssignmentSelectQuery}" +
                              $"&$filter=ProjectId eq guid'{projectId}'";
            logger.Info($"GetODataAssignments {odataUrl}");
            return GetOdataList<ODataAssignment>(odataUrl, FillODataEntityValues);
        }

        #endregion

        #region ODataAssignmentTimephasedDataRecord

        private readonly string getODataAssignmentTimephasedDataRecordSelectQuery =
            "ProjectId,AssignmentId,TimeByDay,AssignmentActualWork,TaskId,ResourceId";

        public List<ODataAssignmentTimephasedDataRecord> GetODataAssignmentTimephasedDataRecords(DateTime startDate, DateTime endDate)
        {
            string odataUrl = ProjectOnlineUrl + $"/_api/ProjectData/AssignmentTimephasedDataSet?$select={getODataAssignmentTimephasedDataRecordSelectQuery}" +
                              $"&$filter=TimeByDay ge datetime'{startDate}' and TimeByDay le datetime'{endDate}' and AssignmentActualWork ne 0";
            return GetOdataList<ODataAssignmentTimephasedDataRecord>(odataUrl, FillODataEntityValues);
        }

        #endregion

        #region ODataLookup

        public ODataLookupTable GetODataLookupTable(string tableName)
        {
            string odataUrl = ProjectOnlineUrl + $"/_api/ProjectServer/LookupTables?$filter=Name eq '{tableName}'";
            logger.Info($"GetODataLookupTable {odataUrl}");
            return GetOdataEntity<ODataLookupTable>(odataUrl, FillODataEntityValues);
        }

        public List<ODataLookupTableEntry> GetODataLookupTableEntries(ODataLookupTable lookupTable)
        {
            if (lookupTable.Id == Guid.Empty)
            {
                return new List<ODataLookupTableEntry>();
            }
            string odataUrl = ProjectOnlineUrl + $"/_api/ProjectServer/LookupTables('{lookupTable.Id}')/Entries";
            logger.Info($"GetODataLookupTableEntries {odataUrl}");
            return GetOdataList<ODataLookupTableEntry>(odataUrl, FillODataEntityValues);
        }

        public ODataLookupTable GetODataLookupTableWithEntries(string tableName)
        {
            logger.Info($"GetODataLookupTableWithEntries {tableName}");
            ODataLookupTable lookupTable = GetODataLookupTable(tableName);
            lookupTable.Entries = GetODataLookupTableEntries(lookupTable);
            return lookupTable;
        }

        #endregion

        #region OODataProject

        private readonly string getODataProjectSelectQuery = "ProjectName,ProjectId";

        private List<string> GetFilterQueries(List<Guid> projectUids)
        {
            logger.Info($"GetFilterQueries projectUids: {projectUids.Count};");

            List<string> resultQueries = new List<string>();
            int index = 0;
            resultQueries.Add(String.Empty);
            foreach (Guid projectUid in projectUids)
            {
                string tempQuery;
                if (resultQueries[index].Length == 0)
                {
                    tempQuery = resultQueries[index] + $"ProjectId eq guid'{projectUid}'";
                }
                else
                {
                    tempQuery = resultQueries[index] + $" or ProjectId eq guid'{projectUid}'";
                }
                if (tempQuery.Length <= GetODataTaskMaxQueryLength)
                {
                    resultQueries[index] = tempQuery;
                }
                else
                {
                    index++;
                    resultQueries.Add(String.Empty);
                    resultQueries[index] = resultQueries[index] + $"ProjectId eq guid'{projectUid}'";
                }
            }
            return resultQueries;
        }

        public List<ODataProject> GetODataProjects(List<Guid> projectUids)
        {
            logger.Info($"GetODataProjects projectUids: {projectUids.Count};");

            List<string> resultQueries = GetFilterQueries(projectUids);
            List<ODataProject> resultList = GetODataProjects(resultQueries);
            return resultList;
        }

        private List<ODataProject> GetODataProjects(List<string> resultQueries)
        {
            List<ODataProject> resultList = new List<ODataProject>();

            foreach (string resultQuery in resultQueries)
            {
                string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Projects?$select={getODataProjectSelectQuery}" +
                                  $"&$filter={resultQuery}";
                resultList.AddRange(GetOdataList<ODataProject>(odataUrl, FillODataEntityValues));
            }
            return resultList;
        }

        public ODataProject GetODataProject(Guid projectUid)
        {
            string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Projects?$select={getODataProjectSelectQuery}&$filter=ProjectId eq guid'{projectUid}'";
            return GetOdataList<ODataProject>(odataUrl, FillODataEntityValues).FirstOrDefault();
        }

        public ODataProject GetODataProject(string projectName)
        {
            string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Projects?$select={getODataProjectSelectQuery}&$filter=ProjectName eq '{projectName}'";
            return GetOdataList<ODataProject>(odataUrl, FillODataEntityValues).FirstOrDefault();
        }

        #endregion

        #region ODataResource

        public List<ODataResource> GetODataResources()
        {
            if (ProjectOnlineCache.ODataResources == null
                || ProjectOnlineCache.ODataResources.Count == 0)
            {
                string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/Resources?$select=ResourceId,ResourceEmailAddress,ResourceName,ResourceNTAccount&$filter=ResourceIsActive eq true";
                ProjectOnlineCache.ODataResources = GetOdataList<ODataResource>(odataUrl, FillODataEntityValues);
            }
            return ProjectOnlineCache.ODataResources;
        }

        public ODataResource GetCurrentODataResource()
        {
            return ProjectOnlineCache.CurrentODataResource ??
                   (ProjectOnlineCache.CurrentODataResource = GetODataResource(ProjectOnlineUserName));
        }

        public ODataResource GetODataResource(string assignee)
        {
            if (String.IsNullOrEmpty(assignee))
            {
                return null;
            }
            if (ProjectOnlineCache.ODataResources == null)
            {
                ProjectOnlineCache.ODataResources = GetODataResources();
            }
            return ProjectOnlineCache.ODataResources
                .FirstOrDefault(x => !String.IsNullOrEmpty(x.ResourceNTAccount)
                                     && x.ResourceNTAccount.ToLower().Contains(assignee.ToLower()));
        }

        #endregion

        #region TimesheetPeriods

        public List<ODataTimesheetPeriod> GetODataTimesheetPeriods()
        {
            if (ProjectOnlineCache.TimesheetPeriods == null)
            {
                string odataUrl = $"{ProjectOnlineUrl}/_api/ProjectData/TimesheetPeriods?$filter=Description eq 'Opened'";
                ProjectOnlineCache.TimesheetPeriods = GetOdataList<ODataTimesheetPeriod>(odataUrl, FillODataEntityValues);
            }
            return ProjectOnlineCache.TimesheetPeriods;
        }

        #endregion

        #region FillODataEntityValues

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataAssignment assignment)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "ProjectId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            assignment.ProjectId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "TaskId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            assignment.TaskId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "AssignmentId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            assignment.AssignmentId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "ResourceId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            assignment.ResourceId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "TaskName":
                        assignment.TaskName = xElement.Value;
                        break;

                    case ProjectServerConstants.SystemId + ProjectServerConstants.ODataReferencePostfix:
                        decimal systemId;
                        if (!String.IsNullOrEmpty(xElement.Value) && Decimal.TryParse(xElement.Value, out systemId))
                        {
                            assignment.SystemId = (int)systemId;
                        }
                        break;
                    case ProjectServerConstants.IssueKey + ProjectServerConstants.ODataReferencePostfix:
                        assignment.IssueKey = xElement.Value;
                        break;
                    case ProjectServerConstants.IssueId + ProjectServerConstants.ODataReferencePostfix:
                        assignment.IssueId = xElement.Value;
                        break;
                    case ProjectServerConstants.ParentEpicKey + ProjectServerConstants.ODataReferencePostfix:
                        assignment.ParentEpicKey = xElement.Value;
                        break;
                    case ProjectServerConstants.ParentVersionId + ProjectServerConstants.ODataReferencePostfix:
                        assignment.ParentVersionId = xElement.Value;
                        break;
                    case ProjectServerConstants.IssueTypeName + ProjectServerConstants.ODataReferencePostfix:
                        assignment.IssueTypeName = xElement.Value;
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataProject proj)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "ProjectId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            proj.ProjectId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "ProjectName":
                        proj.ProjectName = xElement.Value;
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataTask task)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "ProjectId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            task.ProjectId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "TaskId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            task.TaskId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "ParentTaskId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            task.ParentTaskId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "TaskName":
                        task.TaskName = xElement.Value;
                        break;
                    case ProjectServerConstants.SystemId:
                        decimal systemId;
                        if (!String.IsNullOrEmpty(xElement.Value) && Decimal.TryParse(xElement.Value, out systemId))
                        {
                            task.SystemId = (int)systemId;
                        }
                        break;
                    case ProjectServerConstants.IssueId:
                        task.IssueId = xElement.Value;
                        break;
                    case ProjectServerConstants.IssueKey:
                        task.IssueKey = xElement.Value;
                        break;
                    case ProjectServerConstants.ParentEpicKey:
                        task.ParentEpicKey = xElement.Value;
                        break;
                    case ProjectServerConstants.ParentVersionId:
                        task.ParentVersionId = xElement.Value;
                        break;
                    case ProjectServerConstants.IssueTypeName:
                        task.IssueTypeName = xElement.Value;
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataLookupTable lookupTable)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "Id":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            lookupTable.Id = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "Name":
                        lookupTable.Name = xElement.Value;
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataLookupTableEntry entry)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "Id":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            entry.Id = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "FullValue":
                        entry.FullValue = xElement.Value;
                        break;
                    case "InternalName":
                        entry.InternalName = xElement.Value;
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataResource oDataResource)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "ResourceId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            oDataResource.ResourceId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "ResourceEmailAddress":
                        oDataResource.ResourceEmailAddress = xElement.Value.ToLower();
                        break;
                    case "ResourceName":
                        oDataResource.ResourceName = xElement.Value.ToLower();
                        break;
                    case "ResourceNTAccount":
                        oDataResource.ResourceNTAccount = xElement.Value.ToLower();
                        break;
                }
            }
            return true;
        }

        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataTimesheetPeriod oDataTimesheetPeriod)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "PeriodId":
                        if (!String.IsNullOrEmpty(xElement.Value))
                        {
                            oDataTimesheetPeriod.PeriodId = Guid.Parse(xElement.Value);
                        }
                        break;
                    case "PeriodName":
                        oDataTimesheetPeriod.PeriodName = xElement.Value;
                        break;
                    case "Description":
                        oDataTimesheetPeriod.Description = xElement.Value;
                        break;
                    case "PeriodStatusId":
                        oDataTimesheetPeriod.PeriodStatusId = Int32.Parse(xElement.Value);
                        break;
                    case "EndDate":
                        oDataTimesheetPeriod.EndDate = DateTime.Parse(xElement.Value);
                        break;
                    case "StartDate":
                        oDataTimesheetPeriod.StartDate = DateTime.Parse(xElement.Value);
                        break;
                }
            }
            return true;
        }
        private bool FillODataEntityValues(IEnumerable<XElement> propertiesElements, ODataAssignmentTimephasedDataRecord oDataAssignmentTimephasedDataRecord)
        {
            foreach (XElement xElement in propertiesElements)
            {
                switch (xElement.Name.LocalName)
                {
                    case "ProjectId":
                        oDataAssignmentTimephasedDataRecord.ProjectId = Guid.Parse(xElement.Value);
                        break;
                    case "AssignmentId":
                        oDataAssignmentTimephasedDataRecord.AssignmentId = Guid.Parse(xElement.Value);
                        break;
                    case "TimeByDay":
                        oDataAssignmentTimephasedDataRecord.TimeByDay = DateTime.Parse(xElement.Value);
                        break;
                    case "AssignmentActualWork":
                        oDataAssignmentTimephasedDataRecord.AssignmentActualWork = Decimal.Parse(xElement.Value);
                        break;
                    case "TaskId":
                        oDataAssignmentTimephasedDataRecord.TaskId = Guid.Parse(xElement.Value);
                        break;
                    case "ResourceId":
                        oDataAssignmentTimephasedDataRecord.ResourceId = Guid.Parse(xElement.Value);
                        break;
                }
            }
            return true;
        }

        #endregion

        public Dictionary<string, string> GetCustomFieldsDict()
        {
            return ProjectOnlineCache.CustomFieldsDict;
        }

        public void ClearCache()
        {
            ProjectOnlineCache.ODataResources = null;
            ProjectOnlineCache.TimesheetPeriods = null;
        }

        public void InitCache()
        {
            try
            {
                GetODataResources();
                GetODataTimesheetPeriods();
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }
    }
}