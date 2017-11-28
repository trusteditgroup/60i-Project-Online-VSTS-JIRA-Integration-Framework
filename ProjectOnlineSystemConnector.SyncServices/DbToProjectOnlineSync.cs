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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.ProjectServer.Client;
using Microsoft.SharePoint.Client;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataModel.OData;
using ProjectOnlineSystemConnector.SyncServices.BL;
using ProjectOnlineSystemConnector.SyncServices.DataModel;
using ThreadingTask = System.Threading.Tasks.Task;

namespace ProjectOnlineSystemConnector.SyncServices
{
    public class DbToProjectOnlineSync
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly StagingBusinessService stagingBusinessService;
        private readonly MasterWorklogBusinessService masterWorklogBusinessService;
        private ThreadingTask[] processingTasks;
        private readonly int publishMax, stagingRecordLifeTime, projectsPerIteration;
        private readonly UnitOfWork unitOfWork;
        private readonly Guid winServiceIterationUid;
        private readonly ProjectOnlineODataService projectOnlineODataService;

        public DbToProjectOnlineSync(UnitOfWork unitOfWork, ProjectOnlineODataService projectOnlineODataService,
            int publishMax, int stagingRecordLifeTime, int projectsPerIteration, Guid winServiceIterationUid)
        {
            //stagingBusinessService = new StagingBusinessService(unitOfWork);
            //syncSystemBusinessService = new SyncSystemBusinessService(unitOfWork);
            //projectServerSystemLinkBusinessService = new ProjectServerSystemLinkBusinessService(unitOfWork);
            //stagingFieldMappingValueBusinessService = new StagingFieldMappingValueBusinessService(unitOfWork);
            //masterWorklogBusinessService = new MasterWorklogBusinessService(unitOfWork);

            //this.projectOnlineAccessService = projectOnlineAccessService;

            //this.publishMax = publishMax;
            //this.stagingRecordLifeTime = stagingRecordLifeTime;
            //this.projectsPerIteration = projectsPerIteration;
            this.projectOnlineODataService = projectOnlineODataService;

            logger.LogAndSendMessage(null, "DataSyncJira.ctor START", null,
                winServiceIterationUid, "DataSyncJira.ctor START", false, null);

            this.winServiceIterationUid = winServiceIterationUid;
            this.unitOfWork = unitOfWork;
            stagingBusinessService = new StagingBusinessService(unitOfWork);
            masterWorklogBusinessService = new MasterWorklogBusinessService(unitOfWork);

            this.publishMax = publishMax;
            this.stagingRecordLifeTime = stagingRecordLifeTime;
            this.projectsPerIteration = projectsPerIteration;

            logger.LogAndSendMessage(null, "DataSyncJira.ctor END", null, winServiceIterationUid,
                "DataSyncJira.ctor END", false, null);
        }

        public void SynchronizeData(object state)
        {
            HelperMethods.InitCsvLog();

            logger.LogAndSendMessage(null, "SynchronizeData START", null, winServiceIterationUid,
                "SynchronizeData START", false, null);

            logger.LogAndSendMessage(null, "GetDataFromDbViewModel", null,
                winServiceIterationUid, "GetDataFromDbViewModel START", false, null, CommonConstants.SixtyI, CommonConstants.Start);
            DataFromDbViewModel dataFromDbViewModel = DataBusinessService
                .GetDataFromDbViewModel(projectOnlineODataService, unitOfWork, false);
            logger.LogAndSendMessage(null, "GetDataFromDbViewModel", null,
                winServiceIterationUid, "GetDataFromDbViewModel END", false, null, CommonConstants.SixtyI, CommonConstants.End);

            logger.LogAndSendMessage(null, "GetProjectsToCheckout", null,
                winServiceIterationUid, "GetProjectsToCheckout START", false, null);
            GetProjectsToCheckout(dataFromDbViewModel);
            logger.LogAndSendMessage(null, "GetProjectsToCheckout", null,
                winServiceIterationUid, "GetProjectsToCheckout END", false, null);
            ProcessProjects(dataFromDbViewModel);
            ReturnDataToDb(dataFromDbViewModel);

            logger.LogAndSendMessage(null, "SynchronizeData END", null, winServiceIterationUid,
                "SynchronizeData END", false, null);
            try
            {
                HelperMethods.WriteToCsvLog();
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }

        private void ReturnDataToDb(DataFromDbViewModel dataFromDbViewModel)
        {
            logger.LogAndSendMessage(null, "ReturnDataToDb", null,
                winServiceIterationUid,
                "SynchronizeData ReturnDataToDb START", false, null, CommonConstants.SixtyI, CommonConstants.Start);

            try
            {
                foreach (StagingDTO staging in dataFromDbViewModel.StagingsAll)
                {
                    if (staging.RecordStateGeneral == RecordStateConst.Done)
                    {
                        List<StagingDTO> oldStagings = dataFromDbViewModel.StagingsAll
                            .Where(x => x.IssueKey == staging.IssueKey
                                        && x.SystemId == staging.SystemId
                                        && x.StagingId != staging.StagingId
                                        && x.RecordStateGeneral != RecordStateConst.Done)
                            .ToList();
                        foreach (StagingDTO oldStaging in oldStagings)
                        {
                            oldStaging.RecordStateGeneral = RecordStateConst.Done;
                        }
                    }
                }

                List<int> stagingToDeleteIds = dataFromDbViewModel.StagingsAll
                    .Where(x => x.RecordStateGeneral == RecordStateConst.Done
                                && x.RecordStateActual == RecordStateConst.Done)
                    .Select(x => x.StagingId).ToList();
                List<int> stagingToSetDoneIds = dataFromDbViewModel.StagingsAll
                    .Where(x => x.RecordStateGeneral == RecordStateConst.Done
                                && x.RecordStateActual != RecordStateConst.Done)
                    .Select(x => x.StagingId).ToList();

                DateTime borderDate = DateTime.Now.AddHours(-stagingRecordLifeTime);

                stagingBusinessService.RemoveStagings(stagingToDeleteIds, borderDate);
                stagingBusinessService.SetStagingsRecordState(stagingToSetDoneIds,
                    ProjectServerConstants.RecordStateGeneral, RecordStateConst.Done);

                List<int> worklogsToDelete = new List<int>();
                if (dataFromDbViewModel.MasterWorklogs != null)
                {
                    worklogsToDelete = dataFromDbViewModel.MasterWorklogs
                        .Where(x => x.RecordState == RecordStateConst.Done)
                        .Select(x => x.MasterWorklogId)
                        .ToList();
                    masterWorklogBusinessService.RemoveMasterWorklogs(worklogsToDelete);
                }
                unitOfWork.SaveChanges();
                logger.LogAndSendMessage(null, "ReturnDataToDb", null,
                    winServiceIterationUid,
                    $"SynchronizeData ReturnDataToDb END stagingsAll: {dataFromDbViewModel.StagingsAll.Count}; " +
                    $"stagingToDeleteIds: {stagingToDeleteIds.Count}; " +
                    //$"stagingToRestoreIds: {stagingToRestoreIds.Count}; " +
                    $"stagingToSetDoneIds: {stagingToSetDoneIds.Count}; " +
                    $"worklogsToDelete: {worklogsToDelete.Count}; ", false, null, CommonConstants.SixtyI,
                    CommonConstants.End);
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }

        private void ProcessProjects(DataFromDbViewModel dataFromDbViewModel)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                logger.LogAndSendMessage(null, "SynchronizeData ThreadingTask.WaitAll 1 START",
                    null, winServiceIterationUid,
                    $"SynchronizeData ThreadingTask.WaitAll 1 START stopwatch.ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}; " +
                    $"dataFromDbViewModel.ProjectInfos: {dataFromDbViewModel.ProjectInfos.Count}", false, null);
                int skip = 0;

                while (true)
                {
                    List<ProjectInfo> projectInfosOnPage = dataFromDbViewModel.ProjectInfos
                        .Skip(skip).Take(projectsPerIteration).ToList();
                    if (projectInfosOnPage.Count != 0)
                    {
                        logger.LogAndSendMessage(null, "SynchronizeData ThreadingTask.WaitAll 2 START",
                            null, winServiceIterationUid,
                            $"SynchronizeData ThreadingTask.WaitAll 2 START {stopwatch.ElapsedMilliseconds}; " +
                            $"dataFromDbViewModel.ProjectInfos: {dataFromDbViewModel.ProjectInfos.Count}; " +
                            $"projectsOnPage: {projectInfosOnPage.Count}; skip: {skip}; take: {projectsPerIteration}",
                            false, null);
                        processingTasks = projectInfosOnPage
                            .Select(projectInfo => ThreadingTask.Run(async () =>
                            {
                                logger.LogAndSendMessage(null, "ProcessProject",
                                    projectInfo.ProjectGuid, winServiceIterationUid,
                                    $"ProcessProject START {projectInfo.ProjectGuid}",
                                    false, null);
                                await ThreadingTask.Delay(1000 * 3 * 1);
                                ProcessProject(projectInfo, dataFromDbViewModel);
                                logger.LogAndSendMessage(null, "ProcessProject",
                                    projectInfo.ProjectGuid, winServiceIterationUid,
                                    $"ProcessProject END {projectInfo.ProjectGuid}",
                                    false, null);
                            })).ToArray();
                        ThreadingTask.WaitAll(processingTasks);
                        processingTasks = null;
                        skip += projectsPerIteration;
                    }
                    else
                    {
                        break;
                    }
                }
                stopwatch.Stop();
                logger.LogAndSendMessage(null, "SynchronizeData ThreadingTask.WaitAll END",
                    null, winServiceIterationUid,
                    $"SynchronizeData ThreadingTask.WaitAll END stopwatch.ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}; " +
                    $"dataFromDbViewModel.ProjectInfos: {dataFromDbViewModel.ProjectInfos.Count}", false, null);
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }

        private void ProcessProject(ProjectInfo projectInfo, DataFromDbViewModel dataFromDbViewModel)
        {
            List<IGrouping<int, ProjectServerSystemLinkDTO>> linksForProjectGrouped = dataFromDbViewModel
                .ProjectServerSystemLinks.Where(x => x.ProjectUid == projectInfo.ProjectGuid)
                .GroupBy(x => x.SystemId)
                .ToList();
            //logger.LogAndSendMessage(null, "ProcessProject",
            //    projectInfo.ProjectGuid, winServiceIterationUid,
            //    $"ProcessProject projectInfo.ProjectGuid: {projectInfo.ProjectGuid}; " +
            //    $"linksForProjectGrouped: {linksForProjectGrouped.Count}",
            //    false, null);

            try
            {
                List<int> systemIds = linksForProjectGrouped.Select(x => x.Key).ToList();
                List<SyncSystemDTO> workingSystems =
                    dataFromDbViewModel.SyncSystems.Where(x => systemIds.Contains(x.SystemId)).ToList();
                string systemNames =
                    workingSystems.Aggregate("", (current, workingSystem) => current + (workingSystem + ", ")).Trim(',');
                //DraftProject draftProject = projectOnlineAccessService.GetPublishedProject(projectUid);
                projectInfo.DraftProject =
                    projectInfo.ProjectOnlineAccessService.CheckOutProject(projectInfo.PublishedProject, systemNames,
                        projectInfo.ProjectGuid);
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "ProcessProject ERROR",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"ProcessProject : {projectInfo.ProjectGuid}",
                    false, exception);
            }

            if (projectInfo.DraftProject == null)
            {
                logger.LogAndSendMessage(null, "ProcessProjec INFO",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"ProcessProject draftProject is null: {projectInfo.ProjectGuid}",
                    false, null);
                return;
            }

            try
            {
                //draftProject = CheckDefaultSystemsTasks(linksForProjectGrouped, dataFromDbViewModel.SyncSystems, draftProject);
                logger.LogAndSendMessage(null, "ProcessProject INFO",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"ProcessProject ProjectUid: {projectInfo.ProjectGuid}; syncSystems: {dataFromDbViewModel.SyncSystems.Count}; " +
                    $"draftProject.Tasks.Count: {projectInfo.DraftProject.Tasks.Count}; ",
                    false, null);
                int globalPublishCounter = 0;
                projectInfo.DraftProject = AddNewTasks(projectInfo, dataFromDbViewModel, ref globalPublishCounter);
                projectInfo.DraftProject = UpdateTasks(projectInfo, dataFromDbViewModel, ref globalPublishCounter);
                projectInfo.DraftProject = SetAssignments(projectInfo, dataFromDbViewModel, ref globalPublishCounter);
                if (globalPublishCounter != 0)
                {
                    string message =
                        $"ProcessProject PublishProject draftProject.Id: {projectInfo.DraftProject.Id}; " +
                        $"draftProject.Name: {projectInfo.DraftProject.Name}; globalPublishCounter: {globalPublishCounter}";

                    PublishProject(projectInfo, dataFromDbViewModel.StagingsAll, message);
                }
            }
            catch (Exception exception)
            {
                logger.LogAndSendMessage(null, "ProcessProject ERROR",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"================ ProcessProject publishedProject.Id: {projectInfo.ProjectGuid} ===================",
                    false, exception);
            }
            finally
            {
                try
                {
                    projectInfo.ProjectOnlineAccessService.CheckInProject(projectInfo.DraftProject, projectInfo.ProjectGuid);
                }
                catch (Exception exception)
                {
                    logger.LogAndSendMessage(null, "projectInfo.ProjectOnlineAccessService.CheckInProject",
                        projectInfo.ProjectGuid, winServiceIterationUid,
                        $"projectInfo.ProjectOnlineAccessService.CheckInProject Exception {projectInfo.ProjectGuid}",
                        false, exception);
                }

                logger.LogAndSendMessage(null, "ProcessProject INFO",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"ProcessProject Published->Done projectInfo.ProjectGuid: {projectInfo.ProjectGuid}; " +
                    $"stagingsToProcess: {dataFromDbViewModel.StagingsAll.Count}",
                    false, null);
                foreach (StagingDTO staging in dataFromDbViewModel.StagingsAll)
                {
                    if (staging.RecordStateGeneral == RecordStateConst.Published)
                    {
                        staging.RecordStateGeneral = RecordStateConst.Done;
                    }
                }
            }
        }

