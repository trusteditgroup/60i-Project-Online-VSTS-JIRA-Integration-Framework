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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Atlassian.Jira;
using NLog;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataModel.JiraWebHook;
using RestSharp;

namespace ProjectOnlineSystemConnector.DataAccess.Jira
{
    public class JiraAccessService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public SyncSystemDTO SyncSystem { get; }
        public Atlassian.Jira.Jira JiraConnection { get; }
        private readonly string jiraUserName, jiraPassword, jiraApiUrl;

        public JiraAccessService(SyncSystemDTO syncSystem)
        {
            SyncSystem = syncSystem;
            jiraUserName = syncSystem.SystemLogin;
            jiraPassword = syncSystem.SystemPassword;
            jiraApiUrl = syncSystem.SystemApiUrl.Trim('/');
            JiraRestClientSettings jiraRestClientSettings = new JiraRestClientSettings();
            jiraRestClientSettings.CustomFieldSerializers.Remove("com.pyxis.greenhopper.jira:gh-sprint");
            JiraConnection = Atlassian.Jira.Jira.CreateRestClient(syncSystem.SystemUrl, jiraUserName, jiraPassword, jiraRestClientSettings);
        }

        private string EncodeTo64(string userName, string password)
        {
            string mergedCredentials = $"{userName}:{password}";
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(mergedCredentials);
            string returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public async Task<string> GetJiraResponse(JiraProxyRequest jiraProxyRequest)
        {
            logger.Info("GetJiraResponse " + jiraApiUrl + "/" + jiraProxyRequest.ApiUrl.Trim('/'));
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(jiraApiUrl + "/" + jiraProxyRequest.ApiUrl.Trim('/'));
            httpWebRequest.Method = jiraProxyRequest.RequestType;
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add("Authorization", "Basic " + EncodeTo64(jiraUserName, jiraPassword));
            if ((jiraProxyRequest.RequestType == "POST" || jiraProxyRequest.RequestType == "PUT") &&
                !String.IsNullOrEmpty(jiraProxyRequest.PostData))
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(jiraProxyRequest.PostData);
                httpWebRequest.ContentLength = byteArray.Length;
                Stream dataStream = httpWebRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }
            HttpWebResponse httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
            Stream responseStream = httpWebResponse.GetResponseStream();
            if (responseStream != null)
            {
                StreamReader streamReader = new StreamReader(responseStream);
                string response = await streamReader.ReadToEndAsync();
                return response;
            }
            return null;
        }

        #region Projects

        public async Task<List<Project>> GetProjects()
        {
            return JiraCache.Projects ??
                   (JiraCache.Projects =
                       (await JiraConnection.Projects.GetProjectsAsync()).Take(int.MaxValue).ToList());
        }

        public async Task<Project> GetProjectById(string projectId)
        {
            Project project = (await GetProjects()).FirstOrDefault(x => x.Id == projectId);
            if (project == null)
            {
                JiraCache.Projects = null;
                return (await GetProjects()).FirstOrDefault(x => x.Id == projectId);
            }
            return project;
        }

        public async Task<Project> GetProjectByKey(string projectKey)
        {
            Project project = (await GetProjects()).FirstOrDefault(x => x.Key == projectKey);
            if (project == null)
            {
                JiraCache.Projects = null;
                return (await GetProjects()).FirstOrDefault(x => x.Key == projectKey);
            }
            return project;
        }

        public async Task<List<Project>> GetProjectsByKeys(List<string> projectKeys)
        {
            JiraCache.Projects = null;
            List<Project> projects = (await GetProjects()).Where(x => projectKeys.Contains(x.Key)).ToList();
            return projects;
        }

        #endregion

        #region Issues

        public async Task<List<Issue>> GetIssuesAsync(string jql, string updateIssueStartDate, SyncSystemDTO syncSystem)
        {
            int itemsPerPage = 1000;
            int counter = 0;
            List<Issue> result = new List<Issue>();
            if (syncSystem.ActualsStartDate.HasValue)
            {
                jql = $"({jql}) and ((updated >= {updateIssueStartDate} and issuetype != 'Epic') or issuetype = 'Epic')";
            }
            logger.Info($"GetIssuesAsync jql: {jql}");
            IPagedQueryResult<Issue> query = await JiraConnection.Issues
                    .GetIssuesFromJqlAsync(jql, itemsPerPage, counter);

            logger.Info($"GetIssuesAsync counter: {counter}; query.TotalItems: {query.TotalItems}; " +
                        $"query.ItemsPerPage: {query.ItemsPerPage}; query.StartAt: {query.StartAt}");

            result.AddRange(query.ToList());

            counter += itemsPerPage;

            while (query.TotalItems >= counter)
            {
                logger.Info("GetIssuesAsync while1 counter " + counter);

                query = await JiraConnection.Issues
                    .GetIssuesFromJqlAsync(jql, itemsPerPage, counter);
                counter += itemsPerPage;

                logger.Info($"GetIssuesAsync while2 counter: {counter}; query.TotalItems: {query.TotalItems}; " +
                    $"query.ItemsPerPage: {query.ItemsPerPage}; query.StartAt: {query.StartAt}");

                result.AddRange(query.ToList());
            }
            logger.Info("GetIssuesAsync result.Count " + result.Count);
            return result;
        }

