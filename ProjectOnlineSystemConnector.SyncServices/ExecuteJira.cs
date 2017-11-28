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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlassian.Jira;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataAccess.Jira;
using ProjectOnlineSystemConnector.DataModel.JiraWebHook;
using ProjectOnlineSystemConnector.DataModel.ViewModel;

namespace ProjectOnlineSystemConnector.SyncServices
{
    public class ExecuteJira
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private CustomField jiraEpicLinkField, jiraEpicNameField, jiraSprintField;
        private readonly SyncSystemBusinessService syncSystemBusinessService;
        private readonly MasterBusinessService masterBusinessService;
        private readonly CommonBusinessService commonBusinessService;
        private readonly ProjectServerSystemLinkBusinessService projectServerSystemLinkBusinessService;
        private readonly SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService;

        public ExecuteJira(UnitOfWork unitOfWork)
        {
            syncSystemBusinessService = new SyncSystemBusinessService(unitOfWork);
            masterBusinessService = new MasterBusinessService(unitOfWork);
            commonBusinessService = new CommonBusinessService(unitOfWork);
            syncSystemFieldMappingBusinessService = new SyncSystemFieldMappingBusinessService(unitOfWork);
            projectServerSystemLinkBusinessService = new ProjectServerSystemLinkBusinessService(null, unitOfWork);
        }

        public async Task Execute(Guid projectUid, int? systemId, string projectId, string epicKey)
        {
            logger.Info($"Execute {projectUid}, {systemId}, {projectId}, {epicKey} START");
            List<ProjectServerSystemLinkDTO> projectServerSystemLinks = await projectServerSystemLinkBusinessService
                .GetListAsync(projectUid, systemId, projectId, epicKey);
            await projectServerSystemLinkBusinessService.UpdateLastExecuted(projectServerSystemLinks, DateTime.Now);

            var resultGroupingBySystem = (from pssl in projectServerSystemLinks
                                          group pssl by pssl.SystemId into tempGroup
                                          select new
                                          {
                                              SystemId = tempGroup.Key,
                                              ProjectId = tempGroup.Where(x => x.IsHomeProject && !String.IsNullOrEmpty(x.ProjectId))
                                                      .Select(x => x.ProjectId)
                                                      .FirstOrDefault(),
                                              ProjectKey = tempGroup.Where(x => x.IsHomeProject && !String.IsNullOrEmpty(x.ProjectKey))
                                                      .Select(x => x.ProjectKey)
                                                      .FirstOrDefault(),
                                              IsHomeProject = tempGroup.Where(x => String.IsNullOrEmpty(x.EpicKey))
                                                      .Select(x => x.IsHomeProject)
                                                      .FirstOrDefault(),
                                              EpicKeys = tempGroup.Select(x => x.EpicKey).Where(x => !String.IsNullOrEmpty(x)).ToList()
                                          }).ToList();

            logger.Info($"Execute {projectUid} START resultGroupingBySystem.Count:{resultGroupingBySystem.Count}");
            foreach (var groupBySystem in resultGroupingBySystem)
            {
                logger.Info($"Execute {projectUid} groupBySystem.SystemId: {groupBySystem.SystemId}");
                SyncSystemDTO syncSystem = syncSystemBusinessService.GetSyncSystem(groupBySystem.SystemId);
                if (syncSystem == null)
                {
                    logger.Info($"Execute {projectUid} groupBySystem.SystemId: {groupBySystem.SystemId} IS NULL");
                    continue;
                }
                JiraAccessService jiraAccessService = new JiraAccessService(syncSystem);
                string updateIssueStartDate = syncSystem.ActualsStartDate?.AddDays(-7).ToString("yyyy-MM-dd") ?? "";
                IEnumerable<CustomField> customFields = await jiraAccessService.GetCustomFields();
                if (customFields == null)
                {
                    logger.Error($"Execute {projectUid} Custom fields for system {groupBySystem.SystemId} is NULL!");
                    continue;
                }
                jiraEpicLinkField = customFields.FirstOrDefault(x => x.Name.ToLower() == JiraConstants.EpicLinkFieldNameLowerCase);
                jiraEpicNameField = customFields.FirstOrDefault(x => x.Name.ToLower() == JiraConstants.EpicNameFieldNameLowerCase);
                jiraSprintField = customFields.FirstOrDefault(x => x.Name.ToLower() == JiraConstants.SprintFieldNameLowerCase);
                List<Issue> epicsAndLinkedTasks = new List<Issue>();
                string resultJql = "";
                logger.Info($"Execute projectUid: {projectUid}; ProjectId: {groupBySystem.ProjectId}; ProjectId: {groupBySystem.ProjectId}; " +
                            $"updateIssueStartDate: {updateIssueStartDate}");
                if (!groupBySystem.IsHomeProject)
                {
                    resultJql = GetJqlForEpicsAndLinkedTasks(groupBySystem.EpicKeys);
                    if (!String.IsNullOrEmpty(resultJql))
                    {
                        logger.Info($"Execute {projectUid} ResultJql 1 {resultJql}");
                        epicsAndLinkedTasks =
                            await jiraAccessService.GetIssuesAsync(resultJql, updateIssueStartDate, syncSystem);
                    }
                    await GetLinkedIssues(epicsAndLinkedTasks, groupBySystem.EpicKeys);

                    logger.Info($"Execute {projectUid} EpicsAndLinkedTasks {epicsAndLinkedTasks.Count}");
                    List<Issue> subTasks =
                        await GetSubTasks(epicsAndLinkedTasks, jiraAccessService, updateIssueStartDate, syncSystem);
                    await MergeEpicsAndLinkedTasksIntoDb(epicsAndLinkedTasks, subTasks, syncSystem, jiraAccessService);
                }
                else
                {
                    //get EPICs, that belong to Jira Project (projectId), but linked to another PO project (projectUid)
                    List<string> epicsToIgnore = projectServerSystemLinkBusinessService.GetEpicsToIgnore(projectUid, projectId, syncSystem);
                    string projectJql = ProjectJql(syncSystem, groupBySystem.ProjectId, epicsToIgnore);
                    if (String.IsNullOrEmpty(projectJql))
                    {
                        continue;
                    }
                    resultJql += $"{projectJql}";
                    logger.Info($"Execute {projectUid} ResultJql 2 {resultJql}");
                    List<Issue> homeProjectIssues =
                        await jiraAccessService.GetIssuesAsync(resultJql, updateIssueStartDate, syncSystem);
                    logger.Info($"Execute {projectUid} homeProjectIssues.Count {homeProjectIssues.Count}");
                    List<string> issueKeys = homeProjectIssues.Select(x => x.Key.Value).ToList();
                    ////get EPM projects
                    //List<ODataProject> oDataProjects = projectOnlineAccessService
                    //    .GetODataProjectsForJiraWithoutProject(projectUid);
                    //logger.Info("Execute ODataProjects " + oDataProjects.Count);
                    ////check, if EPICs is selected by another project. If yes - remove it from sync
                    //foreach (ODataProject project in oDataProjects)
                    //{
                    //    if (project.ODataSyncObjectLinks == null || project.ODataSyncObjectLinks.Count == 0)
                    //    {
                    //        continue;
                    //    }
                    //    foreach (ODataSyncObjectLink syncObjectLink in project.ODataSyncObjectLinks)
                    //    {
                    //        if (syncObjectLink.SystemId != oDataSyncObjectLink.SystemId)
                    //        {
                    //            continue;
                    //        }
                    //        if (syncObjectLink.Epics != null)
                    //        {
                    //            foreach (string epicKey in syncObjectLink.Epics)
                    //            {
                    //                if (epicKeys.Contains(epicKey))
                    //                {
                    //                    Issue issueToDelete = epicsAndLinkedTasks.FirstOrDefault(x => x.Key == epicKey);
                    //                    if (issueToDelete != null)
                    //                    {
                    //                        epicsAndLinkedTasks.Remove(issueToDelete);
                    //                    }
                    //                    epicKeys.Remove(epicKey);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //final query
                    //get all assigned epics and their issues for the EPM project
                    resultJql = GetJqlForEpicsAndLinkedTasks(groupBySystem.EpicKeys);
                    logger.Info($"Execute {projectUid} ResultJql 3 {resultJql}");
                    //get issues, that belong to Epics in home project
                    if (issueKeys.Count != 0)
                    {
                        string epicKeysStr = "(";
                        foreach (string epic in issueKeys)
                        {
                            epicKeysStr += "'" + epic + "',";
                        }
                        epicKeysStr = epicKeysStr.Trim(',');
                        epicKeysStr += ")";
                        //Get 
                        if (jiraEpicLinkField != null)
                        {
                            if (!String.IsNullOrEmpty(resultJql))
                            {
                                resultJql += " or ";
                                resultJql += $"'{jiraEpicLinkField.Name}' in " + epicKeysStr;
                                //get free issues (Epic Link is null)
                                resultJql += $" or ('{jiraEpicLinkField.Name}' is null";
                                projectJql = ProjectJql(syncSystem, groupBySystem.ProjectId, new List<string>());
                                resultJql += $" and issueType!='Epic' and {projectJql})";
                            }
                        }
                    }

                    logger.Info($"Execute {projectUid} ResultJql 4 {resultJql}");
                    if (!String.IsNullOrEmpty(resultJql))
                    {
                        epicsAndLinkedTasks =
                            await jiraAccessService.GetIssuesAsync(resultJql, updateIssueStartDate, syncSystem);
                    }
                    logger.Info($"Execute {projectUid} EpicsAndLinkedTasks 1: {epicsAndLinkedTasks.Count}");
                    epicsAndLinkedTasks = epicsAndLinkedTasks.Union(homeProjectIssues).ToList();
                    await GetLinkedIssues(epicsAndLinkedTasks, groupBySystem.EpicKeys);
                    logger.Info($"Execute {projectUid} EpicsAndLinkedTasks 2: {epicsAndLinkedTasks.Count}");
                    List<Issue> subTasks =
                        await GetSubTasks(epicsAndLinkedTasks, jiraAccessService, updateIssueStartDate, syncSystem);
                    await MergeEpicsAndLinkedTasksIntoDb(epicsAndLinkedTasks, subTasks, syncSystem, jiraAccessService);
                }
            }
        }

