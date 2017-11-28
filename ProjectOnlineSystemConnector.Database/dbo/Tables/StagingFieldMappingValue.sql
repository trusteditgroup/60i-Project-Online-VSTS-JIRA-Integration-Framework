CREATE TABLE [dbo].[StagingFieldMappingValue] (
    [StagingFieldMappingValueId] INT            IDENTITY (1, 1) NOT NULL,
    [StagingId]                  INT            NOT NULL,
    [SyncSystemFieldMappingId]   INT            NOT NULL,
    [Value]                      NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_StagingFieldMappingValue] PRIMARY KEY CLUSTERED ([StagingFieldMappingValueId] ASC),
    CONSTRAINT [FK_FieldMappingValueStaging_Staging] FOREIGN KEY ([StagingId]) REFERENCES [dbo].[Staging] ([StagingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_FieldMappingValueStaging_SyncSystemFieldMapping] FOREIGN KEY ([SyncSystemFieldMappingId]) REFERENCES [dbo].[SyncSystemFieldMapping] ([SyncSystemFieldMappingId])
);

