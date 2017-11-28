CREATE TABLE [dbo].[MasterFieldMappingValue] (
    [MasterFieldMappingValueId] INT            IDENTITY (1, 1) NOT NULL,
    [MasterId]                  INT            NOT NULL,
    [SyncSystemFieldMappingId]  INT            NOT NULL,
    [Value]                     NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_MasterFieldMappingValue] PRIMARY KEY CLUSTERED ([MasterFieldMappingValueId] ASC),
    CONSTRAINT [FK_FieldMappingValueMaster_Master] FOREIGN KEY ([MasterId]) REFERENCES [dbo].[Master] ([MasterId]) ON DELETE CASCADE,
    CONSTRAINT [FK_FieldMappingValueMaster_SyncSystemFieldMapping] FOREIGN KEY ([SyncSystemFieldMappingId]) REFERENCES [dbo].[SyncSystemFieldMapping] ([SyncSystemFieldMappingId])
);