        private string ProjectJql(SyncSystemDTO syncSystem, string projectId, List<string> epicsToIgnore)
        {
            string epicKeysStr = "";
            if (epicsToIgnore != null && epicsToIgnore.Count > 0)
            {
                epicKeysStr = "(";
                foreach (string epic in epicsToIgnore)
                {
                    epicKeysStr += "'" + epic + "',";
                }
                epicKeysStr = epicKeysStr.Trim(',');
                epicKeysStr += ")";
            }

            if (String.IsNullOrEmpty(projectId))
            {
                logger.Error($"Execute syncSystem.SystemId: {syncSystem.SystemId}; ProjectId: {projectId}");
                return null;
            }
            //get epics
            string result = $"project='{projectId}'";
            if (!String.IsNullOrEmpty(result) && !String.IsNullOrEmpty(epicKeysStr))
            {
                result += " and key not in " + epicKeysStr;
            }
            return result;
        }

        private async Task<List<Issue>> GetSubTasks(List<Issue> epicsAndLinkedTasks, JiraAccessService jiraAccessService, string updateIssueStartDate, SyncSystemDTO syncSystem)
        {
            if (epicsAndLinkedTasks.Count != 0)
            {
                string issueKeys = "(";
                foreach (Issue issue in epicsAndLinkedTasks)
                {
                    issueKeys += "'" + issue.Key + "',";
                }
                issueKeys = issueKeys.Trim(',');
                issueKeys += ")";
                string resultJql = "parent in " + issueKeys;
                logger.Info($"Execute ResultJql 5 GetSubTasks: {resultJql}");
                List<Issue> subTasks = await jiraAccessService.GetIssuesAsync(resultJql, updateIssueStartDate, syncSystem);
                return subTasks;
            }
            return new List<Issue>();
        }