        private DraftProject AddNewTasks(ProjectInfo projectInfo, DataFromDbViewModel dataFromDbViewModel, ref int globalPublishCounter)
        {
            if (!dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                return projectInfo.DraftProject;
            }
            foreach (KeyValuePair<int, List<TaskInfo>> keyValuePair in dataFromDbViewModel.NewTaskChangesTracker[projectInfo.DraftProject.Id])
            {
                int updateCounter = 0;
                string message =
                    $"ProcessProject AddNewTasks UpdateProject draftProject.Id: {projectInfo.DraftProject.Id}; " +
                    $"draftProject.Name: {projectInfo.DraftProject.Name}; ";
                foreach (TaskInfo taskInfo in keyValuePair.Value)
                {

                    DraftTask parentTask = null;
                    string name;
                    if (!string.IsNullOrEmpty(taskInfo.DefaultParentTaskName))
                    {
                        //default system task
                        name = taskInfo.DefaultParentTaskName;
                    }
                    else
                    {
                        SyncSystemDTO syncSystem = dataFromDbViewModel.SyncSystems
                            .FirstOrDefault(x => x.SystemId == taskInfo.MainStaging.SystemId);
                        if (syncSystem == null)
                        {
                            continue;
                        }
                        //parentTask = taskInfo.IsHomeProject ||
                        //             taskInfo.MainStaging.IssueTypeName == "Epic"
                        //    //project link case or this is linked epic. Parent task = default parent task
                        //    ? GetDraftTask(projectInfo.DraftProject, syncSystem.DefaultParentTaskName)
                        //    //epic link case. Parent task is epic
                        //    : GetDraftTask(projectInfo.DraftProject, taskInfo.MainStaging.SystemId,
                        //        taskInfo.MainStaging.ParentEpicKey);
                        parentTask = GetParentDraftTask(taskInfo.IsHomeProject, syncSystem, projectInfo.DraftProject, taskInfo.MainStaging);

                        name = BuildComplexTaskName(taskInfo.MainStaging.IssueName, taskInfo.MainStaging.SystemId,
                            taskInfo.MainStaging.IssueKey);
                    }

                    var taskCreationInformation = new TaskCreationInformation
                    {
                        Id = Guid.NewGuid(),
                        Name = name
                        //Start = staging.RecordDateCreated
                    };
                    if (parentTask != null)
                    {
                        taskCreationInformation.ParentId = parentTask.Id;
                    }
                    projectInfo.DraftProject.Tasks.Add(taskCreationInformation);
                    logger.LogAndSendMessage(null, "AddNewTask", null,
                        winServiceIterationUid,
                        $"AddNewTask projectInfo.DraftProject.Id: {projectInfo.DraftProject.Id}; " +
                        $"taskInfo.Staging.SystemId: {taskInfo.MainStaging?.SystemId}; " +
                        $"taskInfo.Staging.IssueKey: {taskInfo.MainStaging?.IssueKey}; ", false, null);

                    updateCounter++;
                    globalPublishCounter++;
                    CheckPublishCounterGt0(projectInfo, new List<StagingDTO>(),
                        $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}", ref updateCounter);
                }
                CheckPublishCounterNotEq0(projectInfo, new List<StagingDTO>(),
                    $"{message}{updateCounter}", ref updateCounter);
                projectInfo.DraftProject = projectInfo.ProjectOnlineAccessService
                    .GetDraftProject(projectInfo.DraftProject, projectInfo.ProjectGuid);
            }
            return projectInfo.DraftProject;
        }

        private DraftProject UpdateTasks(ProjectInfo projectInfo, DataFromDbViewModel dataFromDbViewModel, ref int globalPublishCounter)
        {
            int updateCounter = 0;
            List<StagingDTO> affectedStagings = new List<StagingDTO>();
            string message = $"ProcessProject UpdateTasks draftProject.Id: {projectInfo.DraftProject.Id}; " +
                             $"draftProject.Name: {projectInfo.DraftProject.Name}; ";

            if (dataFromDbViewModel.UpdateTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                foreach (TaskInfo taskInfo in dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.DraftProject.Id])
                {
                    if (taskInfo.HasChanges)
                    {
                        UpdateTask(projectInfo.DraftProject, taskInfo, dataFromDbViewModel, ref updateCounter,
                            ref globalPublishCounter, affectedStagings);
                        CheckPublishCounterGt0(projectInfo, affectedStagings,
                            $"{message}{updateCounter}", ref updateCounter);
                    }
                }
            }
            CheckPublishCounterNotEq0(projectInfo, affectedStagings,
                $"{message}{updateCounter}", ref updateCounter);

