using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.ProjectServer.Client;
using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataModel.OData;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class ProjectOnlineAccessServiceUnitTest
    {
        [TestMethod]
        public void GetEnterpriseResourcesTestMethod()
        {

            int oDataEntitesPerRequest = int.Parse(ConfigurationManager.AppSettings["ODataEntitesPerRequest"]);
            int oDataMaxQueryLength = int.Parse(ConfigurationManager.AppSettings["ODataMaxQueryLength"]);
            int requestTimeout = int.Parse(ConfigurationManager.AppSettings["RequestTimeout"]);
            bool isProjectOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);

            ProjectOnlineAccessService projectOnlineAccessService = new ProjectOnlineAccessService(ConfigurationManager
                .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline, Guid.NewGuid());

            List<EnterpriseResource> enterpriseResources = projectOnlineAccessService.GetEnterpriseResources(new List<Guid>() { Guid.NewGuid() });

            Assert.AreNotEqual(0, enterpriseResources.Count);
        }

        [TestMethod]
        public void TimesheetTest()
        {
            Guid guidYaroslav = Guid.Parse("9f52b233-4135-e511-80d0-00155d646d0b");
            //string emailYaroslav = "Yaroslav@trusteditgroup.com";

            Guid guidAlex = Guid.Parse("a72f2f98-bd2f-e511-80c9-00155d60910a");
            //string emailAlex = "alex.rodov@trusteditgroup.com";

            Guid guidKiryl = Guid.Parse("79d8d031-0801-e711-80d3-00155d74dc09");
            //string emailKiryl = "Kiryl.Shpak@trusteditgroup.com";

            int oDataEntitesPerRequest = int.Parse(ConfigurationManager.AppSettings["ODataEntitesPerRequest"]);
            int oDataMaxQueryLength = int.Parse(ConfigurationManager.AppSettings["ODataMaxQueryLength"]);
            int requestTimeout = int.Parse(ConfigurationManager.AppSettings["RequestTimeout"]);
            bool isProjectOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);

            ProjectOnlineAccessService projectOnlineAccessService = new ProjectOnlineAccessService(ConfigurationManager
                    .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline, Guid.NewGuid());

            ProjectOnlineODataService projectOnlineODataService = new ProjectOnlineODataService(ConfigurationManager
                    .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline);


            List<ODataAssignment> assignments = projectOnlineODataService
                .GetODataAssignments(Guid.Parse("8cc0d309-bd02-e711-80d4-00155d70390b"));

            ODataAssignment assignmentYaroslavYaroslavHours = assignments
                .FirstOrDefault(x => x.TaskName == "Yaroslav Hours" && x.ResourceId == guidYaroslav);
            ODataAssignment assignmentYaroslavAlexHours = assignments
                .FirstOrDefault(x => x.TaskName == "Alex Hours" && x.ResourceId == guidYaroslav);

            ODataAssignment assignmentKyrilKyrilHours = assignments
                .FirstOrDefault(x => x.TaskName == "Kyril Hours" && x.ResourceId == guidKiryl);
            ODataAssignment assignmentKyrilAlexHours = assignments
                .FirstOrDefault(x => x.TaskName == "Alex Hours" && x.ResourceId == guidKiryl);

            if (assignmentYaroslavYaroslavHours == null || assignmentYaroslavAlexHours == null ||
                assignmentKyrilKyrilHours == null || assignmentKyrilAlexHours == null)
            {
                return;
            }
            //EnterpriseResource resourceAlex = projectOnlineAccessService.ProjectContext.EnterpriseResources.GetByGuid(guidAlex);
            //projectOnlineAccessService.ProjectContext.Load(resourceAlex.Assignments);
            //EnterpriseResource resourceKyril = projectOnlineAccessService.ProjectContext.EnterpriseResources.GetByGuid(guidKiryl);
            //projectOnlineAccessService.ProjectContext.Load(resourceKyril.Assignments);

            EnterpriseResource resourceYaroslav = projectOnlineAccessService.ProjectContext.EnterpriseResources.GetByGuid(guidYaroslav);
            TimePhase timePhaseYaroslav = resourceYaroslav.Assignments.GetTimePhase(new DateTime(2017, 8, 22), new DateTime(2017, 8, 22));

            projectOnlineAccessService.ProjectContext.Load(resourceYaroslav.Assignments);
            projectOnlineAccessService.ProjectContext.Load(timePhaseYaroslav);
            projectOnlineAccessService.ProjectContext.Load(timePhaseYaroslav.Assignments);
            projectOnlineAccessService.ProjectContext.ExecuteQuery();

            foreach (StatusAssignment statusAssignment in timePhaseYaroslav.Assignments)
            {
                if (statusAssignment.Id == assignmentYaroslavYaroslavHours.AssignmentId)
                {
                    statusAssignment.ActualWork = "1h";
                }
                if (statusAssignment.Id == assignmentYaroslavAlexHours.AssignmentId)
                {
                    statusAssignment.ActualWork = "2h";
                }
                if (statusAssignment.Id == assignmentKyrilKyrilHours.AssignmentId)
                {
                    statusAssignment.ActualWork = "3h";
                }
                if (statusAssignment.Id == assignmentKyrilAlexHours.AssignmentId)
                {
                    statusAssignment.ActualWork = "4h";
                }
            }
            timePhaseYaroslav.Assignments.Update();
            timePhaseYaroslav.Assignments.SubmitAllStatusUpdates($"Submitted by CSOM on {DateTime.Now}");
            projectOnlineAccessService.ProjectContext.ExecuteQuery();
            var temp = 1;
            //projectOnlineAccessService.ProjectContext.Load(projectOnlineAccessService.ProjectContext.TimeSheetPeriods, c => c.Where(p => p.Start <= DateTime.Now && p.End >= DateTime.Now).IncludeWithDefaultProperties(p => p.TimeSheet, p => p.TimeSheet.Lines.Where(l => l.LineClass == TimeSheetLineClass.StandardLine).IncludeWithDefaultProperties(l => l.Assignment, l => l.Assignment.Task, l => l.Work)));

            //projectOnlineAccessService.ProjectContext.ExecuteQuery();
        }
    }
}