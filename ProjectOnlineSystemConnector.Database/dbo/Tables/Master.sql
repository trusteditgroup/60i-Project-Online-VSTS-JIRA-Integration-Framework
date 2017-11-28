CREATE TABLE [dbo].[Master] (
    [MasterId]              INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]              INT            NOT NULL,
    [RecordDateCreated]     DATETIME       NOT NULL,
    [RecordDateUpdated]     DATETIME       NOT NULL,
    [ProjectId]             NVARCHAR (50)  NULL,
    [ProjectKey]            NVARCHAR (200) NULL,
    [ProjectName]           NVARCHAR (MAX) NULL,
    [IssueId]               NVARCHAR (50)  NULL,
    [IssueKey]              NVARCHAR (200) NULL,
    [IssueName]             NVARCHAR (MAX) NULL,
    [IssueTypeId]           NVARCHAR (50)  NULL,
    [IssueTypeName]         NVARCHAR (50)  NULL,
    [IsSubTask]             BIT            CONSTRAINT [DF_Master_IsSubTask] DEFAULT ((0)) NOT NULL,
    [ParentEpicId]          NVARCHAR (50)  NULL,
    [ParentEpicKey]         NVARCHAR (50)  NULL,
    [ParentSprintId]        NVARCHAR (50)  NULL,
    [ParentSprintName]      NVARCHAR (MAX) NULL,
    [ParentVersionId]       NVARCHAR (50)  NULL,
    [ParentVersionName]     NVARCHAR (MAX) NULL,
    [ParentVersionReleased] BIT            NULL,
    [ParentIssueId]         NVARCHAR (50)  NULL,
    [ParentIssueKey]        NVARCHAR (50)  NULL,
    [DateStart]             DATETIME       NULL,
    [DateFinish]            DATETIME       NULL,
    [Assignee]              NVARCHAR (MAX) NULL,
    [IssueStatus]           NVARCHAR (200) NULL,
    [DateRelease]           DATETIME       NULL,
    [DateStartActual]       DATETIME       NULL,
    [DateFinishActual]      DATETIME       NULL,
    [IssueActualWork]       INT            NULL,
    [OriginalEstimate]      INT            NULL,
    CONSTRAINT [PK_Master] PRIMARY KEY CLUSTERED ([MasterId] ASC),
    CONSTRAINT [FK_Master_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_Master_SystemId_IssueKey]
    ON [dbo].[Master]([SystemId] ASC, [IssueKey] ASC);

