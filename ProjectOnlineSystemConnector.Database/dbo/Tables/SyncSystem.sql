CREATE TABLE [dbo].[SyncSystem] (
    [SystemId]              INT            IDENTITY (1, 1) NOT NULL,
    [SystemTypeId]          INT            NOT NULL,
    [SystemName]            NVARCHAR (MAX) NOT NULL,
    [SystemUrl]             NVARCHAR (MAX) NOT NULL,
    [SystemApiUrl]          NVARCHAR (MAX) NOT NULL,
    [SystemLogin]           NVARCHAR (MAX) NOT NULL,
    [SystemPassword]        NVARCHAR (MAX) NOT NULL,
    [DefaultParentTaskName] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_SyncSystem] PRIMARY KEY CLUSTERED ([SystemId] ASC),
    CONSTRAINT [FK_SyncSystem_SyncSystemType] FOREIGN KEY ([SystemTypeId]) REFERENCES [dbo].[SyncSystemType] ([SystemTypeId]) ON DELETE CASCADE
);