        private async Task GetLinkedIssues(List<Issue> epicsAndLinkedTasks, List<string> epicDirectLinks)
        {
            if (jiraEpicLinkField == null)
            {
                List<Issue> linkedIssues = new List<Issue>();

                foreach (Issue epic in epicsAndLinkedTasks)
                {
                    if (epic.Type.Name != "Epic")
                    {
                        continue;
                    }
                    if (!epicDirectLinks.Contains(epic.Key.Value))
                    {
                        continue;
                    }
                    List<IssueLink> issueLinks = (await epic.GetIssueLinksAsync()).ToList();
                    linkedIssues.AddRange(issueLinks
                        .Where(
                            x =>
                                x.LinkType.Name == "Relates" && x.OutwardIssue != null &&
                                x.OutwardIssue.Type.Name != "Epic" && x.OutwardIssue.Key.Value != epic.Key.Value)
                        .Select(x => x.OutwardIssue)
                        .ToList());
                    linkedIssues.AddRange(issueLinks
                        .Where(
                            x =>
                                x.LinkType.Name == "Relates" && x.InwardIssue != null &&
                                x.InwardIssue.Type.Name != "Epic" && x.InwardIssue.Key.Value != epic.Key.Value)
                        .Select(x => x.InwardIssue)
                        .ToList());
                }
                epicsAndLinkedTasks.AddRange(linkedIssues);
            }
        }

        private string GetJqlForEpicsAndLinkedTasks(List<string> epics)
        {
            if (epics != null && epics.Count > 0)
            {
                string epicKeys = "(";
                foreach (string epic in epics)
                {
                    epicKeys += "'" + epic + "',";
                }
                epicKeys = epicKeys.Trim(',');
                epicKeys += ")";
                string result = "key in " + epicKeys;
                if (jiraEpicLinkField != null)
                {
                    result += " or '" + jiraEpicLinkField.Name + "' in " + epicKeys;
                }
                return result;
            }
            return null;
        }

        private async Task FillAllWorkLogs(Issue issue, Dictionary<string, List<Worklog>> allWorkLogs)
        {
            //logger.Info($"Execute FillAllWorkLogs issue.Key: {issue.Key} START");
            List<Worklog> worklogs = (await issue.GetWorklogsAsync()).ToList();
            if (!allWorkLogs.ContainsKey(issue.Key.Value))
            {
                allWorkLogs.Add(issue.Key.Value, new List<Worklog>());
            }
            allWorkLogs[issue.Key.Value].AddRange(worklogs);
            logger.Info($"Execute FillAllWorkLogs issue.Key: {issue.Key} allWorkLogs[issue.Key.Value].Count: {allWorkLogs[issue.Key.Value].Count}");
        }

