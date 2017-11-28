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
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataModel.OData;
using Z.EntityFramework.Plus;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class ProjectServerSystemLinkBusinessService : BaseBusinessService
    {
        private readonly ProjectOnlineODataService projectOnlineODataService;

        public ProjectServerSystemLinkBusinessService(ProjectOnlineODataService projectOnlineODataService,
            UnitOfWork unitOfWork) : base(unitOfWork)
        {
            this.projectOnlineODataService = projectOnlineODataService;
        }

        public IQueryable<ProjectServerSystemLinkDTO> GetQuery(Guid? projectUid = null, int? systemId = null,
            string projectId = null, string epicKey = null)
        {
            IQueryable<ProjectServerSystemLink> query = UnitOfWork.ProjectServerSystemLinkRepository.GetQuery();
            if (projectUid.HasValue)
            {
                query = query.Where(x => x.ProjectUid == projectUid);
            }
            if (systemId.HasValue)
            {
                query = query.Where(x => x.SystemId == systemId.Value);
            }
            if (!String.IsNullOrEmpty(projectId))
            {
                query = query.Where(x => x.ProjectId == projectId);
            }
            if (!String.IsNullOrEmpty(epicKey))
            {
                query = query.Where(x => x.EpicKey == epicKey);
            }
            IQueryable<ProjectServerSystemLinkDTO> result = query.ProjectTo<ProjectServerSystemLinkDTO>();
            return result;
        }

        public IQueryable<ProjectServerSystemLinkDTO> GetQuery(
            Expression<Func<ProjectServerSystemLink, bool>> predicate)
        {
            IQueryable<ProjectServerSystemLink> query = UnitOfWork.ProjectServerSystemLinkRepository
                .GetQuery(predicate);
            IQueryable<ProjectServerSystemLinkDTO> result = query.ProjectTo<ProjectServerSystemLinkDTO>();
            return result;
        }

        public async Task<List<ProjectServerSystemLinkDTO>> GetListAsync(
            Expression<Func<ProjectServerSystemLink, bool>> predicate)
        {
            IQueryable<ProjectServerSystemLinkDTO> query = GetQuery(predicate);
            List<ProjectServerSystemLinkDTO> result = await query.ToListAsync();
            return result;
        }

        public async Task<List<ProjectServerSystemLinkDTO>> GetListAsync(Guid? projectUid = null, int? systemId = null,
            string projectId = null, string epicKey = null)
        {
            IQueryable<ProjectServerSystemLinkDTO> query = GetQuery(projectUid, systemId, projectId, epicKey);
            List<ProjectServerSystemLinkDTO> result = await query.ToListAsync();
            return result;
        }

        public List<ProjectServerSystemLinkDTO> GetList(Guid? projectUid = null, int? systemId = null,
            string projectId = null, string epicKey = null)
        {
            IQueryable<ProjectServerSystemLinkDTO> query = GetQuery(projectUid, systemId, projectId, epicKey);
            List<ProjectServerSystemLinkDTO> result = query.ToList();
            return result;
        }

        public async Task<int> UpdateLastExecuted(List<int> projectServerSystemLinkIds, DateTime dateTimeLastExecuted)
        {
            return await UnitOfWork.ProjectServerSystemLinkRepository
                .GetQuery(x => projectServerSystemLinkIds.Contains(x.ProjectServerSystemLinkId))
                .UpdateAsync(x => new ProjectServerSystemLink { LastExecuted = dateTimeLastExecuted });
        }

        public async Task<int> UpdateLastExecuted(List<ProjectServerSystemLinkDTO> projectServerSystemLinks,
            DateTime dateTimeLastExecuted)
        {
            List<int> projectServerSystemLinkIds = projectServerSystemLinks
                .Select(x => x.ProjectServerSystemLinkId)
                .ToList();
            return await UpdateLastExecuted(projectServerSystemLinkIds, dateTimeLastExecuted);
        }

        public List<string> GetEpicDirectLinks(int systemId)
        {
            List<string> epicDirectLinks = UnitOfWork.ProjectServerSystemLinkRepository.GetQuery()
                .Where(x => x.SystemId == systemId && !String.IsNullOrEmpty(x.EpicKey))
                .Select(x => x.EpicKey)
                .ToList();

            return epicDirectLinks;
        }

        public List<string> GetEpicsToIgnore(Guid projectUid, string projectId, SyncSystemDTO syncSystem)
        {
            return UnitOfWork.ProjectServerSystemLinkRepository
                .GetQuery(x => x.ProjectUid != projectUid
                               && !String.IsNullOrEmpty(x.EpicKey)
                               && x.SystemId == syncSystem.SystemId
                               && x.ProjectId == projectId)
                .Select(x => x.EpicKey)
                .ToList();
        }

        public void RemoveRange(Expression<Func<ProjectServerSystemLink, bool>> predicate)
        {
            UnitOfWork.ProjectServerSystemLinkRepository.RemoveRange(predicate);
        }

        public async Task LinkEpmToSystem(ProjectServerSystemLinkDTO syncObjectLinkViewModel)
        {
            List<ProjectServerSystemLinkDTO> linksToAddDto = new List<ProjectServerSystemLinkDTO>();

            List<ProjectServerSystemLinkDTO> links =
                GetList(syncObjectLinkViewModel.ProjectUid, syncObjectLinkViewModel.SystemId);
            if (syncObjectLinkViewModel.IsHomeProject)
            {
                ProjectServerSystemLinkDTO homeLink = links.FirstOrDefault(x => x.IsHomeProject);
                if (homeLink == null)
                {
                    homeLink = new ProjectServerSystemLinkDTO();
                    linksToAddDto.Add(homeLink);
                }
                if (homeLink.ProjectId != syncObjectLinkViewModel.ProjectId)
                {
                    homeLink.ProjectUid = syncObjectLinkViewModel.ProjectUid;
                    homeLink.SystemId = syncObjectLinkViewModel.SystemId;
                    homeLink.ProjectKey = syncObjectLinkViewModel.ProjectKey;
                    homeLink.ProjectName = syncObjectLinkViewModel.ProjectName;
                    homeLink.ProjectId = syncObjectLinkViewModel.ProjectId;
                    homeLink.IsHomeProject = syncObjectLinkViewModel.IsHomeProject;
                    homeLink.LastExecuted = syncObjectLinkViewModel.LastExecuted;
                    homeLink.ExecuteStatus = syncObjectLinkViewModel.ExecuteStatus;
                    homeLink.DateCreated = DateTime.Now;
                }
            }
            else
            {
                RemoveRange(x => x.ProjectUid == syncObjectLinkViewModel.ProjectUid
                                 && x.SystemId == syncObjectLinkViewModel.SystemId
                                 && x.IsHomeProject);
            }
            if (syncObjectLinkViewModel.Epics != null)
            {
                List<string> epicKeys = syncObjectLinkViewModel.Epics.Select(x => x.EpicKey).ToList();
                RemoveRange(x => x.ProjectUid == syncObjectLinkViewModel.ProjectUid
                                 && x.SystemId == syncObjectLinkViewModel.SystemId
                                 && !epicKeys.Contains(x.EpicKey) && !x.IsHomeProject);

                foreach (ProjectServerSystemLinkDTO epic in syncObjectLinkViewModel.Epics)
                {
                    ProjectServerSystemLinkDTO epicLink = links.FirstOrDefault(x => x.EpicKey == epic.EpicKey);
                    if (epicLink == null)
                    {
                        epicLink = new ProjectServerSystemLinkDTO();
                        linksToAddDto.Add(epicLink);
                    }
                    if (epicLink.EpicKey != epic.EpicKey)
                    {
                        epicLink.ProjectUid = syncObjectLinkViewModel.ProjectUid;
                        epicLink.SystemId = syncObjectLinkViewModel.SystemId;
                        epicLink.IsHomeProject = false;
                        epicLink.ProjectKey = epic.ProjectKey;
                        epicLink.ProjectName = epic.ProjectName;
                        epicLink.ProjectId = epic.ProjectId;
                        epicLink.EpicId = epic.EpicId;
                        epicLink.EpicName = epic.EpicName;
                        epicLink.EpicKey = epic.EpicKey;
                        epicLink.DateCreated = DateTime.Now;
                    }
                }
            }
            if (syncObjectLinkViewModel.Epics == null || syncObjectLinkViewModel.Epics.Count == 0)
            {
                RemoveRange(x => x.ProjectUid == syncObjectLinkViewModel.ProjectUid
                                 && x.SystemId == syncObjectLinkViewModel.SystemId
                                 && !x.IsHomeProject);
            }

            List<ProjectServerSystemLink> linksToAdd = linksToAddDto.Select(x => new ProjectServerSystemLink()
            {
                IsHomeProject = x.IsHomeProject,
                DateCreated = x.DateCreated,
                EpicId = x.EpicId,
                EpicKey = x.EpicKey,
                EpicName = x.EpicName,
                ExecuteStatus = x.ExecuteStatus,
                LastExecuted = x.LastExecuted,
                ProjectId = x.ProjectId,
                ProjectKey = x.ProjectKey,
                ProjectName = x.ProjectName,
                ProjectServerSystemLinkId = x.ProjectServerSystemLinkId,
                ProjectUid = x.ProjectUid,
                SystemId = x.SystemId
            })
                .ToList();

            UnitOfWork.ProjectServerSystemLinkRepository.AddRange(linksToAdd);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<List<ProjectServerSystemLinkDTO>> GetLinksAndBlocksWithEpmProjectsAsync(Guid? projectUid)
        {
            List<ProjectServerSystemLinkDTO> projectLinks = await UnitOfWork.VProjectServerSystemLinkRepository
                .GetQuery()
                .ProjectTo<ProjectServerSystemLinkDTO>().ToListAsync();
            if (projectUid != null && projectUid != Guid.Empty)
            {
                return projectLinks;
            }
            List<Guid> projectUids = projectLinks.Select(x => x.ProjectUid).Distinct().ToList();
            List<ODataProject> projects = projectOnlineODataService.GetODataProjects(projectUids);
            List<ProjectServerSystemLinkDTO> result = (from pl in projectLinks
                                                       join p in projects on pl.ProjectUid equals p.ProjectId
                                                       select new ProjectServerSystemLinkDTO
                                                       {
                                                           ProjectUid = pl.ProjectUid,
                                                           ProjectId = pl.ProjectId,
                                                           DateCreated = pl.DateCreated,
                                                           EpicId = pl.EpicId,
                                                           EpicName = pl.EpicName,
                                                           EpicKey = pl.EpicKey,
                                                           EpmProjectName = p.ProjectName,
                                                           ExecuteStatus = pl.ExecuteStatus,
                                                           IsHomeProject = pl.IsHomeProject,
                                                           LastExecuted = pl.LastExecuted,
                                                           ProjectKey = pl.ProjectKey,
                                                           ProjectName = pl.ProjectName,
                                                           ProjectServerSystemLinkId = pl.ProjectServerSystemLinkId,
                                                           RowNumber = pl.RowNumber,
                                                           SystemId = pl.SystemId,
                                                           SystemName = pl.SystemName,
                                                       }).ToList();
            return result;
        }
    }
}