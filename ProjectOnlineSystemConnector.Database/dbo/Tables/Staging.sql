CREATE TABLE [dbo].[Staging] (
    [StagingId]             INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]              INT            NOT NULL,
    [RecordDateCreated]     DATETIME       NOT NULL,
    [RecordDateUpdated]     DATETIME       NOT NULL,
    [WebHookEvent]          NVARCHAR (MAX) NOT NULL,
    [RecordState]           NVARCHAR (50)  NOT NULL,
    [ChangedFields]         NVARCHAR (MAX) NULL,
    [ProjectId]             NVARCHAR (50)  NULL,
    [ProjectKey]            NVARCHAR (200) NULL,
    [ProjectName]           NVARCHAR (MAX) NULL,
    [IssueId]               NVARCHAR (50)  NULL,
    [IssueKey]              NVARCHAR (200) NULL,
    [IssueName]             NVARCHAR (MAX) NULL,
    [IssueTypeId]           NVARCHAR (50)  NULL,
    [IssueTypeName]         NVARCHAR (50)  NULL,
    [IsSubTask]             BIT            CONSTRAINT [DF_Staging_IsSubTask] DEFAULT ((0)) NOT NULL,
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
    CONSTRAINT [PK_Stating] PRIMARY KEY CLUSTERED ([StagingId] ASC),
    CONSTRAINT [FK_Stating_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_Staging_SystemId_IssueKey]
    ON [dbo].[Staging]([SystemId] ASC, [IssueKey] ASC);