            if (dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                foreach (
                    KeyValuePair<int, List<TaskInfo>> keyValuePair in
                    dataFromDbViewModel.NewTaskChangesTracker[projectInfo.DraftProject.Id])
                {
                    foreach (TaskInfo taskInfo in keyValuePair.Value)
                    {
                        UpdateTask(projectInfo.DraftProject, taskInfo, dataFromDbViewModel, ref updateCounter,
                            ref globalPublishCounter, affectedStagings);
                        CheckPublishCounterGt0(projectInfo, affectedStagings,
                            $"{message}{updateCounter}", ref updateCounter);
                    }
                }
                CheckPublishCounterNotEq0(projectInfo, affectedStagings,
                    $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}",
                    ref updateCounter);
            }
            if (globalPublishCounter > 0)
            {
                projectInfo.DraftProject = projectInfo.ProjectOnlineAccessService
                    .GetDraftProject(projectInfo.DraftProject, projectInfo.ProjectGuid);
            }
            return projectInfo.DraftProject;
        }

        #region Assignments

        private DraftProject SetAssignments(ProjectInfo projectInfo, DataFromDbViewModel dataFromDbViewModel, ref int globalPublishCounter)
        {
            List<Guid> resourcesGuids = new List<Guid>();
            if (dataFromDbViewModel.NewProjectResourcesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                resourcesGuids.AddRange(
                    dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.DraftProject.Id].Select(x => x.ResourceUid));
            }

            if (dataFromDbViewModel.UpdateTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                IEnumerable<Guid> resourcesGuidsTemp = dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.DraftProject.Id]
                    .Where(x => x.AddedAssignments != null && x.AddedAssignments.Count != 0)
                    .SelectMany(x => x.AddedAssignments).Select(x => x.ResourceUid);
                resourcesGuids.AddRange(resourcesGuidsTemp);
            }


            if (dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                IEnumerable<Guid> resourcesGuidsTemp = dataFromDbViewModel.NewTaskChangesTracker[projectInfo.DraftProject.Id]
                    .SelectMany(x => x.Value)
                    .Where(x => x.AddedAssignments != null && x.AddedAssignments.Count != 0)
                    .SelectMany(x => x.AddedAssignments).Select(x => x.ResourceUid);
                resourcesGuids.AddRange(resourcesGuidsTemp);
            }

            resourcesGuids = resourcesGuids.Distinct().ToList();

            List<EnterpriseResource> enterpriseResources = projectInfo.ProjectOnlineAccessService.GetEnterpriseResources(resourcesGuids);
            int updateCounter = 0;
            string message = "";
            if (dataFromDbViewModel.NewProjectResourcesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.DraftProject.Id] = dataFromDbViewModel
                    .NewProjectResourcesTracker[projectInfo.DraftProject.Id].Distinct(new AssignmentInfoAuthorKeyComparer())
                    .ToList();
                message =
                    $"ProcessProject SetAssignments AddEnterpriseResource PublishProject 1 draftProject.Id: {projectInfo.DraftProject.Id}; " +
                    $"draftProject.Name: {projectInfo.DraftProject.Name}; " +
                    $"NewProjectResourcesTracker: {dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.DraftProject.Id].Count}; ";

                foreach (AssignmentInfo assignmentInfo in dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.DraftProject.Id])
                {
                    logger.LogAndSendMessage(null, "ProcessProject AddEnterpriseResource",
                        projectInfo.DraftProject.Id, winServiceIterationUid,
                        $"ProcessProject AddEnterpriseResource draftProject.Id: {projectInfo.DraftProject.Id}; " +
                        $"assignmentInfo.AuthorKey: {assignmentInfo.AuthorKey}; " +
                        $"globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}",
                        false, null);
                    EnterpriseResource enterpriseResource = enterpriseResources
                        .FirstOrDefault(x => x.Id == assignmentInfo.ResourceUid);
                    if (enterpriseResource != null)
                    {
                        projectInfo.DraftProject.ProjectResources.AddEnterpriseResource(enterpriseResource);
                        updateCounter++;
                        globalPublishCounter++;
                        CheckPublishCounterGt0(projectInfo, new List<StagingDTO>(),
                            $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}", ref updateCounter);
                    }
                }
                CheckPublishCounterNotEq0(projectInfo, new List<StagingDTO>(),
                    $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}", ref updateCounter);
            }

            updateCounter = 0;
            List<StagingDTO> affectedStagings = new List<StagingDTO>();
            if (dataFromDbViewModel.UpdateTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                message =
                    $"ProcessProject SetAssignments PublishProject 1 draftProject.Id: {projectInfo.DraftProject.Id}; " +
                    $"draftProject.Name: {projectInfo.DraftProject.Name}; " +
                    $"UpdateTaskChangesTracker: {dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.DraftProject.Id].Count}; ";

                foreach (TaskInfo taskInfo in dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.DraftProject.Id])
                {
                    SetAssignment(projectInfo.DraftProject, taskInfo, ref updateCounter, ref globalPublishCounter,
                        dataFromDbViewModel, affectedStagings);
                    CheckPublishCounterGt0(projectInfo, affectedStagings,
                        $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}",
                        ref updateCounter);
                }
            }

            if (dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectInfo.DraftProject.Id))
            {
                foreach (KeyValuePair<int, List<TaskInfo>> keyValuePair in dataFromDbViewModel.NewTaskChangesTracker[projectInfo.DraftProject.Id])
                {
                    message =
                        $"ProcessProject SetAssignments PublishProject 1 draftProject.Id: {projectInfo.DraftProject.Id}; " +
                        $"draftProject.Name: {projectInfo.DraftProject.Name}; " +
                        $"NewTaskChangesTracker.Key: {keyValuePair.Key}; " +
                        $"NewTaskChangesTracker.Value: {keyValuePair.Value.Count}; ";

                    foreach (TaskInfo taskInfo in keyValuePair.Value)
                    {
                        SetAssignment(projectInfo.DraftProject, taskInfo, ref updateCounter, ref globalPublishCounter,
                            dataFromDbViewModel, affectedStagings);
                        CheckPublishCounterGt0(projectInfo, affectedStagings,
                            $"{message}globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}",
                            ref updateCounter);
                    }
                }
            }
            CheckPublishCounterNotEq0(projectInfo, affectedStagings,
                $"{message}{updateCounter}", ref updateCounter);

            return projectInfo.DraftProject;
        }

        private void SetAssignment(DraftProject draftProject, TaskInfo taskInfo, ref int updateCounter,
            ref int globalPublishCounter, DataFromDbViewModel dataFromDbViewModel, List<StagingDTO> affectedStagings)
        {
            DraftTask draftTask = null;
            if (taskInfo.MainStaging != null)
            {
                draftTask = GetDraftTask(draftProject, taskInfo.MainStaging.SystemId, taskInfo.MainStaging.IssueKey);
            }
            else
            {
                if (taskInfo.TaskUid != Guid.Empty)
                {
                    draftTask = GetDraftTask(draftProject, taskInfo.TaskUid);
                }
            }
            if (draftTask == null)
            {
                logger.LogAndSendMessage(null, "ProcessProject SetAssignment",
                    draftProject.Id, winServiceIterationUid,
                    "ProcessProject SetAssignment Draft Task Not Found " +
                    $"draftProject.Id: {draftProject.Id}; draftProject.Name: {draftProject.Name}; " +
                    $"taskInfo.TaskUid: {taskInfo.TaskUid}; " +
                    $"taskInfo.MainStaging.SystemId: {taskInfo.MainStaging?.SystemId}; " +
                    $"taskInfo.MainStaging.IssueKey: {taskInfo.MainStaging?.IssueKey}; ",
                    true, null);
                return;
            }
            taskInfo.AddedAssignments = taskInfo.AddedAssignments
                .Distinct(new AssignmentInfoAuthorKeyComparer())
                .ToList();
            foreach (AssignmentInfo assignmentInfo in taskInfo.AddedAssignments)
            {
                logger.LogAndSendMessage(null, "ProcessProject SetAssignment",
                    draftProject.Id, winServiceIterationUid,
                    $"ProcessProject SetAssignment draftProject.Id: {draftProject.Id}; draftProject.Name: {draftProject.Name}; " +
                    $"draftTask.Id: {draftTask.Id}; assignmentInfo.IssueKey: {assignmentInfo.IssueKey}; " +
                    $"assignmentInfo.AuthorKey: {assignmentInfo.AuthorKey}; " +
                    $"globalPublishCounter: {globalPublishCounter}; updateCounter: {updateCounter}",
                    false, null);

                AssignmentCreationInformation assignment =
                    new AssignmentCreationInformation
                    {
                        Id = Guid.NewGuid(),
                        TaskId = draftTask.Id,
                        ResourceId = assignmentInfo.ResourceUid,
                        Finish = assignmentInfo.LastUpdateDate
                    };
                draftTask.Assignments.Add(assignment);
                globalPublishCounter++;
                updateCounter++;
            }
            if (taskInfo.AddedAssignments.Count != 0)
            {
                SetStagingsState(taskInfo, dataFromDbViewModel, RecordStateConst.Processed, affectedStagings);
            }
        }

        #endregion

        private void UpdateTask(DraftProject draftProject, TaskInfo taskInfo, DataFromDbViewModel dataFromDbViewModel,
            ref int updateCounter, ref int globalPublishCounter, List<StagingDTO> affectedStagings)
        {
            if (taskInfo.MainStaging == null)
            {
                return;
            }
            DraftTask draftTask = GetDraftTask(draftProject, taskInfo.MainStaging.SystemId, taskInfo.MainStaging.IssueKey);

            logger.LogAndSendMessage(taskInfo.MainStaging, "ProcessProject UpdateTask",
                draftProject.Id, winServiceIterationUid,
                $"ProcessProject UpdateTask Task draftProject.Id: {draftProject.Id}; staging.IssueKey: {taskInfo.MainStaging.IssueKey}; " +
                $"draftTask.Id: {draftTask.Id}; staging.ChangedFields: {taskInfo.MainStaging.ChangedFields}",
                false, null);
            List<VStagingFieldMappingValue> stagingCustomValues = dataFromDbViewModel.CustomValuesAll
                .Where(x => x.SystemId == taskInfo.MainStaging.SystemId
                            && (x.StagingId != null && x.StagingId == taskInfo.MainStaging.StagingId
                                || x.StagingId == null))
                .ToList();

            draftTask.Name = taskInfo.MainStaging.IssueName.RemoveNewLinesTabs();
            if (taskInfo.TaskUid == Guid.Empty)
            {
                draftTask.ConstraintType = ConstraintType.AsSoonAsPossible;
            }
            SetCustomFields(taskInfo.MainStaging, stagingCustomValues, draftTask, draftProject);

            if (taskInfo.AddedAssignments == null || taskInfo.AddedAssignments.Count == 0)
            {
                SetStagingsState(taskInfo, dataFromDbViewModel, RecordStateConst.Processed, affectedStagings);
            }
            updateCounter++;
            globalPublishCounter++;
        }

        private void SetStagingsState(TaskInfo taskInfo, DataFromDbViewModel dataFromDbViewModel, string recordState,
            List<StagingDTO> affectedStagings)
        {
            if (taskInfo == null)
            {
                return;
            }
            if (taskInfo.MainStaging != null)
            {
                if (taskInfo.MainStaging.RecordStateGeneral != RecordStateConst.Done)
                {
                    taskInfo.MainStaging.RecordStateGeneral = recordState;
                }

                List<StagingDTO> oldStagings = dataFromDbViewModel.StagingsAll
                    .Where(x => x.IssueKey == taskInfo.MainStaging.IssueKey &&
                                x.SystemId == taskInfo.MainStaging.SystemId)
                    .ToList();
                foreach (StagingDTO oldStaging in oldStagings)
                {
                    if (taskInfo.MainStaging.RecordStateGeneral != RecordStateConst.Done)
                    {
                        oldStaging.RecordStateGeneral = recordState;
                    }
                }
                affectedStagings.AddRange(oldStagings);
            }
            foreach (StagingDTO affectedStaging in taskInfo.AffectedStagings)
            {
                if (affectedStaging.RecordStateGeneral != RecordStateConst.Done)
                {
                    affectedStaging.RecordStateGeneral = recordState;
                }
            }
            affectedStagings.AddRange(taskInfo.AffectedStagings);
        }

        private void SetCustomFields(StagingDTO staging, List<VStagingFieldMappingValue> stagingCustomValues, DraftTask draftTask, DraftProject draftProject)
        {
            logger.LogAndSendMessage(staging, "ProcessProject SetCustomFields",
                draftProject.Id, winServiceIterationUid,
                $"ProcessStagings SetCustomFields staging.IssueKey: {staging.IssueKey}; " +
                $"draftTask.Id: {draftTask.Id}; stagingCustomValues: {stagingCustomValues.Count}",
                false, null);

            Dictionary<string, string> customFieldsDict = projectOnlineODataService.GetCustomFieldsDict();

            foreach (VStagingFieldMappingValue stagingCustomValue in stagingCustomValues)
            {
                if (!customFieldsDict.ContainsKey(stagingCustomValue.EpmFieldName))
                {
                    continue;
                }
                try
                {
                    //logger.LogAndSendMessage(
                    //    $"SetCustomFields 1 StagingId: {stagingCustomValue.StagingId} " +
                    //    $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                    //    $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                    //    $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                    //    $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                    //    $"Value: {stagingCustomValue.Value}", draftProject.Id);
                    if (!String.IsNullOrEmpty(stagingCustomValue.StagingFieldName))
                    {
                        PropertyInfo pi = typeof(StagingDTO).GetProperty(stagingCustomValue.StagingFieldName);
                        if (pi != null)
                        {
                            logger.LogAndSendMessage(staging, "ProcessProject SetCustomFields",
                                draftProject.Id, winServiceIterationUid,
                                $"ProcessProject SetCustomFields 1 StagingId: {stagingCustomValue.StagingId} " +
                                $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                                $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                                $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                                $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                                $"PropertyInfo.GetValue: {pi.GetValue(staging)}",
                                false, null);
                            object value = pi.GetValue(staging);

                            if (draftTask.FieldValues.ContainsKey(stagingCustomValue.EpmFieldName))
                            {
                                draftTask.FieldValues[customFieldsDict[stagingCustomValue.EpmFieldName]] = value;
                            }
                            draftTask[customFieldsDict[stagingCustomValue.EpmFieldName]] = value;
                        }
                    }
                    if (!String.IsNullOrEmpty(stagingCustomValue.SystemFieldName))
                    {
                        logger.LogAndSendMessage(staging, "ProcessProject SetCustomFields",
                            draftProject.Id, winServiceIterationUid,
                            $"SetCustomFields 2 StagingId: {stagingCustomValue.StagingId} " +
                            $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                            $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                            $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                            $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                            $"stagingCustomValue.Value: {stagingCustomValue.Value}",
                            false, null);
                        string value = stagingCustomValue.Value;
                        if (draftTask.FieldValues.ContainsKey(stagingCustomValue.EpmFieldName))
                        {
                            draftTask.FieldValues[customFieldsDict[stagingCustomValue.EpmFieldName]] = value;
                        }
                        draftTask[customFieldsDict[stagingCustomValue.EpmFieldName]] = value;
                    }
                }
                catch (Exception exception)
                {
                    logger.Warn(exception);
                }
            }
        }

        private string BuildComplexTaskName(string issueName, int systemId, string issueKey)
        {
            issueName = issueName.RemoveNewLinesTabs();
            return $"{issueName}|{systemId}|{issueKey}";
        }

        private void CheckPublishCounterGt0(ProjectInfo projectInfo, List<StagingDTO> affectedStagings,
            string message, ref int updateCounter)
        {
            if (updateCounter >= publishMax - 1)
            {
                UpdateProject(projectInfo, affectedStagings, message);
                logger.LogAndSendMessage(null, "ProcessProject CheckPublishCounterGt0",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"ProcessProject CheckPublishCounterGt0 projectInfo.ProjectGuid: {projectInfo.ProjectGuid}; " +
                    $"affectedStagings: {affectedStagings.Count}; " +
                    $"Processed stagings: {affectedStagings.Count(x => x.RecordStateGeneral == RecordStateConst.Processed)}; " +
                    $"Updated stagings: {affectedStagings.Count(x => x.RecordStateGeneral == RecordStateConst.Updated)}; " +
                    $"updateCounter: {updateCounter}",
                    false, null);
                updateCounter = 0;
                affectedStagings.Clear();
            }
        }

        private void CheckPublishCounterNotEq0(ProjectInfo projectInfo, List<StagingDTO> affectedStagings,
            string message, ref int updateCounter)
        {
            if (updateCounter != 0)
            {
                UpdateProject(projectInfo, affectedStagings, message);
                logger.LogAndSendMessage(null, "ProcessProject CheckPublishCounterNotEq0",
                    projectInfo.ProjectGuid, winServiceIterationUid,
                    $"CheckPublishCounterNotEq0 projectInfo.ProjectGuid: {projectInfo.ProjectGuid}; " +
                    $"affectedStagings: {affectedStagings.Count}; " +
                    $"Processed stagings: {affectedStagings.Count(x => x.RecordStateGeneral == RecordStateConst.Processed)}; " +
                    $"Updated stagings: {affectedStagings.Count(x => x.RecordStateGeneral == RecordStateConst.Updated)}; " +
                    $"publishCounter: {updateCounter}",
                    false, null);
                updateCounter = 0;
                affectedStagings.Clear();
            }
        }

        private List<StagingDTO> PrepareStagingsToIterate(List<StagingDTO> stagings, string message,
            ProjectInfo projectInfo, string recordState)
        {
            logger.LogAndSendMessage(null, $"ProcessProject PrepareStagingsToIterate stagings: {stagings}",
                projectInfo.ProjectGuid, winServiceIterationUid,
                message, false, null);
            List<StagingDTO> stagingsToIterate = null;
            if (stagings != null)
            {
                stagingsToIterate = stagings
                    .Where(x => x.RecordStateGeneral == recordState)
                    .ToList();
            }
            return stagingsToIterate;
        }

        private void PublishProject(ProjectInfo projectInfo, List<StagingDTO> stagings, string message)
        {
            logger.LogAndSendMessage(null, "ProcessProject PublishProject",
                projectInfo.ProjectGuid, winServiceIterationUid,
                message, false, null);

            List<StagingDTO> stagingsToIterate = PrepareStagingsToIterate(stagings, message, projectInfo, RecordStateConst.Updated);
            projectInfo.ProjectOnlineAccessService.PublishProject(projectInfo.DraftProject, stagingsToIterate);
        }

        private void UpdateProject(ProjectInfo projectInfo, List<StagingDTO> stagings, string message)
        {
            logger.LogAndSendMessage(null, "ProcessProject UpdateProject",
                projectInfo.ProjectGuid, winServiceIterationUid,
                message, false, null);

            List<StagingDTO> stagingsToIterate = PrepareStagingsToIterate(stagings, message, projectInfo, RecordStateConst.Processed);
            projectInfo.ProjectOnlineAccessService.UpdateProject(projectInfo.DraftProject, stagingsToIterate);
        }

        protected virtual void GetProjectsToCheckout(DataFromDbViewModel dataFromDbViewModel)
        {
            List<StagingDTO> stagingsResult = (from staging in dataFromDbViewModel.StagingsAll
                                               group staging by new { staging.SystemId, staging.IssueKey } into tempGroup
                                               select new
                                               {
                                                   tempGroup.Key.SystemId,
                                                   tempGroup.Key.IssueKey,
                                                   Staging = tempGroup.OrderByDescending(x => x.RecordDateUpdated).FirstOrDefault()
                                               }).Select(x => x.Staging).ToList();

            foreach (StagingDTO staging in stagingsResult)
            {
                ProjectServerSystemLinkDTO projectServerSystemLink = null;
                try
                {
                    ProjectInfo projectInfo;
                    List<ProjectServerSystemLinkDTO> directEpicLinks = dataFromDbViewModel.ProjectServerSystemLinks
                        .Where(x => x.SystemId == staging.SystemId && !String.IsNullOrEmpty(x.EpicKey)).ToList();
                    List<ProjectServerSystemLinkDTO> homeProjects = dataFromDbViewModel.ProjectServerSystemLinks
                        .Where(x => x.SystemId == staging.SystemId && x.IsHomeProject).ToList();
                    SyncSystemDTO syncSystem = dataFromDbViewModel.SyncSystems
                        .FirstOrDefault(x => x.SystemId == staging.SystemId);
                    if (syncSystem == null)
                    {
                        staging.RecordStateGeneral = RecordStateConst.Done;
                        continue;
                    }
                    if (staging.IsSubTask)
                    {
                        //If it is sub-task
                        #region Subtask

                        //search parent
                        Master masterParent = GetMaster(dataFromDbViewModel.ParentIssues, syncSystem.SystemId,
                            staging.ParentIssueKey);
                        if (masterParent != null)
                        {
                            staging.ParentEpicKey = masterParent.ParentEpicKey;
                            //if exists, check for Epic key
                            if (String.IsNullOrEmpty(masterParent.ParentEpicKey))
                            {
                                //if null - it is a free story
                                //Check for Project Key
                                projectServerSystemLink = homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                                if (projectServerSystemLink != null)
                                {
                                    //There is home project for this story
                                    projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                    CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                }
                                else
                                {
                                    staging.RecordStateGeneral = RecordStateConst.Done;
                                }
                            }
                            else
                            {
                                //Issue has Epic Key
                                //Check for Epic Link
                                projectServerSystemLink = directEpicLinks
                                    .FirstOrDefault(x => x.EpicKey == masterParent.ParentEpicKey);
                                if (projectServerSystemLink != null)
                                {
                                    // There is Epic Link. Check for Project Link
                                    ProjectServerSystemLinkDTO projectLink = homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                                    if (projectLink != null)
                                    {
                                        //There is Project Link
                                        //Compare linking dates
                                        if (projectLink.DateCreated >= projectServerSystemLink.DateCreated)
                                        {
                                            //Project link was created later than Direct Epic link. All info goes into Epic
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                            CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                        }
                                        else
                                        {
                                            //Direct Epic Link was created later than Project link
                                            //It is possible, that some hours should go into Project, so we will create this task in Project or add assignment
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectLink, staging);
                                            CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                            //and some go to Epic
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                            CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                        }
                                    }
                                    else
                                    {
                                        //no Project link
                                        projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                        CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                    }
                                }
                                else
                                {
                                    //no direct links. Check for Project
                                    projectServerSystemLink = homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                                    if (projectServerSystemLink != null)
                                    {
                                        //There is home project for this Project Link
                                        projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                        CheckChangesSubtask(dataFromDbViewModel, projectInfo.DraftProject, staging);
                                    }
                                    else
                                    {
                                        staging.RecordStateGeneral = RecordStateConst.Done;
                                    }
                                }
                            }
                        }
                        else
                        {
                            staging.RecordStateGeneral = RecordStateConst.Done;
                        }

                        #endregion
                    }
                    else
                    {
                        //it is not sub task. Generaly, the same actions
                        if (staging.IssueTypeName != "Epic")
                        {
                            if (String.IsNullOrEmpty(staging.ParentEpicKey))
                            {
                                //no Parent Epic Key. Check fo Project Link
                                projectServerSystemLink = SearchParentLink(staging, dataFromDbViewModel, homeProjects, directEpicLinks);
                                if (projectServerSystemLink != null)
                                {
                                    projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                    CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                                }
                                else
                                {
                                    staging.RecordStateGeneral = RecordStateConst.Done;
                                }
                            }
                            else
                            {
                                //Check for Direct Link
                                projectServerSystemLink = directEpicLinks
                                    .FirstOrDefault(x => x.EpicKey == staging.ParentEpicKey);
                                if (projectServerSystemLink != null)
                                {
                                    // There is Epic Link. Check for Project Link
                                    ProjectServerSystemLinkDTO projectLink = homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                                    if (projectLink != null)
                                    {
                                        //There is Project Link
                                        //Compare linking dates
                                        if (projectLink.DateCreated >= projectServerSystemLink.DateCreated)
                                        {
                                            //Project link was created later than Direct Epic link. All info goes into Epic
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                            CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                                        }
                                        else
                                        {
                                            //Direct Epic Link was created later than Project link
                                            //It is possible, that some hours should go into Project
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectLink, staging);
                                            CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectLink);
                                            //and some go to Epic
                                            projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                            CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                                        }
                                    }
                                    else
                                    {
                                        //no Project link
                                        projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                        CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                                    }
                                }
                                else
                                {
                                    //no direct links. Check for Project
                                    projectServerSystemLink = homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                                    if (projectServerSystemLink != null)
                                    {
                                        //There is home project for this Project Link
                                        projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                        CheckChangesIssue(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                                    }
                                    else
                                    {
                                        staging.RecordStateGeneral = RecordStateConst.Done;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //This is Epic. Check for Direct Link
                            projectServerSystemLink = directEpicLinks
                                .FirstOrDefault(x => x.EpicKey == staging.IssueKey);
                            if (projectServerSystemLink != null)
                            {
                                projectInfo = GetProjectInfo(dataFromDbViewModel, projectServerSystemLink, staging);
                                CheckChangesEpic(dataFromDbViewModel, projectInfo.DraftProject, staging, projectServerSystemLink);
                            }
                            else
                            {
                                staging.RecordStateGeneral = RecordStateConst.Done;
                            }
                            //No direct link. Ignore this EPIC
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogAndSendMessage(staging, "ProcessProject GetProjectsToCheckout",
                        projectServerSystemLink?.ProjectUid, winServiceIterationUid,
                        $"GetProjectsToCheckout staging.IssueKey: {staging.IssueKey}; " +
                        $"staging.SystemId: {staging.IssueKey}; " +
                        $"projectServerSystemLink.ProjectUid: {projectServerSystemLink?.ProjectUid}; " +
                        $"projectServerSystemLink.ProjectServerSystemLinkId: {projectServerSystemLink?.ProjectServerSystemLinkId}",
                        false, exception);
                }
            }
            dataFromDbViewModel.NewTaskChangesTracker = dataFromDbViewModel.NewTaskChangesTracker
                .Where(x => x.Value.Count != 0).ToDictionary(x => x.Key, x => x.Value);
            dataFromDbViewModel.NewProjectResourcesTracker = dataFromDbViewModel.NewProjectResourcesTracker
                .Where(x => x.Value.Count != 0).ToDictionary(x => x.Key, x => x.Value);
            dataFromDbViewModel.UpdateTaskChangesTracker = dataFromDbViewModel.UpdateTaskChangesTracker
                .Where(x => x.Value.Count != 0).ToDictionary(x => x.Key, x => x.Value);

            List<Guid> resultProjectsToCheckout = dataFromDbViewModel.NewTaskChangesTracker.Keys
                .Union(dataFromDbViewModel.NewProjectResourcesTracker.Keys)
                .Union(dataFromDbViewModel.UpdateTaskChangesTracker.Keys).Distinct().ToList();

            dataFromDbViewModel.ProjectInfos = dataFromDbViewModel.ProjectInfos
                .Where(x => resultProjectsToCheckout.Contains(x.ProjectGuid)).ToList();
        }

        ProjectServerSystemLinkDTO SearchParentLink(StagingDTO staging, DataFromDbViewModel dataFromDbViewModel,
            List<ProjectServerSystemLinkDTO> homeProjects, List<ProjectServerSystemLinkDTO> directEpicLinks)
        {
            ProjectServerSystemLinkDTO projectServerSystemLink;
            if (staging.IssueTypeName != "Epic")
            {
                if (String.IsNullOrEmpty(staging.ParentEpicKey))
                {
                    if (String.IsNullOrEmpty(staging.ParentIssueKey))
                    {
                        //no Parent Epic Key AND no Parent Issue Key. Check fo Project Link
                        return homeProjects.FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
                    }
                    //Check for Parent Issue
                    var parentMaster = dataFromDbViewModel.ParentIssues
                        .FirstOrDefault(x => x.IssueKey == staging.ParentIssueKey);
                    if (parentMaster != null)
                    {
                        StagingDTO virtualStaging = new StagingDTO
                        {
                            IssueKey = parentMaster.IssueKey,
                            ProjectKey = parentMaster.ProjectKey,
                            IssueTypeName = parentMaster.IssueTypeName,
                            ParentIssueKey = parentMaster.ParentIssueKey,
                            ParentEpicKey = parentMaster.ParentEpicKey,
                        };
                        return SearchParentLink(virtualStaging, dataFromDbViewModel, homeProjects, directEpicLinks);
                    }
                    var parentStaging = dataFromDbViewModel.StagingsAll
                        .FirstOrDefault(x => x.IssueKey == staging.ParentIssueKey);
                    if (parentStaging != null)
                    {
                        return SearchParentLink(parentStaging, dataFromDbViewModel, homeProjects, directEpicLinks);
                    }
                    return null;
                }
                //Check for Direct Link
                projectServerSystemLink = directEpicLinks
                    .FirstOrDefault(x => x.EpicKey == staging.ParentEpicKey);
                if (projectServerSystemLink != null)
                {
                    return projectServerSystemLink;
                }
                return homeProjects
                    .FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
            }
            //This is Epic. Check for Direct Link
            projectServerSystemLink = directEpicLinks
                .FirstOrDefault(x => x.EpicKey == staging.IssueKey);
            if (projectServerSystemLink != null)
            {
                return projectServerSystemLink;
            }
            return homeProjects
                .FirstOrDefault(x => x.ProjectKey == staging.ProjectKey);
        }

        #region Epic

        protected virtual void CheckChangesEpic(DataFromDbViewModel dataFromDbViewModel, DraftProject draftProject,
            StagingDTO staging, ProjectServerSystemLinkDTO projectServerSystemLink)
        {
            List<AssignmentInfo> assignments = dataFromDbViewModel.Assignments
                .Where(x => x.SystemId == staging.SystemId && x.IssueId == staging.IssueId)
                .ToList();
            CheckProjectResources(dataFromDbViewModel, assignments, draftProject);
            List<VStagingFieldMappingValue> stagingCustomValues = dataFromDbViewModel.CustomValuesAll
                .Where(x => x.StagingId == staging.StagingId)
                .ToList();

            DraftTask draftTask = GetDraftTask(draftProject, staging.SystemId, staging.IssueKey);
            if (draftTask == null)
            {
                FillNewTaskChangesTrackerEpic(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                    assignments, projectServerSystemLink);
            }
            else
            {
                FillUpdateTaskChangesTrackerEpic(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                    draftTask, assignments);
            }
        }

        protected virtual void FillNewTaskChangesTrackerEpic(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            List<VStagingFieldMappingValue> stagingCustomValues, DraftProject draftProject,
            List<AssignmentInfo> assignments, ProjectServerSystemLinkDTO projectServerSystemLink)
        {
            FillNewTaskChangesTrackerIssue(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                assignments, projectServerSystemLink);
        }

        protected virtual void FillUpdateTaskChangesTrackerEpic(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            List<VStagingFieldMappingValue> stagingCustomValues, DraftProject draftProject,
            DraftTask draftTask, List<AssignmentInfo> assignments)
        {
            FillUpdateTaskChangesTrackerIssue(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                draftTask, assignments);
        }

        #endregion

        #region Subtask

        protected virtual void CheckChangesSubtask(DataFromDbViewModel dataFromDbViewModel, DraftProject draftProject, StagingDTO staging)
        {
            List<AssignmentInfo> assignments = dataFromDbViewModel.Assignments
                .Where(x => x.SystemId == staging.SystemId && x.IssueId == staging.IssueId).ToList();
            CheckProjectResources(dataFromDbViewModel, assignments, draftProject);
            DraftTask draftTask = GetDraftTask(draftProject, staging.SystemId, staging.ParentIssueKey);

            if (draftTask == null)
            {
                FillNewTaskChangesTrackerSubtask(dataFromDbViewModel, staging, draftProject, assignments);
            }
            else
            {
                FillUpdateTaskChangesTrackerSubtask(dataFromDbViewModel, staging, draftProject, draftTask, assignments);
            }
        }

        protected virtual void FillNewTaskChangesTrackerSubtask(DataFromDbViewModel dataFromDbViewModel,
            StagingDTO staging, DraftProject draftProject, List<AssignmentInfo> assignments)
        {
            bool isFound = false;
            foreach (KeyValuePair<int, List<TaskInfo>> keyValuePair in dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id])
            {
                TaskInfo taskInfo = keyValuePair.Value
                    .FirstOrDefault(x => x.MainStaging != null && x.MainStaging.SystemId == staging.SystemId
                                         && x.MainStaging.IssueKey == staging.ParentIssueKey);
                if (taskInfo != null)
                {
                    taskInfo.AddedAssignments.AddRange(assignments);
                    taskInfo.AddedAssignments = taskInfo.AddedAssignments
                        .Distinct(new AssignmentInfoComparer()).ToList();
                    taskInfo.AffectedStagings.Add(staging);
                    isFound = true;
                    break;
                }
            }
            staging.RecordStateGeneral = !isFound ? RecordStateConst.Done : RecordStateConst.Pending;
        }

        protected virtual void FillUpdateTaskChangesTrackerSubtask(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            DraftProject draftProject, DraftTask draftTask, List<AssignmentInfo> assignments)
        {
            if (dataFromDbViewModel.UpdateTaskChangesTracker[draftProject.Id] == null)
            {
                dataFromDbViewModel.UpdateTaskChangesTracker[draftProject.Id] = new List<TaskInfo>();
            }

            TaskInfo taskInfo = GetTaskInfo(dataFromDbViewModel, draftProject,
                draftTask, out var isExist);

            CheckTaskAssignments(taskInfo, assignments, draftTask, staging);
            if (taskInfo.MainStaging != null || taskInfo.AddedAssignments.Count != 0)
            {
                if (!isExist)
                {
                    AddUpdateTaskChangesTrackerRecord(dataFromDbViewModel, taskInfo, draftProject.Id);
                }
                staging.RecordStateGeneral = RecordStateConst.Pending;
            }
            else
            {
                staging.RecordStateGeneral = RecordStateConst.Done;
            }
        }

        #endregion

        #region Issue

        protected virtual void CheckChangesIssue(DataFromDbViewModel dataFromDbViewModel, DraftProject draftProject,
            StagingDTO staging, ProjectServerSystemLinkDTO projectServerSystemLink)
        {
            List<AssignmentInfo> assignments = dataFromDbViewModel.Assignments
                .Where(x => x.SystemId == staging.SystemId && x.IssueId == staging.IssueId)
                .ToList();
            CheckProjectResources(dataFromDbViewModel, assignments, draftProject);
            List<VStagingFieldMappingValue> stagingCustomValues = dataFromDbViewModel.CustomValuesAll
                .Where(x => x.StagingId == staging.StagingId)
                .ToList();

            DraftTask draftTask = GetDraftTask(draftProject, staging.SystemId, staging.IssueKey);
            if (draftTask == null)
            {
                FillNewTaskChangesTrackerIssue(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                    assignments, projectServerSystemLink);
            }
            else
            {
                FillUpdateTaskChangesTrackerIssue(dataFromDbViewModel, staging, stagingCustomValues, draftProject,
                    draftTask, assignments);
            }
        }

        protected virtual void FillNewTaskChangesTrackerIssue(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            List<VStagingFieldMappingValue> stagingCustomValues, DraftProject draftProject,
            List<AssignmentInfo> assignments, ProjectServerSystemLinkDTO projectServerSystemLink)
        {
            SyncSystemDTO syncSystem = dataFromDbViewModel.SyncSystems
                .FirstOrDefault(x => x.SystemId == staging.SystemId);
            if (syncSystem == null)
            {
                return;
            }
            DraftTask parentTask = GetParentDraftTask(projectServerSystemLink.IsHomeProject, syncSystem, draftProject,
                staging);
            if (parentTask != null)
            {
                AddNewTaskChangesTrackerRecord(dataFromDbViewModel, staging, stagingCustomValues,
                    draftProject.Id, 0, assignments, projectServerSystemLink);
            }
            else
            {
                bool isFound = false;
                foreach (KeyValuePair<int, List<TaskInfo>> keyValuePair in dataFromDbViewModel
                    .NewTaskChangesTracker[draftProject.Id])
                {

                    TaskInfo parentTaskInfo;
                    if (projectServerSystemLink.IsHomeProject || staging.IssueTypeName == "Epic")
                    {
                        parentTaskInfo = keyValuePair.Value
                            .FirstOrDefault(x => x.DefaultParentTaskName == syncSystem.DefaultParentTaskName);
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(staging.ParentEpicKey))
                        {
                            parentTaskInfo = keyValuePair.Value
                                .FirstOrDefault(x => x.MainStaging != null
                                                     && x.MainStaging.IssueKey == staging.ParentEpicKey);
                        }
                        else
                        {
                            parentTaskInfo = !String.IsNullOrEmpty(staging.ParentIssueKey)
                                ? keyValuePair.Value
                                    .FirstOrDefault(x => x.MainStaging != null
                                                         && x.MainStaging.IssueKey == staging.ParentIssueKey)
                                : keyValuePair.Value
                                    .FirstOrDefault(x => x.DefaultParentTaskName == syncSystem.DefaultParentTaskName);
                        }
                    }

                    if (parentTaskInfo != null)
                    {
                        AddNewTaskChangesTrackerRecord(dataFromDbViewModel, staging, stagingCustomValues,
                            draftProject.Id, keyValuePair.Key + 1, assignments, projectServerSystemLink);
                        isFound = true;
                        break;
                    }
                }
                if (!isFound)
                {
                    staging.RecordStateGeneral = RecordStateConst.Done;
                }
            }
        }

        protected virtual void FillUpdateTaskChangesTrackerIssue(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            List<VStagingFieldMappingValue> stagingCustomValues, DraftProject draftProject,
            DraftTask draftTask, List<AssignmentInfo> assignments)
        {
            TaskInfo taskInfo = GetTaskInfo(dataFromDbViewModel, draftProject, draftTask, out var isExist);

            if (CheckFieldsChanges(staging, stagingCustomValues, draftTask, draftProject.Id))
            {
                taskInfo.HasChanges = true;
                taskInfo.StagingCustomValues = stagingCustomValues;
            }
            CheckTaskAssignments(taskInfo, assignments, draftTask, staging);
            taskInfo.MainStaging = staging;
            taskInfo.AffectedStagings.Add(staging);
            if (taskInfo.HasChanges || taskInfo.AddedAssignments.Count != 0)
            {
                if (!isExist)
                {
                    AddUpdateTaskChangesTrackerRecord(dataFromDbViewModel, taskInfo, draftProject.Id);
                }
            }
            else
            {
                staging.RecordStateGeneral = RecordStateConst.Done;
            }
        }

        protected virtual bool CheckFieldsChanges(StagingDTO staging, List<VStagingFieldMappingValue> stagingCustomValues,
            DraftTask draftTask, Guid projectUid)
        {
            logger.LogAndSendMessage(staging, "CheckCustomFieldsChanges",
                projectUid, winServiceIterationUid,
                $"CheckCustomFieldsChanges staging.IssueKey: {staging.IssueKey}; " +
                $"draftTask.Id: {draftTask.Id}; stagingCustomValues: {stagingCustomValues.Count}",
                false, null);
            if (staging.IssueName != draftTask.Name)
            {
                return true;
            }
            Dictionary<string, string> customFieldsDict = projectOnlineODataService.GetCustomFieldsDict();

            foreach (VStagingFieldMappingValue stagingCustomValue in stagingCustomValues)
            {
                if (!customFieldsDict.ContainsKey(stagingCustomValue.EpmFieldName)
                    || stagingCustomValue.EpmFieldName == ProjectServerConstants.SystemId)
                {
                    continue;
                }
                try
                {
                    //logger.LogAndSendMessage(
                    //    $"SetCustomFields 1 StagingId: {stagingCustomValue.StagingId} " +
                    //    $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                    //    $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                    //    $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                    //    $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                    //    $"Value: {stagingCustomValue.Value}");
                    if (!String.IsNullOrEmpty(stagingCustomValue.StagingFieldName))
                    {
                        PropertyInfo pi = typeof(StagingDTO).GetProperty(stagingCustomValue.StagingFieldName);
                        if (pi != null)
                        {
                            //logger.LogAndSendMessage(
                            //    $"SetCustomFields 1 StagingId: {stagingCustomValue.StagingId} " +
                            //    $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                            //    $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                            //    $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                            //    $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                            //    $"PropertyInfo.GetValue: {pi.GetValue(staging)}");

                            var stagingValue = (string)pi.GetValue(staging);

                            if (draftTask.FieldValues.ContainsKey(
                                customFieldsDict[stagingCustomValue.EpmFieldName]))
                            {
                                var taskValue =
                                    (string)draftTask.FieldValues[
                                        customFieldsDict[stagingCustomValue.EpmFieldName]];
                                if (stagingValue != taskValue)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(stagingValue))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    if (!String.IsNullOrEmpty(stagingCustomValue.SystemFieldName))
                    {
                        //logger.LogAndSendMessage(
                        //    $"SetCustomFields 2 StagingId: {stagingCustomValue.StagingId} " +
                        //    $"SyncSystemFieldMappingId: {stagingCustomValue.SyncSystemFieldMappingId} " +
                        //    $"EpmFieldName: {stagingCustomValue.EpmFieldName} " +
                        //    $"SystemFieldName: {stagingCustomValue.SystemFieldName} " +
                        //    $"StagingFieldName: {stagingCustomValue.StagingFieldName} " +
                        //    $"stagingCustomValue.Value: {stagingCustomValue.Value}");

                        if (draftTask.FieldValues.ContainsKey(customFieldsDict[stagingCustomValue.EpmFieldName]))
                        {
                            var taskValue =
                                (string)draftTask.FieldValues[customFieldsDict[stagingCustomValue.EpmFieldName]];
                            string stagingValue = stagingCustomValue.Value;
                            if (stagingValue != taskValue)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(stagingCustomValue.Value))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.Warn(exception);
                }
            }
            return false;
        }

        #endregion

        private Master GetMaster(List<Master> masters, int systemId, string issueKey)
        {
            return masters.FirstOrDefault(x => x.SystemId == systemId && x.IssueKey == issueKey);
        }

        private ProjectInfo GetProjectInfo(DataFromDbViewModel dataFromDbViewModel, ProjectServerSystemLinkDTO projectLink, StagingDTO staging)
        {
            //logger.LogAndSendMessage(staging, "GetProjectInfo.GetPublishedProject START",
            //    projectLink.ProjectUid, winServiceIterationUid,
            //    $"GetProjectInfo.GetPublishedProject START ProjectUid: {projectLink.ProjectUid}",
            //    false, null);

            ProjectInfo projectInfo = dataFromDbViewModel.ProjectInfos
                .FirstOrDefault(x => x.ProjectGuid == projectLink.ProjectUid);
            if (projectInfo == null)
            {
                projectInfo = new ProjectInfo
                {
                    ProjectGuid = projectLink.ProjectUid,
                    ProjectOnlineAccessService =
                        new ProjectOnlineAccessService(projectOnlineODataService.ProjectOnlineUrl,
                            projectOnlineODataService.ProjectOnlineUserName,
                            projectOnlineODataService.ProjectOnlinePassword,
                            projectOnlineODataService.IsOnline, winServiceIterationUid)
                };
                projectInfo.PublishedProject = projectInfo.ProjectOnlineAccessService.GetPublishedProject(projectLink.ProjectUid);
                if (projectInfo.PublishedProject == null)
                {
                    throw new Exception($"PublishedProject {projectLink.ProjectUid} is NULL");
                }

                projectInfo.DraftProject = projectInfo.ProjectOnlineAccessService.GetDraftProject(projectInfo.PublishedProject);
                if (projectInfo.PublishedProject == null)
                {
                    throw new Exception($"DraftProject {projectLink.ProjectUid} is NULL");
                }

                dataFromDbViewModel.ProjectInfos.Add(projectInfo);
            }
            if (!dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectInfo.ProjectGuid)
                || dataFromDbViewModel.NewTaskChangesTracker[projectInfo.ProjectGuid] == null)
            {
                dataFromDbViewModel.NewTaskChangesTracker[projectInfo.ProjectGuid] = new Dictionary<int, List<TaskInfo>>();
            }
            if (!dataFromDbViewModel.UpdateTaskChangesTracker.ContainsKey(projectInfo.ProjectGuid)
                || dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.ProjectGuid] == null)
            {
                dataFromDbViewModel.UpdateTaskChangesTracker[projectInfo.ProjectGuid] = new List<TaskInfo>();
            }
            if (!dataFromDbViewModel.NewProjectResourcesTracker.ContainsKey(projectInfo.ProjectGuid)
                || dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.ProjectGuid] == null)
            {
                dataFromDbViewModel.NewProjectResourcesTracker[projectInfo.ProjectGuid] = new List<AssignmentInfo>();
                CheckSystemResource(projectInfo.DraftProject, dataFromDbViewModel);
            }
            CheckDefaultSystemsTask(dataFromDbViewModel, projectInfo.DraftProject, staging);
            //logger.LogAndSendMessage(staging, "GetProjectInfo.GetPublishedProject END",
            //    projectLink.ProjectUid, winServiceIterationUid,
            //    $"GetProjectInfo.GetPublishedProject START ProjectUid: {projectLink.ProjectUid}",
            //    false, null);
            return projectInfo;
        }

        private void CheckSystemResource(DraftProject draftProject, DataFromDbViewModel dataFromDbViewModel)
        {
            try
            {
                ODataResource currentODataResource = projectOnlineODataService.GetCurrentODataResource();
                try
                {
                    DraftProjectResource draftProjectResource = draftProject.ProjectResources
                        .FirstOrDefault(x => x.Id == currentODataResource.ResourceId);
                    if (draftProjectResource == null)
                    {
                        dataFromDbViewModel.NewProjectResourcesTracker[draftProject.Id].Add(new AssignmentInfo()
                        {
                            ResourceUid = currentODataResource.ResourceId
                        });
                    }
                }
                catch (PropertyOrFieldNotInitializedException exception)
                {
                    logger.Error(exception);
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }

        private void CheckDefaultSystemsTask(DataFromDbViewModel dataFromDbViewModel, DraftProject draftProject, StagingDTO staging)
        {
            SyncSystemDTO syncSystem = dataFromDbViewModel.SyncSystems.FirstOrDefault(x => x.SystemId == staging.SystemId);
            if (syncSystem != null)
            {
                DraftTask draftTask = GetDraftTask(draftProject, syncSystem.DefaultParentTaskName);
                if (draftTask == null)
                {
                    if (!dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(draftProject.Id))
                    {
                        dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id] =
                            new Dictionary<int, List<TaskInfo>>();
                    }
                    if (!dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id].ContainsKey(0)
                        || dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id][0] == null)
                    {
                        dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id][0] = new List<TaskInfo>();
                    }

                    TaskInfo taskInfo = dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id][0]
                        .FirstOrDefault(x => x.DefaultParentTaskName == syncSystem.DefaultParentTaskName);
                    if (taskInfo == null)
                    {
                        taskInfo = new TaskInfo
                        {
                            ProjectUid = draftProject.Id,
                            DefaultParentTaskName = syncSystem.DefaultParentTaskName
                        };
                        dataFromDbViewModel.NewTaskChangesTracker[draftProject.Id][0].Add(taskInfo);
                    }
                }
            }
        }

        private void CheckProjectResources(DataFromDbViewModel dataFromDbViewModel, List<AssignmentInfo> assignments, DraftProject draftProject)
        {
            foreach (AssignmentInfo assignmentInfo in assignments)
            {
                if (assignmentInfo.ODataResource == null)
                {
                    assignmentInfo.ODataResource = projectOnlineODataService
                        .GetODataResource(assignmentInfo.AuthorKey);
                }
                if (assignmentInfo.ODataResource != null)
                {
                    DraftProjectResource draftProjectResource = draftProject.ProjectResources
                        .FirstOrDefault(x => x.Id == assignmentInfo.ODataResource.ResourceId);
                    if (draftProjectResource == null)
                    {
                        AssignmentInfo assignmentInfoExisitng = dataFromDbViewModel
                            .NewProjectResourcesTracker[draftProject.Id]
                            .FirstOrDefault(x => x.SystemId == assignmentInfo.SystemId
                                                 && x.IssueId == assignmentInfo.IssueId
                                                 && x.AuthorKey == assignmentInfo.AuthorKey);
                        if (assignmentInfoExisitng == null)
                        {
                            dataFromDbViewModel.NewProjectResourcesTracker[draftProject.Id].Add(assignmentInfo);
                        }
                    }
                }
            }
        }

        private void CheckTaskAssignments(TaskInfo taskInfo, List<AssignmentInfo> assignments, DraftTask draftTask, StagingDTO staging)
        {
            foreach (AssignmentInfo assignmentInfo in assignments)
            {
                if (assignmentInfo.ODataResource == null)
                {
                    assignmentInfo.ODataResource = projectOnlineODataService
                        .GetODataResource(assignmentInfo.AuthorKey);
                }
                if (assignmentInfo.ODataResource != null)
                {
                    DraftAssignment draftAssignment = draftTask
                        .Assignments.FirstOrDefault(x => x.Resource.Id == assignmentInfo.ODataResource.ResourceId);
                    if (draftAssignment == null)
                    {
                        taskInfo.AddedAssignments.Add(assignmentInfo);
                        taskInfo.AffectedStagings.Add(staging);
                    }
                }
            }
        }

        private TaskInfo GetTaskInfo(DataFromDbViewModel dataFromDbViewModel, DraftProject draftProject,
            DraftTask draftTask, out bool isExist)
        {
            isExist = false;
            TaskInfo taskInfo = dataFromDbViewModel.UpdateTaskChangesTracker[draftProject.Id]
                .FirstOrDefault(x => x.TaskUid == draftTask.Id);
            if (taskInfo != null)
            {
                isExist = true;
            }
            else
            {
                taskInfo = new TaskInfo
                {
                    ProjectUid = draftProject.Id,
                    TaskUid = draftTask.Id
                };
            }
            return taskInfo;
        }

        private void AddNewTaskChangesTrackerRecord(DataFromDbViewModel dataFromDbViewModel, StagingDTO staging,
            List<VStagingFieldMappingValue> stagingCustomValues, Guid projectUid, int level,
            List<AssignmentInfo> assignments, ProjectServerSystemLinkDTO projectServerSystemLink)
        {
            if (!dataFromDbViewModel.NewTaskChangesTracker.ContainsKey(projectUid))
            {
                dataFromDbViewModel.NewTaskChangesTracker[projectUid] = new Dictionary<int, List<TaskInfo>>();
            }
            if (!dataFromDbViewModel.NewTaskChangesTracker[projectUid].ContainsKey(level)
                || dataFromDbViewModel.NewTaskChangesTracker[projectUid][level] == null)
            {
                dataFromDbViewModel.NewTaskChangesTracker[projectUid][level] = new List<TaskInfo>();
            }
            TaskInfo taskInfo = new TaskInfo
            {
                MainStaging = staging,
                StagingCustomValues = stagingCustomValues,
                ProjectUid = projectUid,
                AddedAssignments = assignments,
                IsHomeProject = projectServerSystemLink.IsHomeProject
            };
            taskInfo.AffectedStagings.Add(staging);
            dataFromDbViewModel.NewTaskChangesTracker[projectUid][level].Add(taskInfo);
        }

        private void AddUpdateTaskChangesTrackerRecord(DataFromDbViewModel dataFromDbViewModel,
            TaskInfo taskInfo, Guid projectUid)
        {
            if (dataFromDbViewModel.UpdateTaskChangesTracker[projectUid] == null)
            {
                dataFromDbViewModel.UpdateTaskChangesTracker[projectUid] = new List<TaskInfo>();
            }
            dataFromDbViewModel.UpdateTaskChangesTracker[projectUid].Add(taskInfo);
        }

        private DraftTask GetParentDraftTask(bool isHomeProject,
            SyncSystemDTO syncSystem, DraftProject draftProject, StagingDTO staging)
        {
            DraftTask parentTask;
            if (isHomeProject || staging.IssueTypeName == "Epic")
            {
                parentTask = GetDraftTask(draftProject, syncSystem.DefaultParentTaskName);
            }
            else
            {
                if (!String.IsNullOrEmpty(staging.ParentEpicKey))
                {
                    parentTask = GetDraftTask(draftProject, staging.SystemId, staging.ParentEpicKey);
                }
                else
                {
                    parentTask = !String.IsNullOrEmpty(staging.ParentIssueKey)
                        ? GetDraftTask(draftProject, staging.SystemId, staging.ParentIssueKey)
                        : GetDraftTask(draftProject, syncSystem.DefaultParentTaskName);
                }
            }
            return parentTask;
        }

        private DraftTask GetDraftTask(DraftProject draftProject, string taskName)
        {
            return draftProject.Tasks.FirstOrDefault(x => x.Name == taskName);
        }

        private DraftTask GetDraftTask(DraftProject draftProject, Guid taskId)
        {
            return draftProject.Tasks.FirstOrDefault(x => x.Id == taskId);
        }

        private DraftTask GetDraftTask(DraftProject draftProject, int syncSystemId, string issueKey)
        {
            Dictionary<string, string> customFieldsDict = projectOnlineODataService.GetCustomFieldsDict();
            if (!customFieldsDict.ContainsKey(ProjectServerConstants.SystemId) ||
                !customFieldsDict.ContainsKey(ProjectServerConstants.IssueKey))
            {
                return null;
            }
            string systemIdFieldName = customFieldsDict[ProjectServerConstants.SystemId];
            string issueKeyFieldName = customFieldsDict[ProjectServerConstants.IssueKey];


            return draftProject.Tasks.FirstOrDefault(
                draftTask =>
                    CompareTaskWithSystemIdIssueKey(systemIdFieldName, issueKeyFieldName, draftTask.FieldValues,
                        syncSystemId, issueKey, draftTask.ServerObjectIsNull, draftTask.Name));
        }

        private bool CompareTaskWithSystemIdIssueKey(string systemIdFieldName, string issueKeyFieldName,
            Dictionary<string, object> fieldValues, int syncSystemId, string issueKey,
            bool? serverObjectIsNull, string name)
        {
            if (fieldValues.ContainsKey(systemIdFieldName)
                && fieldValues[systemIdFieldName] != null
                && fieldValues.ContainsKey(issueKeyFieldName)
                && fieldValues[issueKeyFieldName] != null)
            {
                decimal systemId = 0;
                Type type = fieldValues[systemIdFieldName].GetType();
                if (type == typeof(decimal))
                {
                    systemId = (decimal)fieldValues[systemIdFieldName];
                }
                if (type == typeof(int))
                {
                    systemId = (int)fieldValues[systemIdFieldName];
                }

                string currentIssueKey = (string)fieldValues[issueKeyFieldName];

                if (systemId != 0 && !String.IsNullOrEmpty(currentIssueKey)
                    && systemId == syncSystemId && issueKey == currentIssueKey)
                {
                    return true;
                }
            }
            else
            {
                if (serverObjectIsNull.HasValue && !serverObjectIsNull.Value
                    && !String.IsNullOrEmpty(name) && name.Contains($"{syncSystemId}|{issueKey}"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}