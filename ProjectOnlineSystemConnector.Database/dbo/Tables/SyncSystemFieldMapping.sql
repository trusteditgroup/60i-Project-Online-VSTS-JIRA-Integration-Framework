CREATE TABLE [dbo].[SyncSystemFieldMapping] (
    [SyncSystemFieldMappingId] INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]                 INT            NOT NULL,
    [SystemFieldName]          NVARCHAR (MAX) NULL,
    [EpmFieldName]             NVARCHAR (MAX) NOT NULL,
    [FieldType]                NVARCHAR (MAX) NOT NULL,
    [StagingFieldName]         NVARCHAR (MAX) NULL,
    [IsMultiSelect]            BIT            CONSTRAINT [DF_SyncSystemFieldMapping_IsMultiSelect] DEFAULT ((0)) NOT NULL,
    [IsIdWithValue]            BIT            CONSTRAINT [DF_SyncSystemFieldMapping_IsSelectable] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_SyncSystemFieldMapping] PRIMARY KEY CLUSTERED ([SyncSystemFieldMappingId] ASC),
    CONSTRAINT [FK_SyncSystemFieldMapping_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);



