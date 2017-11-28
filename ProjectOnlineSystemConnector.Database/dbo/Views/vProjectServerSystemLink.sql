




CREATE VIEW [dbo].[vProjectServerSystemLink]
 AS
 SELECT ISNULL(CAST(ROW_NUMBER() OVER(ORDER BY ProjectServerSystemLinkId ASC) AS int), 0) AS RowNumber, val.*, map.SystemName
	FROM [ProjectServerSystemLink] val 
		LEFT JOIN SyncSystem map ON val.SystemId = map.SystemId