        private async Task MergeEpicsAndLinkedTasksIntoDb(List<Issue> epicsAndLinkedTasks, List<Issue> subTasks,
            SyncSystemDTO syncSystem, JiraAccessService jiraAccessService)
        {
            if (epicsAndLinkedTasks != null && epicsAndLinkedTasks.Count != 0)
            {
                List<string> projectKeys = epicsAndLinkedTasks.Select(x => x.Project).Distinct().ToList();
                List<Project> projects = await jiraAccessService.GetProjectsByKeys(projectKeys);
                List<ProjectVersion> projectVersions = new List<ProjectVersion>();

                foreach (string projectKey in projectKeys)
                {
                    IEnumerable<ProjectVersion> tempList = await jiraAccessService.GetVersionsAsync(projectKey);
                    projectVersions.AddRange(tempList);
                }
                Dictionary<string, List<Worklog>> allWorkLogs = new Dictionary<string, List<Worklog>>();
                Dictionary<string, JiraEpicLink> epicLinks = new Dictionary<string, JiraEpicLink>();
                Dictionary<string, IssueTimeTrackingData> timeTrackingData = new Dictionary<string, IssueTimeTrackingData>();
                List<string> epicDirectLinks =
                    projectServerSystemLinkBusinessService.GetEpicDirectLinks(syncSystem.SystemId);
                foreach (Issue issue in epicsAndLinkedTasks)
                {
                    await FillAllWorkLogs(issue, allWorkLogs);
                    await FillEpicLinks(issue, epicLinks, syncSystem, epicDirectLinks);
                    //await FillIssueTimeTrackingData(issue, timeTrackingData);
                }
                foreach (Issue issue in subTasks)
                {
                    await FillAllWorkLogs(issue, allWorkLogs);
                    //await FillIssueTimeTrackingData(issue, timeTrackingData);
                }

                //List<string> issueKeys = epicsAndLinkedTasks.Select(x => x.Key.Value).ToList();
                //List<string> subTaskKeys = subTasks.Select(x => x.Key.Value).ToList();
                //issueKeys = issueKeys.Union(subTaskKeys).ToList();

                //List<string> issueIds = epicsAndLinkedTasks.Select(x => x.Key.Value).ToList();
                //List<string> subTaskIds = subTasks.Select(x => x.Key.Value).ToList();
                //issueIds = issueIds.Union(subTaskIds).ToList();


                logger.Info("Execute epicsAndLinkedTasks: " + epicsAndLinkedTasks.Count);

                SystemToDbViewModel syncAllViewModel = new SystemToDbViewModel();
                List<SyncSystemFieldMapping> syncSystemFieldMappings = syncSystemFieldMappingBusinessService
                    .GetSyncSystemFieldMappings(syncSystem.SystemId);


                foreach (Issue issue in epicsAndLinkedTasks)
                {
                    FillTables(syncAllViewModel, syncSystem, issue, null, projects, allWorkLogs, epicLinks,
                        timeTrackingData, syncSystemFieldMappings, jiraAccessService);
                }

                logger.Info("Execute SubTasks: " + subTasks.Count);
                foreach (Issue issue in subTasks)
                {
                    Issue parentIssue = epicsAndLinkedTasks.FirstOrDefault(x => x.Key == issue.ParentIssueKey);
                    FillTables(syncAllViewModel, syncSystem, issue, parentIssue, projects, allWorkLogs, epicLinks,
                        timeTrackingData, syncSystemFieldMappings, jiraAccessService);
                }

                logger.Info("Execute ProjectVersions " + projectVersions.Count);
                foreach (ProjectVersion projectVersion in projectVersions)
                {
                    Project project = projects.FirstOrDefault(x => x.Key == projectVersion.ProjectKey);
                    Staging staging = FillStagingTable(syncAllViewModel, syncSystem.SystemId, projectVersion, project);
                    FillMasterTable(syncAllViewModel, syncSystem.SystemId, projectVersion, project);
                    FillMasterHistoryTable(syncAllViewModel, syncSystem.SystemId, projectVersion, project);

                    SyncSystemFieldMapping fieldMappingRecordStateGeneral = syncSystemFieldMappings
                        .FirstOrDefault(x => x.SystemId == syncSystem.SystemId &&
                                             x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);
                    SetCustomFields(syncAllViewModel, fieldMappingRecordStateGeneral, RecordStateConst.Done, null, null, staging);

                    SyncSystemFieldMapping fieldMappingRecordStateActual = syncSystemFieldMappings
                        .FirstOrDefault(x => x.SystemId == syncSystem.SystemId &&
                                             x.EpmFieldName == ProjectServerConstants.RecordStateActual);
                    SetCustomFields(syncAllViewModel, fieldMappingRecordStateActual, RecordStateConst.Done, null, null, staging);
                }

                logger.Info("Execute MergeEpicsAndLinkedTasksIntoDb UnitOfWork START");
                //Delete old data and adding new should be in one transsactions.
                //But sometimes it has happened taht we try to delete the same records in different threads. 
                //Deadlock occured, transaction is rolling back and insert is not happening
                //Delete is not primary operation - we can skip it. Old data will be deleted during pushing to EPM.
                //So i decided to do it in different transactions with logic: INSERT records-->
                //--> delete x => x.SystemId == systemId && issueKeys.Contains(x.IssueKey) && !newStagingIds.Contains(x.StagingId)
                await commonBusinessService.AddNewData(syncAllViewModel);
                commonBusinessService.DeleteOldData(syncAllViewModel, syncSystem.SystemId);
                logger.Info("Execute MergeEpicsAndLinkedTasksIntoDb UnitOfWork END");
            }
        }

