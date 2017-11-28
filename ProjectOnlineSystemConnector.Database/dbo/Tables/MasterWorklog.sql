CREATE TABLE [dbo].[MasterWorklog] (
    [MasterWorklogId]    INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]           INT            CONSTRAINT [DF_MasterWorklog_SystemId] DEFAULT ((1)) NOT NULL,
    [RecordDateCreated]  DATETIME       CONSTRAINT [DF_MasterWorklog_RecordDateCreated] DEFAULT (getdate()) NOT NULL,
    [RecordDateUpdated]  DATETIME       CONSTRAINT [DF_MasterWorklog_RecordDateUpdated] DEFAULT (getdate()) NOT NULL,
    [WebHookEvent]       NVARCHAR (MAX) CONSTRAINT [DF_MasterWorklog_WebHookEvent] DEFAULT ('') NOT NULL,
    [RecordState]        NVARCHAR (50)  NOT NULL,
    [WorkLogId]          NVARCHAR (MAX) NULL,
    [IssueId]            NVARCHAR (50)  NULL,
    [TimeSpentSeconds]   BIGINT         NULL,
    [DateStarted]        DATETIME       NULL,
    [DateCreated]        DATETIME       NULL,
    [DateUpdated]        DATETIME       NULL,
    [Comment]            NVARCHAR (MAX) NULL,
    [AuthorEmailAddress] NVARCHAR (MAX) NULL,
    [AuthorName]         NVARCHAR (50)  NULL,
    [AuthorKey]          NVARCHAR (50)  NULL,
    CONSTRAINT [PK_MasterWorklog] PRIMARY KEY CLUSTERED ([MasterWorklogId] ASC),
    CONSTRAINT [FK_MasterWorklog_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);