        #endregion

        #region CustomFields

        public async Task<IEnumerable<CustomField>> GetCustomFieldsFromJira()
        {
            logger.Info("GetCustomFieldsFromJira");
            return await JiraConnection.Fields.GetCustomFieldsAsync();
        }

        public async Task<List<CustomField>> GetCustomFields()
        {
            if (JiraCache.SystemCustomFields == null)
            {
                JiraCache.SystemCustomFields = new Dictionary<int, List<CustomField>>();
            }
            if (!JiraCache.SystemCustomFields.ContainsKey(SyncSystem.SystemId))
            {
                var customFields = await JiraConnection.Fields.GetCustomFieldsAsync();
                JiraCache.SystemCustomFields.Add(SyncSystem.SystemId, customFields.ToList());
                logger.Info("GetCustomFields JiraCacheHelper.SystemCustomFields.Count " + JiraCache.SystemCustomFields.Count);
            }
            return JiraCache.SystemCustomFields[SyncSystem.SystemId];
        }

        #endregion

        #region Versions

        public async Task<IEnumerable<ProjectVersion>> GetVersionsAsync(string projectKey)
        {
            return await JiraConnection.Versions.GetVersionsAsync(projectKey);
        }

        #endregion

        #region WorkLog

        public async Task<JiraWorklog> GetJiraWorkLog(string issueId, string workLogId)
        {
            string resultQuery = "rest/api/latest/issue/" + issueId + "/worklog/" + workLogId;
            logger.Info("JiraApiUrl: " + jiraApiUrl);
            logger.Info("GetJiraWorkLog: " + resultQuery);
            JiraWorklog jiraWorklog = await JiraConnection.RestClient.ExecuteRequestAsync<JiraWorklog>(Method.GET, resultQuery);
            return jiraWorklog;
        }

        #endregion

        #region Sprint

        public JiraSprint ParseSprintField(string[] sprintsStrings)
        {
            //var sprintArray = (JArray)JsonConvert.DeserializeObject(fieldValue);
            List<JiraSprint> sprints = new List<JiraSprint>();
            foreach (string sprintString in sprintsStrings)
            {
                Match match = Regex.Match(sprintString, @"id=(?<id>.*),rapidViewId=(?<rapidViewId>.*),state=(?<state>.*),name=(?<name>.*),goal=(?<goal>.*),startDate=(?<startDate>.*),endDate=(?<endDate>.*),completeDate=(?<completeDate>.*),sequence=(?<sequence>.*$)");
                JiraSprint jiraSprint = new JiraSprint
                {
                    Id = match.Groups["id"].ToString(),
                    Name = match.Groups["name"].ToString(),
                    State = match.Groups["state"].ToString(),
                    StartDate = match.Groups["startDate"].ToString(),
                    EndDate = match.Groups["endDate"].ToString()
                };
                sprints.Add(jiraSprint);
                logger.Info($"ParseSprintField jiraSprint.Id: {jiraSprint.Id} " +
                            $"jiraSprint.Name: {jiraSprint.Name} " +
                            $"jiraSprint.State: {jiraSprint.State} " +
                            $"jiraSprint.StartDate: {jiraSprint.StartDate} " +
                            $"jiraSprint.EndDate: {jiraSprint.EndDate}");
            }

            JiraSprint resultSprint = (sprints.FirstOrDefault(x => x.State == "ACTIVE")
                    ?? sprints.FirstOrDefault(x => x.State == "FUTURE"))
                ?? (sprints.Where(x => !String.IsNullOrEmpty(x.EndDate) && x.EndDate != "<null>")
                        .OrderByDescending(x => DateTime.Parse(x.EndDate)).FirstOrDefault()
                    ?? sprints.FirstOrDefault());
            return resultSprint;
        }

        #endregion

        public void RefreshCache()
        {
            JiraCache.SystemCustomFields = null;
            JiraCache.Projects = null;
            Task.WaitAll(GetProjects(), GetCustomFields());
        }
    }
}