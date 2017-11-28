CREATE TABLE [dbo].[MasterHistoryFieldMappingValue] (
    [MasterHistoryFieldMappingValueId] INT            IDENTITY (1, 1) NOT NULL,
    [MasterHistoryId]                  INT            NOT NULL,
    [SyncSystemFieldMappingId]         INT            NOT NULL,
    [Value]                            NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_MasterHistoryFieldMappingValue] PRIMARY KEY CLUSTERED ([MasterHistoryFieldMappingValueId] ASC),
    CONSTRAINT [FK_FieldMappingValueMasterHistory_MasterHistory] FOREIGN KEY ([MasterHistoryId]) REFERENCES [dbo].[MasterHistory] ([MasterHistoryId]) ON DELETE CASCADE,
    CONSTRAINT [FK_FieldMappingValueMasterHistory_SyncSystemFieldMapping] FOREIGN KEY ([SyncSystemFieldMappingId]) REFERENCES [dbo].[SyncSystemFieldMapping] ([SyncSystemFieldMappingId])
);

