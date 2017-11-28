-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[CleanMasterAndMasterWorklog]
AS
BEGIN
DELETE FROM MasterWorklog WHERE MasterWorklogId NOT IN (
	SELECT MasterWorklogId FROM (SELECT SystemId, WorkLogId, max(MasterWorklogId) as MasterWorklogId 
		FROM MasterWorklog GROUP BY SystemId, WorkLogId) tmp)

DELETE FROM Master WHERE MasterId NOT IN (
	SELECT MasterId FROM (SELECT SystemId, IssueKey, max(MasterId) as MasterId 
		FROM Master GROUP BY SystemId, IssueKey) tmp)
END