        private async Task FillEpicLinks(Issue issue, Dictionary<string, JiraEpicLink> epicLinks, SyncSystemDTO syncSystem, List<string> epicDirectLinks)
        {
            string epicLink = null;
            string epicName = null;
            string jiraEpicLinkError = null;
            if (issue.Type.Name != "Epic")
            {
                //logger.Info($"Execute FillEpicLinks 1 syncSystem.IsVersionAsEpic: {syncSystem.IsVersionAsEpic} " +
                //            $"issue.Key.Value: {issue.Key.Value} " +
                //            $"jiraEpicLinkField.Name: {jiraEpicLinkField?.Name}");

                if (jiraEpicLinkField != null && issue[jiraEpicLinkField.Name] != null)
                {
                    epicLink = issue[jiraEpicLinkField.Name].Value;
                    Master masterEpic = masterBusinessService.GetMaster(syncSystem.SystemId, issue[jiraEpicLinkField.Name].Value);
                    if (masterEpic != null)
                    {
                        epicName = masterEpic.IssueName;
                    }
                    //logger.Info($"Execute FillEpicLinks issue.Key.Value: {issue.Key.Value} issue[jiraEpicLinkField.Name].Value: {issue[jiraEpicLinkField.Name].Value}");
                }
                else
                {
                    List<IssueLink> issueLinks = (await issue.GetIssueLinksAsync()).ToList();
                    List<IssueLink> links = issueLinks
                        .Where(x => x.LinkType.Name == "Relates"
                            && ((x.OutwardIssue != null && x.OutwardIssue.Type.Name == "Epic")
                                || (x.InwardIssue != null && x.InwardIssue.Type.Name == "Epic")))
                        .ToList();
                    if (links.Count > 0)
                    {
                        epicLink = links[0].OutwardIssue.Type.Name == "Epic"
                            ? links[0].OutwardIssue.Key.Value
                            : links[0].InwardIssue.Type.Name == "Epic"
                                ? links[0].InwardIssue.Key.Value
                                : null;
                        if (jiraEpicNameField != null)
                        {
                            //logger.Info($"Execute FillEpicLinks syncSystem.IsVersionAsEpic: {syncSystem.IsVersionAsEpic} " +
                            //            $"issue.Key.Value: {issue.Key.Value} " +
                            //            $"links[0].OutwardIssue.Type.Name: {links[0].OutwardIssue.Type.Name} " +
                            //            $"links[0].OutwardIssue[jiraEpicNameField.Name]: {links[0].OutwardIssue[jiraEpicNameField.Name]} " +
                            //            $"links[0].InwardIssue.Type.Name: {links[0].InwardIssue.Type.Name} " +
                            //            $"links[0].InwardIssue[jiraEpicNameField.Name]: {links[0].InwardIssue[jiraEpicNameField.Name]}");
                            epicName = links[0].OutwardIssue.Type.Name == "Epic"
                                    ? links[0].OutwardIssue[jiraEpicNameField.Name]?.Value
                                    : links[0].InwardIssue.Type.Name == "Epic"
                                        ? links[0].InwardIssue[jiraEpicNameField.Name]?.Value
                                        : null;
                        }
                        else
                        {
                            //logger.Info($"Execute FillEpicLinks syncSystem.IsVersionAsEpic: {syncSystem.IsVersionAsEpic} " +
                            //            $"issue.Key.Value: {issue.Key.Value} " +
                            //            $"links[0].OutwardIssue.Type.Name: {links[0].OutwardIssue.Type.Name} " +
                            //            $"links[0].OutwardIssue.Summary.Trim(): {links[0].OutwardIssue.Summary.Trim()} " +
                            //            $"links[0].InwardIssue.Type.Name: {links[0].InwardIssue.Type.Name} " +
                            //            $"links[0].InwardIssue.Summary.Trim(): {links[0].InwardIssue.Summary.Trim()}");
                            epicName = links[0].OutwardIssue.Type.Name == "Epic"
                                ? links[0].OutwardIssue.Summary.Trim()
                                : links[0].InwardIssue.Type.Name == "Epic"
                                    ? links[0].InwardIssue.Summary.Trim()
                                    : null;
                        }
                        //logger.Info($"Execute FillEpicLinks issue.Key.Value: {issue.Key.Value} " +
                        //            $"links[0].OutwardIssue.Type.Name: {links[0].OutwardIssue.Type.Name} " +
                        //            $"links[0].OutwardIssue.Key.Value: {links[0].OutwardIssue.Key.Value} " +
                        //            $"links[0].InwardIssue.Type.Name: {links[0].InwardIssue.Type.Name} " +
                        //            $"links[0].InwardIssue.Key.Value: {links[0].InwardIssue.Key.Value}");
                    }
                    if (links.Count > 1)
                    {
                        // we found out, that this issue is connected more than to 1 Epic. But may be none of this Epics is connected directly
                        //Lets check it
                        int counter = 0;
                        foreach (IssueLink jiraIssueLink in links)
                        {
                            if (epicDirectLinks.Contains(jiraIssueLink.InwardIssue?.Key.Value)
                                || epicDirectLinks.Contains(jiraIssueLink.OutwardIssue?.Key.Value))
                            {
                                counter++;
                            }
                        }
                        //We have at least one direct connection. That's mean, we don't know, what to do with this Issue. LEts say it is error
                        if (counter > 0)
                        {
                            jiraEpicLinkError = RecordStateConst.Error + ": too many links to Epics";
                        }
                    }
                }
                epicName = epicName.RemoveNewLinesTabs();
                if (!epicLinks.ContainsKey(issue.Key.Value))
                {
                    epicLinks.Add(issue.Key.Value,
                        new JiraEpicLink { EpicLink = epicLink, EpicName = epicName, EpicLinkError = jiraEpicLinkError });
                }
                else
                {
                    epicLinks[issue.Key.Value] = new JiraEpicLink
                    {
                        EpicLink = epicLink,
                        EpicName = epicName,
                        EpicLinkError = jiraEpicLinkError
                    };
                }

                //logger.Info($"Execute FillEpicLinks issue.Key.Value: {issue.Key.Value} " +
                //            $"epicLinks[issue.Key.Value].EpicLink: {epicLinks[issue.Key.Value].EpicLink} " +
                //            $"epicLinks[issue.Key.Value].EpicName: {epicLinks[issue.Key.Value].EpicName}");
            }
        }

        private void FillTables(SystemToDbViewModel syncAllViewModel, SyncSystemDTO syncSystem, Issue issue, Issue parentIssue,
            List<Project> projects, Dictionary<string, List<Worklog>> allWorkLogs,
            Dictionary<string, JiraEpicLink> epicLinks, Dictionary<string, IssueTimeTrackingData> timeTrackingData,
            List<SyncSystemFieldMapping> syncSystemFieldMappings, JiraAccessService jiraAccessService)
        {
            Project project = projects.FirstOrDefault(x => x.Key == issue.Project);

            Staging staging = FillStagingTable(syncAllViewModel, syncSystem, issue,
                parentIssue, project, epicLinks, timeTrackingData);
            Master master = FillMasterTable(syncAllViewModel, syncSystem, issue,
                parentIssue, project, epicLinks, timeTrackingData);
            MasterHistory masterHistory = FillMasterHistoryTable(syncAllViewModel, syncSystem, issue,
               parentIssue, project, epicLinks, timeTrackingData);
            FillCustomFields(syncAllViewModel, syncSystemFieldMappings, issue, master, masterHistory, staging, epicLinks);
            if (allWorkLogs.ContainsKey(issue.Key.Value))
            {
                FillWorklogTables(syncAllViewModel, allWorkLogs[issue.Key.Value], issue.JiraIdentifier, syncSystem.SystemId);
            }
            AddSprints(syncAllViewModel, issue, master, staging, masterHistory, syncSystemFieldMappings, jiraAccessService);
        }

        private void SetCustomFields(SystemToDbViewModel syncAllViewModel, SyncSystemFieldMapping fieldMapping, string customFieldValue,
            Master master, MasterHistory masterHistory, Staging staging)
        {
            customFieldValue = customFieldValue.RemoveNewLinesTabs();
            if (master != null)
            {
                MasterFieldMappingValue masterValue = new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                    Value = customFieldValue
                };
                syncAllViewModel.MasterFieldMappingValuesToInsert.Add(masterValue);
            }

