-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[CleanStaging]
AS
BEGIN
DELETE FROM Staging WHERE StagingId NOT IN (
	SELECT StagingId FROM (SELECT SystemId, IssueKey, max(StagingId) as StagingId 
		FROM Staging GROUP BY SystemId, IssueKey) tmp)
END