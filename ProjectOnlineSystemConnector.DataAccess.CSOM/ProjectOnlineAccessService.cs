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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using Microsoft.CSharp;
using Microsoft.ProjectServer.Client;
using Microsoft.SharePoint.Client;
using NLog;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.DataAccess.CSOM
{
    public class ProjectOnlineAccessService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int RequestTimeout = 2700 * 1000; //Timeout.Infinite;
        private const int WaitForQueueTimeout = 2700 * 1000; //Int32.MaxValue;

        public ProjectContext ProjectContext { get; }
        public DateTime CurrentDateForCompare { get; }

        public TimeZoneInfo ProjectOnlineTimeZoneInfo { get; set; }

        private readonly bool isOnline;

        public string ProjectOnlineUrl { get; }

        private readonly Guid winServiceIterationUid;

        public ProjectOnlineAccessService(string projectOnlineUrl, string projectOnlineUserName,
            string projectOnlinePassword, bool isOnline, Guid winServiceIterationUid)
        {
            logger.LogAndSendMessage(null, "ProjectOnlineAccessService START",
                null, winServiceIterationUid,
                $"ProjectOnlineAccessService START ProjectOnlineUrl: {ProjectOnlineUrl}; " +
                $"projectOnlineUserName: {projectOnlineUserName}; projectOnlinePassword: {projectOnlinePassword}; " +
                $"isOnline: {isOnline};",
                false, null);

            ProjectOnlineUrl = projectOnlineUrl;
            this.isOnline = isOnline;
            this.winServiceIterationUid = winServiceIterationUid;
            var securePassword = new SecureString();
            foreach (char c in projectOnlinePassword)
            {
                securePassword.AppendChar(c);
            }
            if (isOnline)
            {
                ProjectContext = new ProjectContext(ProjectOnlineUrl)
                {
                    Credentials = new SharePointOnlineCredentials(projectOnlineUserName, securePassword)
                };
            }
            else
            {
                ProjectContext = new ProjectContext(ProjectOnlineUrl)
                {
                    Credentials = new NetworkCredential(projectOnlineUserName, projectOnlinePassword)
                };
            }
            ProjectContext.RequestTimeout = RequestTimeout;
            ProjectContext.PendingRequest.RequestExecutor.WebRequest.Timeout = RequestTimeout;
            ProjectContext.PendingRequest.RequestExecutor.WebRequest.ReadWriteTimeout = RequestTimeout;

            //SPClientCallableSettings

            //TODO:!!!!!!!!!!!!!
            //The underlying connection was closed: A connection that was expected to be kept alive was closed by the server." >> 
            //"Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."
            //In my case, this solved the problem: **System.Net.ServicePointManager.Expect100Continue = false; **
            ServicePointManager.Expect100Continue = false;

            //One more solution. Add into Register
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\ DWORD SynAttackProtect 00000000

            Web web = ProjectContext.Web;
            RegionalSettings regSettings = web.RegionalSettings;
            ProjectContext.Load(web);
            ProjectContext.Load(regSettings); //To get regional settings properties  
            Microsoft.SharePoint.Client.TimeZone projectOnlineTimeZone = regSettings.TimeZone;
            ProjectContext.Load(projectOnlineTimeZone);
            //To get the TimeZone propeties for the current web region settings  
            ExecuteQuery();
            TimeSpan projectOnlineUtcOffset =
                TimeSpan.Parse(projectOnlineTimeZone.Description.Substring(4,
                    projectOnlineTimeZone.Description.IndexOf(")", StringComparison.Ordinal) - 4));
            ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
            ProjectOnlineTimeZoneInfo =
                timeZones.FirstOrDefault(x => x.BaseUtcOffset == projectOnlineUtcOffset);
            CurrentDateForCompare = ProjectOnlineTimeZoneInfo != null
                ? TimeZoneInfo.ConvertTime(DateTime.Now, ProjectOnlineTimeZoneInfo)
                : DateTime.Now;
            logger.LogAndSendMessage(null, "ProjectOnlineAccessService END",
                null, winServiceIterationUid, "ProjectOnlineAccessService END", false, null);
        }

        #region CSOM

        public List<CustomField> GetCustomFields()
        {
            ProjectContext.Load(ProjectContext.CustomFields);
            ExecuteQuery();
            return ProjectContext.CustomFields.ToList();
        }

        public Dictionary<string, string> GetCustomFieldsDict()
        {
            if (ProjectOnlineCache.CustomFieldsDict == null)
            {
                List<CustomField> customFields = GetCustomFields();
                ProjectOnlineCache.CustomFieldsDict = customFields
                    .ToDictionary(customField => customField.Name, customField => customField.InternalName);
            }
            return ProjectOnlineCache.CustomFieldsDict;
        }

        public PublishedProject GetPublishedProject(Guid projectUid)
        {
            logger.LogAndSendMessage(null, "GetPublishedProject",
                projectUid, winServiceIterationUid,
                $"GetPublishedProject START projectUid: {projectUid}",
                false, null, CommonConstants.Epm, CommonConstants.Start);
            PublishedProject project = ProjectContext.Projects.GetByGuid(projectUid);
            ProjectContext.Load(project);
            if (isOnline)
            {
                ProjectContext.Load(project.CheckedOutBy);
            }
            ExecuteQuery();
            logger.LogAndSendMessage(null, "GetPublishedProject",
                projectUid, winServiceIterationUid,
                $"GetPublishedProject END project.Id: {project.Id}",
                false, null, CommonConstants.Epm, CommonConstants.End);
            return project;
        }

        public DraftProject CheckOutProject(PublishedProject publishedProject)
        {
            logger.LogAndSendMessage(null, "CheckOutProject",
                publishedProject.Id, winServiceIterationUid,
                $"CheckOutProject START publishedProject.Id: {publishedProject.Id}",
                false, null);
            DraftProject draftProject = publishedProject.CheckOut();
            try
            {
                draftProject = GetDraftProject(draftProject, publishedProject.Id);
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "CheckOutProject",
                    publishedProject.Id, winServiceIterationUid,
                    $"CheckOutProject publishedProject.Id: {publishedProject.Id}",
                    false, exception);
                if (draftProject != null)
                {
                    QueueJob job = draftProject.CheckIn(true);
                    JobState jobState;
                    WaitForQueue(job, publishedProject.Id, draftProject, out jobState);
                    draftProject = null;
                }
                //ExecuteQuery();
            }
            logger.LogAndSendMessage(null, "CheckOutProject",
                publishedProject.Id, winServiceIterationUid,
                $"CheckOutProject END publishedProject.Id: {publishedProject.Id}",
                false, null);
            return draftProject;
        }

        public DraftProject CheckOutProject(PublishedProject publishedProject, string systemNames, Guid projectUid)
        {
            DraftProject draftProject = null;
            try
            {
                if (publishedProject.IsCheckedOut)
                {
                    if (isOnline)
                    {
                        if (publishedProject.IsCheckedOut && publishedProject.CheckedOutBy?.Email != null)
                        {
                            EmailSender emailSender = new EmailSender();
                            emailSender.SendNotificationEmail(publishedProject.CheckedOutBy.Email,
                                $"Check-in project {publishedProject.Name} request",
                                $"Hello, {publishedProject.CheckedOutBy.Title}. This is 60i Administrator. I've found that several tasks in project {publishedProject.Name} should be synchronized with {systemNames}. " +
                                "Howerver I need to change some properties of these tasks, but project is checked-out to you. Please check-in it for a little time so I can do neccessary operations.");
                            return null;
                        }
                    }
                    return null;
                }
                draftProject = CheckOutProject(publishedProject);
                return draftProject;
            }
            //catch (PropertyOrFieldNotInitializedException exception)
            //{
            //    logger.LogAndSendMessage($"CheckOutProject ProjectUid: {projectUid}");
            //    logger.LogAndSendMessage(exception);
            //    if (draftProject != null)
            //    {
            //        logger.LogAndSendMessage("ForceCheckInProject");
            //        QueueJob job = draftProject.CheckIn(true);
            //        WaitForQueue(job, projectUid, draftProject);
            //        //ExecuteQuery();
            //    }
            //}
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "CheckOutProject",
                    projectUid, winServiceIterationUid,
                    $"CheckOutProject ProjectUid: {projectUid}",
                    false, exception);
                if (draftProject != null)
                {
                    logger.LogAndSendMessage(null, "ForceCheckInProject",
                        projectUid, winServiceIterationUid,
                        $"ForceCheckInProject {draftProject.Id}",
                        false, null);
                    QueueJob job = draftProject.CheckIn(true);
                    JobState jobState;
                    WaitForQueue(job, projectUid, draftProject, out jobState);
                }
            }
            return null;
        }

        public DraftProject GetDraftProject(PublishedProject publishedProject)
        {
            DraftProject draftProject = publishedProject.Draft;
            return GetDraftProject(draftProject, publishedProject.Id);
        }

        public DraftProject GetDraftProject(DraftProject draftProject, Guid projectUid)
        {
            logger.LogAndSendMessage(null, "GetDraftProject",
                projectUid, winServiceIterationUid,
                $"GetDraftProject START projectUid: {projectUid}",
                false, null, CommonConstants.Epm, CommonConstants.Start);
            ProjectContext.Load(draftProject);
            ProjectContext.Load(draftProject.Tasks);
            //ProjectContext.Load(draftProject.Assignments);
            //ProjectContext.Load(draftProject.Assignments, 
            //    a => a.IncludeWithDefaultProperties(assignment => assignment.Owner));
            ProjectContext.Load(draftProject.ProjectResources);
            ProjectContext.Load(draftProject.Tasks,
                c => c.IncludeWithDefaultProperties(t => t.Assignments,
                    t => t.Assignments.IncludeWithDefaultProperties(a => a.Resource)));
            //ProjectContext.Load(draftProject.Tasks,
            //    c => c.IncludeWithDefaultProperties(t => t.Assignments));
            ExecuteQuery();
            logger.LogAndSendMessage(null, "GetDraftProject",
                draftProject.Id, winServiceIterationUid,
                $"GetDraftProject END draftProject.Id: {draftProject.Id}",
                false, null, CommonConstants.Epm, CommonConstants.End);
            return draftProject;
        }

        public void UpdateProject(DraftProject draftProject, List<StagingDTO> affectedStagings)
        {
            if (draftProject == null)
            {
                return;
            }
            QueueJob job = null;
            JobState state = JobState.Unknown;
            try
            {
                logger.LogAndSendMessage(null, "UpdateProject",
                    draftProject.Id, winServiceIterationUid,
                    $"UpdateProject Update {draftProject.Id}",
                    false, null, CommonConstants.Epm, CommonConstants.Start);
                job = draftProject.Update();
                WaitForQueue(job, draftProject.Id, draftProject, out state);
                logger.LogAndSendMessage(null, "UpdateProject Update WaitForQueue",
                    draftProject.Id, winServiceIterationUid,
                    $"UpdateProject Update WaitForQueue {draftProject.Id}",
                    false, null);
                if (state == JobState.Success)
                {
                    if (affectedStagings != null)
                    {
                        foreach (StagingDTO affectedStaging in affectedStagings)
                        {
                            affectedStaging.RecordStateGeneral = RecordStateConst.Updated;
                        }
                    }
                    else
                    {
                        logger.LogAndSendMessage(null, "UpdateProject Update WaitForQueue",
                            draftProject.Id, winServiceIterationUid,
                            $"UpdateProject 1 WaitForQueue job failed. Project UID: {draftProject.Id}; jobState: {state}",
                            true, null);

                    }
                }
                logger.LogAndSendMessage(null, "UpdateProject",
                    draftProject.Id, winServiceIterationUid,
                    $"UpdateProject END {draftProject.Id}",
                    false, null, CommonConstants.Epm, CommonConstants.End);
            }
            catch (ServerException serverException)
            {
                logger.LogAndSendMessage(null, "UpdateProject",
                    draftProject.Id, winServiceIterationUid,
                    $"UpdateProject {draftProject.Id} JobState: {state}",
                    false, serverException);
                try
                {
                    if (job != null)
                    {
                        job.Cancel();
                        ProjectContext.Load(job);
                        ExecuteQuery();
                    }
                }
                catch (Exception exception)
                {
                    logger.LogAndSendMessage(null, "UpdateProject Cancel",
                        draftProject.Id, winServiceIterationUid,
                        $"UpdateProject Cancel {draftProject.Id} JobState: {state}",
                        false, exception);
                }
            }
        }

        public void PublishProject(DraftProject draftProject, List<StagingDTO> affectedStagings)
        {
            if (draftProject == null)
            {
                return;
            }
            JobState state = JobState.Unknown;
            logger.LogAndSendMessage(null, "PublishProject",
                draftProject.Id, winServiceIterationUid,
                $"PublishProject START projectGuid: {draftProject.Id}",
                false, null, CommonConstants.Epm, CommonConstants.Start);
            try
            {
                QueueJob job = draftProject.Publish(false);
                WaitForQueue(job, draftProject.Id, draftProject, out state);
                logger.LogAndSendMessage(null, "PublishProject Publish WaitForQueue",
                    draftProject.Id, winServiceIterationUid,
                    $"PublishProject Publish WaitForQueue {draftProject.Id}",
                    false, null);
                if (state == JobState.Success)
                {
                    if (affectedStagings != null)
                    {
                        foreach (StagingDTO affectedStaging in affectedStagings)
                        {
                            affectedStaging.RecordStateGeneral = RecordStateConst.Published;
                        }
                    }
                }
                else
                {
                    logger.LogAndSendMessage(null, "PublishProject Publish WaitForQueue",
                        draftProject.Id, winServiceIterationUid,
                        $"PublishProject Publish WaitForQueue job failed. Project UID: {draftProject.Id}; jobState: {state}",
                        true, null);
                }
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "PublishProject",
                    draftProject.Id, winServiceIterationUid,
                    $"PublishProject {draftProject.Id} JobState: {state}",
                    false, exception);
            }
            logger.LogAndSendMessage(null, "PublishProject",
                draftProject.Id, winServiceIterationUid,
                $"PublishProject END projectGuid: {draftProject.Id}",
                false, null, CommonConstants.Epm, CommonConstants.End);
        }

        public EnterpriseResource GetEnterpriseResource(Guid resourceGuid)
        {
            ProjectContext.Load(ProjectContext.EnterpriseResources,
                c => c.Where(x => x.Id == resourceGuid)
                    .IncludeWithDefaultProperties(r => r.User)
                    .IncludeWithDefaultProperties(r => r.Assignments));
            ExecuteQuery();
            return ProjectContext.EnterpriseResources.FirstOrDefault();
        }

        public List<EnterpriseResource> GetEnterpriseResources(List<Guid> resGuids)
        {
            if (resGuids.Count == 0)
            {
                return new List<EnterpriseResource>();
            }

            string resGuidsStr = resGuids.Aggregate("", (current, resGuid) => current + $", {resGuid}").Trim(',');
            logger.LogAndSendMessage(null, "GetEnterpriseResources",
                null, winServiceIterationUid,
                $"GetEnterpriseResources resGuids: {resGuids.Count}; resGuidsStr: {resGuidsStr}",
                false, null);

            StringBuilder strBuilder = BuildClass(resGuids);
            CompilerResults compilerResults = CompileAssembly(strBuilder);
            List<EnterpriseResource> resultDynamic = RunCode(compilerResults);

            logger.LogAndSendMessage(null, "GetEnterpriseResources",
                null, winServiceIterationUid,
                $"GetEnterpriseResources resultDynamic: {resultDynamic.Count}",
                false, null);
            return resultDynamic;
        }

        #region Dynamic code

        private StringBuilder BuildClass(List<Guid> resGuidsToTake)
        {
            // need a string to put the code into
            var source = new StringBuilder();
            var sw = new StringWriter(source);
            //Declare your provider and generator
            var codeProvider = new CSharpCodeProvider();
            ICodeGenerator generator = codeProvider.CreateGenerator(sw);
            var codeOpts = new CodeGeneratorOptions();
            var codeNamespace = new CodeNamespace("WebHookData.DataAccess.ExternalAPI");
            codeNamespace.Imports.Add(new CodeNamespaceImport(nameof(System)));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            //Build the class declaration and member variables
            var classDeclaration = new CodeTypeDeclaration
            {
                IsClass = true,
                Name = "ProjectOnlineAccessService",
                Attributes = MemberAttributes.Public,
                IsPartial = true
            };
            CodeMemberMethod method = BuildMethod(resGuidsToTake);
            classDeclaration.Members.Add(method);
            //write code
            codeNamespace.Types.Add(classDeclaration);
            generator.GenerateCodeFromNamespace(codeNamespace, sw, codeOpts);
            // cleanup
            sw.Flush();
            sw.Close();
            return source;
        }

        private CodeMemberMethod BuildMethod(List<Guid> resGuidsToTake)
        {
            var method = new CodeMemberMethod
            {
                Name = "GetResourcesDynamic",
                ReturnType = new CodeTypeReference(typeof(List<IEnumerable<EnterpriseResource>>)),
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ProjectContext), "ProjectContext"));
            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(List<IEnumerable<EnterpriseResource>>), "result"));
            CodeExpression newExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(List<IEnumerable<EnterpriseResource>>)));
            CodeStatement resultInitExpression = new CodeAssignStatement(new CodeVariableReferenceExpression("result"), newExpression);
            method.Statements.Add(resultInitExpression);

            int maxPredicateParts = 20, predicateIndex = -1;
            string varDeclaration = "";
            List<string> predicates = new List<string>();

            for (int i = 0; i < resGuidsToTake.Count; i++)
            {
                varDeclaration = varDeclaration + $"Guid guid{i} = Guid.Parse(\"{resGuidsToTake[i]}\");{Environment.NewLine}";
                if (i % maxPredicateParts == 0)
                {
                    predicates.Add($"guid{i} == ept.Id");
                    predicateIndex++;
                }
                else
                {
                    predicates[predicateIndex] = predicates[predicateIndex] + $" || guid{i} == ept.Id";
                }
            }
            string sourceCode = varDeclaration;

            foreach (string predicate in predicates)
            {
                sourceCode = sourceCode + $"result.Add(ProjectContext.LoadQuery(ProjectContext.EnterpriseResources.Where(ept => {predicate})));{Environment.NewLine}";
            }
            sourceCode = sourceCode + $"ProjectContext.ExecuteQuery();{Environment.NewLine}";
            method.Statements.Add(new CodeSnippetExpression(sourceCode));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("result")));
            return method;
        }

        private CompilerResults CompileAssembly(StringBuilder source)
        {
            CodeDomProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parms = CreateCompilerParameters();
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parms, source.ToString());
            if (results.Errors.Count > 0)
            {
                foreach (CompilerError error in results.Errors)
                {
                    logger.LogAndSendMessage(null, "CompileAssembly",
                        null, winServiceIterationUid,
                        $"CompileAssembly Error: {error.ErrorText}",
                        false, null);
                }
                return null;
            }
            return results;
        }

        private CompilerParameters CreateCompilerParameters()
        {
            //add compiler parameters and assembly references
            var compilerParams = new CompilerParameters
            {
                //CompilerOptions = "/target:library /optimize",
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false
            };
            compilerParams.ReferencedAssemblies.Add(typeof(IQueryable).Assembly.Location);
            compilerParams.ReferencedAssemblies.Add(typeof(ClientObject).Assembly.Location);
            compilerParams.ReferencedAssemblies.Add(typeof(EnterpriseResource).Assembly.Location);
            compilerParams.ReferencedAssemblies.Add(typeof(ClientContext).Assembly.Location);

            return compilerParams;
        }

        private List<EnterpriseResource> RunCode(CompilerResults compilerResults)
        {
            var result = new List<EnterpriseResource>();
            if (compilerResults?.CompiledAssembly == null)
            {
                return result;
            }
            Assembly executingAssembly = compilerResults.CompiledAssembly;
            Type type = executingAssembly.GetModules()[0]
                .GetType("WebHookData.DataAccess.ExternalAPI.ProjectOnlineAccessService");
            if (type != null)
            {
                MethodInfo methodInfo = type.GetMethod("GetResourcesDynamic", BindingFlags.Public | BindingFlags.Static);
                var resultDynamic = (List<IEnumerable<EnterpriseResource>>)methodInfo.Invoke(null, new object[] { ProjectContext });
                foreach (IEnumerable<EnterpriseResource> enterpriseResources in resultDynamic)
                {
                    result.AddRange(enterpriseResources);
                }
            }
            return result;
        }

        #endregion

        public bool WaitForQueue(QueueJob job, Guid projectUid, DraftProject draftProject, out JobState jobState)
        {
            logger.LogAndSendMessage(null, "WaitForQueue",
                projectUid, winServiceIterationUid,
                $"WaitForQueue {projectUid}",
                false, null);
            jobState = ProjectContext.WaitForQueue(job, WaitForQueueTimeout);
            return jobState == JobState.Success;
        }

        public void ExecuteQuery()
        {
            ProjectContext.ExecuteQuery();
        }

        public void CheckInProject(DraftProject draftProject, Guid projectGuid)
        {
            if (draftProject == null)
            {
                return;
            }
            QueueJob job;
            JobState jobState;
            logger.LogAndSendMessage(null, "CheckInProject",
                projectGuid, winServiceIterationUid,
                $"CheckInProject START projectGuid: {projectGuid}",
                false, null, CommonConstants.Epm, CommonConstants.Start);

            try
            {
                logger.LogAndSendMessage(null, "CheckInProject CheckIn",
                    projectGuid, winServiceIterationUid,
                    $"CheckInProject CheckIn projectGuid: {projectGuid}",
                    false, null);
                job = draftProject.CheckIn(false);
                if (!WaitForQueue(job, draftProject.Id, draftProject, out jobState))
                {
                    logger.LogAndSendMessage(null, "CheckInProject CheckIn WaitForQueue",
                        projectGuid, winServiceIterationUid,
                        $"CheckInProject CheckIn WaitForQueue job failed. Project UID: {projectGuid}; " +
                        $"jobState: {jobState}",
                        true, null);
                }
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "CheckInProject",
                    projectGuid, winServiceIterationUid,
                    $"CheckInProject Exception {projectGuid}",
                    false, exception);
                logger.LogAndSendMessage(null, "ForceCheckInProject",
                    projectGuid, winServiceIterationUid,
                    $"ForceCheckInProject projectGuid: {projectGuid}",
                    false, null);
                job = draftProject.CheckIn(true);
                WaitForQueue(job, draftProject.Id, draftProject, out jobState);
            }
            logger.LogAndSendMessage(null, "CheckInProject",
                projectGuid, winServiceIterationUid,
                $"CheckInProject END projectGuid: {projectGuid}",
                false, null, CommonConstants.Epm, CommonConstants.End);
        }

        public void ClearCache()
        {
            ProjectOnlineCache.CustomFieldsDict = null;
        }

        public void InitCache()
        {
            try
            {
                GetCustomFieldsDict();
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "InitCache",
                    null, winServiceIterationUid,
                    "InitCache Exception",
                    false, exception);
            }
        }

        #endregion
    }
}