            if (masterHistory != null)
            {
                MasterHistoryFieldMappingValue masterHistoryValue = new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                    Value = customFieldValue
                };
                syncAllViewModel.MasterHistoryFieldMappingValuesToInsert.Add(masterHistoryValue);
            }

            if (staging != null)
            {
                StagingFieldMappingValue stagingValue = new StagingFieldMappingValue
                {
                    Staging = staging,
                    SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                    Value = customFieldValue
                };
                syncAllViewModel.StagingFieldMappingValuesToInsert.Add(stagingValue);
            }

            if (!String.IsNullOrEmpty(fieldMapping.StagingFieldName))
            {
                PropertyInfo piStaging = typeof(Staging).GetProperty(fieldMapping.StagingFieldName);
                piStaging?.SetValue(staging, customFieldValue);

                PropertyInfo piMaster = typeof(Master).GetProperty(fieldMapping.StagingFieldName);
                piMaster?.SetValue(master, customFieldValue);

                //masterHistory
                PropertyInfo piMasterHistory = typeof(MasterHistory).GetProperty(fieldMapping.StagingFieldName);
                piMasterHistory?.SetValue(masterHistory, customFieldValue);
            }
        }

        private void AddSprints(SystemToDbViewModel syncAllViewModel, Issue issue,
            Master master, Staging staging, MasterHistory masterHistory,
            List<SyncSystemFieldMapping> syncSystemFieldMappings,
            JiraAccessService jiraAccessService)
        {
            logger.Info($"Execute AddSprints issue.Key: {issue.Key}");

            if (jiraSprintField != null && issue[jiraSprintField.Name] != null)
            {
                string[] sprintsStrings = issue.CustomFields[jiraSprintField.Name].Values;

                logger.Info($"Execute AddSprints issue.Key: {issue.Key}; " +
                            $"jiraSprintField.Name: {jiraSprintField.Name}; " +
                            $"jiraSprintField.Id: {jiraSprintField.Id}; " +
                            $"sprints.Length: {sprintsStrings.Length}");

                List<MasterFieldMappingValue> masterFieldMappingValues = new List<MasterFieldMappingValue>();
                List<MasterHistoryFieldMappingValue> masterHistoryFieldMappingValues = new List<MasterHistoryFieldMappingValue>();
                List<StagingFieldMappingValue> stagingFieldMappingValues = new List<StagingFieldMappingValue>();

                JiraSprint jiraSprint = jiraAccessService.ParseSprintField(sprintsStrings);
                commonBusinessService.SetSprintField(jiraSprint, syncSystemFieldMappings, master, masterHistory, staging,
                    masterFieldMappingValues, masterHistoryFieldMappingValues, stagingFieldMappingValues);
                syncAllViewModel.MasterFieldMappingValuesToInsert.AddRange(masterFieldMappingValues);
                syncAllViewModel.MasterHistoryFieldMappingValuesToInsert.AddRange(masterHistoryFieldMappingValues);
                syncAllViewModel.StagingFieldMappingValuesToInsert.AddRange(stagingFieldMappingValues);
            }
        }

        private void FillCustomFields(SystemToDbViewModel syncAllViewModel, List<SyncSystemFieldMapping> syncSystemFieldMappings, Issue issue,
            Master master, MasterHistory masterHistory, Staging staging, Dictionary<string, JiraEpicLink> epicLinks)
        {
            logger.Info($"Execute FillCustomFields issue.Key: {issue.Key} issue.CustomFields.Count: {issue.CustomFields.Count}");
            foreach (SyncSystemFieldMapping fieldMapping in syncSystemFieldMappings)
            {
                if (String.IsNullOrEmpty(fieldMapping.SystemFieldName))
                {
                    if (fieldMapping.EpmFieldName == ProjectServerConstants.RecordStateGeneral)
                    {
                        SetCustomFields(syncAllViewModel, fieldMapping, RecordStateConst.New, master, masterHistory, staging);
                    }
                    if (fieldMapping.EpmFieldName == ProjectServerConstants.RecordStateActual)
                    {
                        SetCustomFields(syncAllViewModel, fieldMapping, RecordStateConst.Done, master, masterHistory, staging);
                    }
                    continue;
                }
                try
                {
                    string customFieldValue = "";
                    if (fieldMapping.EpmFieldName == ProjectServerConstants.EpicName
                        && epicLinks.ContainsKey(issue.Key.Value))
                    {
                        customFieldValue = epicLinks[issue.Key.Value].EpicName;
                        SetCustomFields(syncAllViewModel, fieldMapping, customFieldValue, master, masterHistory, staging);
                        continue;
                    }
                    if (fieldMapping.EpmFieldName == ProjectServerConstants.EpicName
                        && issue.Type.Name == "Epic")
                    {
                        customFieldValue = GetIssueName(issue);
                        SetCustomFields(syncAllViewModel, fieldMapping, customFieldValue, master, masterHistory, staging);
                        continue;
                    }
                    if (issue.CustomFields[fieldMapping.SystemFieldName] != null)
                    {
                        if (issue.CustomFields[fieldMapping.SystemFieldName].Values.Length > 0)
                        {
                            customFieldValue = issue.CustomFields[fieldMapping.SystemFieldName].Values[0];
                        }
                        if (fieldMapping.IsMultiSelect || fieldMapping.IsIdWithValue)
                        {
                            for (int i = 1; i < issue.CustomFields[fieldMapping.SystemFieldName].Values.Length; i++)
                            {
                                customFieldValue += "," + issue.CustomFields[fieldMapping.SystemFieldName].Values[i];
                            }
                        }
                        logger.Info("Execute FillCustomFields fieldMapping.SystemFieldName: " + fieldMapping.SystemFieldName + " customFieldValue: " + customFieldValue);
                        SetCustomFields(syncAllViewModel, fieldMapping, customFieldValue,
                            master, masterHistory, staging);
                    }
                }
                //if field is not exists in Jira, InvalidOperationException is thrown. It is not problem for us - we just ignore it
                catch (InvalidOperationException invalidOperationException)
                {
                }
            }
        }

        private Staging FillStagingTable(SystemToDbViewModel syncAllViewModel, SyncSystemDTO syncSystem, Issue issue,
            Issue parentIssue, Project project, Dictionary<string, JiraEpicLink> epicLinks,
            Dictionary<string, IssueTimeTrackingData> timeTrackingData)
        {
            logger.Info($"Execute FillStagingTable issue.Key.Value: {issue.Key.Value} issue.Type.Name: {issue.Type.Name} issue[jiraEpicNameField.Name]?.Value: {issue[jiraEpicNameField.Name]?.Value}");
            var staging = new Staging
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                RecordState = RecordStateConst.New,
                ChangedFields = "all",
                WebHookEvent = "syncAll",
                SystemId = syncSystem.SystemId,
                IssueId = issue.JiraIdentifier,
                IssueKey = issue.Key.Value,
                IssueTypeId = issue.Type.Id,
                IssueTypeName = issue.Type.Name,
                IssueName = GetIssueName(issue),
                DateFinish = issue.DueDate,
                DateFinishActual = issue.ResolutionDate,
                Assignee = issue.Assignee,
                IssueStatus = issue.Status.Name,
                IsSubTask = issue.Type.IsSubTask,
                DateStart = issue.Created,
            };

            if (project != null)
            {
                staging.ProjectId = project.Id;
                staging.ProjectKey = project.Key;
                staging.ProjectName = project.Name;
            }
            if (epicLinks.ContainsKey(issue.Key.Value))
            {
                staging.ParentEpicKey = epicLinks[issue.Key.Value].EpicLink;
                staging.ParentIssueKey = epicLinks[issue.Key.Value].EpicLink;
                if (!String.IsNullOrEmpty(epicLinks[issue.Key.Value].EpicLinkError))
                {
                    staging.RecordState = epicLinks[issue.Key.Value].EpicLinkError;
                }
            }
            if (timeTrackingData.ContainsKey(issue.Key.Value))
            {
                staging.OriginalEstimate = timeTrackingData[issue.Key.Value].OriginalEstimateInSeconds;
                staging.IssueActualWork = timeTrackingData[issue.Key.Value].TimeSpentInSeconds;
            }
            if (issue.FixVersions != null && issue.FixVersions.Count != 0)
            {
                staging.ParentVersionId = issue.FixVersions[0].Id;
                staging.ParentVersionName = issue.FixVersions[0].Name;
                staging.ParentVersionReleased = issue.FixVersions[0].IsReleased;
                staging.DateRelease = issue.FixVersions[0].ReleasedDate;
            }
            if (parentIssue != null)
            {
                staging.ParentIssueId = parentIssue.JiraIdentifier;
                staging.ParentIssueKey = parentIssue.Key.Value;
            }

            syncAllViewModel.StagingsToInsert.Add(staging);
            logger.Info($"Execute FillStagingTable syncAllViewModel.StagingsToInsert.Count {syncAllViewModel.StagingsToInsert.Count} staging.IssueKey: {staging.IssueKey} staging.IssueTypeName: {staging.IssueTypeName}");
            return staging;
        }

        private Master FillMasterTable(SystemToDbViewModel syncAllViewModel, SyncSystemDTO syncSystem, Issue issue, Issue parentIssue,
            Project project, Dictionary<string, JiraEpicLink> epicLinks, Dictionary<string, IssueTimeTrackingData> timeTrackingData)
        {
            logger.Info("Execute FillMasterTable");
            var master = new Master
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                SystemId = syncSystem.SystemId,
                IssueId = issue.JiraIdentifier,
                IssueKey = issue.Key.Value,
                IssueTypeId = issue.Type.Id,
                IssueTypeName = issue.Type.Name,
                IssueName = GetIssueName(issue),
                DateFinish = issue.DueDate,
                DateFinishActual = issue.ResolutionDate,
                Assignee = issue.Assignee,
                IssueStatus = issue.Status.Name,
                IsSubTask = issue.Type.IsSubTask,
                DateStart = issue.Created
            };
            if (project != null)
            {
                master.ProjectId = project.Id;
                master.ProjectKey = project.Key;
                master.ProjectName = project.Name;
            }
            if (epicLinks.ContainsKey(issue.Key.Value))
            {
                master.ParentEpicKey = epicLinks[issue.Key.Value].EpicLink;
                master.ParentIssueKey = epicLinks[issue.Key.Value].EpicLink;
            }
            if (timeTrackingData.ContainsKey(issue.Key.Value))
            {
                master.OriginalEstimate = timeTrackingData[issue.Key.Value].OriginalEstimateInSeconds;
                master.IssueActualWork = timeTrackingData[issue.Key.Value].TimeSpentInSeconds;
            }
            if (issue.FixVersions != null && issue.FixVersions.Count != 0)
            {
                master.ParentVersionId = issue.FixVersions[0].Id;
                master.ParentVersionName = issue.FixVersions[0].Name;
                master.ParentVersionReleased = issue.FixVersions[0].IsReleased;
                master.DateRelease = issue.FixVersions[0].ReleasedDate;
            }
            if (parentIssue != null)
            {
                master.ParentIssueId = parentIssue.JiraIdentifier;
                master.ParentIssueKey = parentIssue.Key.Value;
            }
            syncAllViewModel.MastersToInsert.Add(master);
            return master;
        }

        private MasterHistory FillMasterHistoryTable(SystemToDbViewModel syncAllViewModel, SyncSystemDTO syncSystem, Issue issue, Issue parentIssue,
           Project project, Dictionary<string, JiraEpicLink> epicLinks, Dictionary<string, IssueTimeTrackingData> timeTrackingData)
        {
            logger.Info("Execute FillMasterHistoryTable");
            var masterHistory = new MasterHistory
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                SystemId = syncSystem.SystemId,
                IssueId = issue.JiraIdentifier,
                IssueKey = issue.Key.Value,
                IssueTypeId = issue.Type.Id,
                IssueTypeName = issue.Type.Name,
                IssueName = GetIssueName(issue),
                DateFinish = issue.DueDate,
                Assignee = issue.Assignee,
                IssueStatus = issue.Status.Name,
                IsSubTask = issue.Type.IsSubTask,
                DateStart = issue.Created
            };
            if (project != null)
            {
                masterHistory.ProjectId = project.Id;
                masterHistory.ProjectKey = project.Key;
                masterHistory.ProjectName = project.Name;
            }
            if (epicLinks.ContainsKey(issue.Key.Value))
            {
                masterHistory.ParentEpicKey = epicLinks[issue.Key.Value].EpicLink;
                masterHistory.ParentIssueKey = epicLinks[issue.Key.Value].EpicLink;
            }
            if (timeTrackingData.ContainsKey(issue.Key.Value))
            {
                masterHistory.OriginalEstimate = timeTrackingData[issue.Key.Value].OriginalEstimateInSeconds;
                masterHistory.IssueActualWork = timeTrackingData[issue.Key.Value].TimeSpentInSeconds;
            }
            if (issue.FixVersions != null && issue.FixVersions.Count != 0)
            {
                masterHistory.ParentVersionId = issue.FixVersions[0].Id;
                masterHistory.ParentVersionName = issue.FixVersions[0].Name;
                masterHistory.ParentVersionReleased = issue.FixVersions[0].IsReleased;
                masterHistory.DateRelease = issue.FixVersions[0].ReleasedDate;
            }
            if (parentIssue != null)
            {
                masterHistory.ParentIssueId = parentIssue.JiraIdentifier;
                masterHistory.ParentIssueKey = parentIssue.Key.Value;
            }
            syncAllViewModel.MasterHistoriesToInsert.Add(masterHistory);
            return masterHistory;
        }

        private Staging FillStagingTable(SystemToDbViewModel syncAllViewModel, int systemId,
             ProjectVersion projectVersion, Project project)
        {
            logger.Info("Execute FillStagingTableAsync");
            var staging = new Staging
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                RecordState = RecordStateConst.New,
                ChangedFields = "all",
                WebHookEvent = "syncAll",
                SystemId = systemId,
                IssueId = projectVersion.Id,
                IssueKey = projectVersion.Id,
                IssueTypeId = "version",
                IssueTypeName = "version",
                IssueName = projectVersion.Name.RemoveNewLinesTabs(),
                ParentVersionReleased = projectVersion.IsReleased,
                DateFinish = projectVersion.ReleasedDate,
                DateRelease = projectVersion.ReleasedDate
            };
            if (project != null)
            {
                staging.ProjectId = project.Id;
                staging.ProjectKey = project.Key;
                staging.ProjectName = project.Name;
            }
            syncAllViewModel.StagingsToInsert.Add(staging);
            return staging;
        }

        private string GetIssueName(Issue issue)
        {
            string issueName = issue.Type.Name == "Epic"
                ? issue[jiraEpicNameField.Name]?.Value
                : issue.Summary.Trim();
            return issueName.RemoveNewLinesTabs();
        }

        private void FillMasterTable(SystemToDbViewModel syncAllViewModel, int systemId,
            ProjectVersion projectVersion, Project project)
        {
            logger.Info("Execute FillMasterTableAsync");
            var master = new Master
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                SystemId = systemId,
                IssueId = projectVersion.Id,
                IssueKey = projectVersion.Id,
                IssueTypeId = "version",
                IssueTypeName = "version",
                IssueName = projectVersion.Name.RemoveNewLinesTabs(),
                ParentVersionReleased = projectVersion.IsReleased,
                DateFinish = projectVersion.ReleasedDate,
                DateRelease = projectVersion.ReleasedDate
            };
            if (project != null)
            {
                master.ProjectId = project.Id;
                master.ProjectKey = project.Key;
                master.ProjectName = project.Name;
            }
            syncAllViewModel.MastersToInsert.Add(master);
        }

        private void FillMasterHistoryTable(SystemToDbViewModel syncAllViewModel, int systemId,
           ProjectVersion projectVersion, Project project)
        {
            logger.Info("Execute FillMasterHistoryTableAsync");
            var masterHistory = new MasterHistory
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                SystemId = systemId,
                IssueId = projectVersion.Id,
                IssueKey = projectVersion.Id,
                IssueTypeId = "version",
                IssueTypeName = "version",
                IssueName = projectVersion.Name.RemoveNewLinesTabs(),
                ParentVersionReleased = projectVersion.IsReleased,
                DateFinish = projectVersion.ReleasedDate,
                DateRelease = projectVersion.ReleasedDate
            };
            if (project != null)
            {
                masterHistory.ProjectId = project.Id;
                masterHistory.ProjectKey = project.Key;
                masterHistory.ProjectName = project.Name;
            }
            syncAllViewModel.MasterHistoriesToInsert.Add(masterHistory);
        }

        private void FillWorklogTables(SystemToDbViewModel syncAllViewModel,
            List<Worklog> worklogs, string issueId, int systemId)
        {
            logger.Info("Execute FillWorklogTables");
            foreach (Worklog worklog in worklogs)
            {
                MasterWorklog masterWorklog = new MasterWorklog
                {
                    SystemId = systemId,
                    RecordDateCreated = DateTime.Now,
                    RecordDateUpdated = DateTime.Now,
                    RecordState = RecordStateConst.New,
                    WebHookEvent = "syncAll",
                    WorkLogId = worklog.Id,
                    IssueId = issueId,
                    TimeSpentSeconds = worklog.TimeSpentInSeconds,
                    DateStarted = worklog.StartDate,
                    DateCreated = worklog.CreateDate,
                    DateUpdated = worklog.UpdateDate,
                    Comment = worklog.Comment,
                    AuthorName = worklog.Author,
                    AuthorKey = worklog.Author,
                    AuthorEmailAddress = worklog.Author,
                };
                syncAllViewModel.MasterWorklogsToInsert.Add(masterWorklog);
            }
        }
    }
}