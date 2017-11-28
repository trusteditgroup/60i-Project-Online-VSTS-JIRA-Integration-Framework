


CREATE VIEW [dbo].[vStagingFieldMappingValue]
	AS
	SELECT ISNULL(CAST(ROW_NUMBER() OVER(ORDER BY SystemId, EpmFieldName, StagingId ASC) AS int), 0) AS RowNumber, val.*, map.EpmFieldName, map.FieldType, map.SystemFieldName, map.StagingFieldName, map.SystemId
	FROM [StagingFieldMappingValue] val 
		RIGHT JOIN SyncSystemFieldMapping map ON val.SyncSystemFieldMappingId = map.SyncSystemFieldMappingId



