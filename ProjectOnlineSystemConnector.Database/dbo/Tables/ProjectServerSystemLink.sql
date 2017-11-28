CREATE TABLE [dbo].[ProjectServerSystemLink] (
    [ProjectServerSystemLinkId] INT              IDENTITY (1, 1) NOT NULL,
    [ProjectUid]                UNIQUEIDENTIFIER NOT NULL,
    [SystemId]                  INT              NOT NULL,
    [ProjectKey]                NVARCHAR (200)   NULL,
    [ProjectName]               NVARCHAR (200)   NULL,
    [ProjectId]                 NVARCHAR (200)   NULL,
    [IsHomeProject]             BIT              NOT NULL,
    [EpicKey]                   NVARCHAR (200)   NULL,
    [EpicName]                  NVARCHAR (200)   NULL,
    [EpicId]                    NVARCHAR (200)   NULL,
    [LastExecuted]              DATETIME         NULL,
    [ExecuteStatus]             NVARCHAR (MAX)   NULL,
    [DateCreated]               DATETIME         DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ProjectServerSystemLink] PRIMARY KEY CLUSTERED ([ProjectServerSystemLinkId] ASC),
    CONSTRAINT [FK_ProjectServerSystemLink_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);




GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectServerSystemLink_SystemId_EpicKey]
    ON [dbo].[ProjectServerSystemLink]([SystemId] ASC, [EpicKey] ASC) WHERE ([EpicKey] IS NOT NULL);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectServerSystemLink_SystemId_ProjectKey]
    ON [dbo].[ProjectServerSystemLink]([SystemId] ASC, [ProjectKey] ASC, [IsHomeProject] ASC) WHERE ([ProjectKey] IS NOT NULL AND [IsHomeProject]=(1));

