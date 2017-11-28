CREATE TABLE [dbo].[WebHookEntry] (
    [WebHookEntryId] INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]       INT            NOT NULL,
    [JsonRequest]    NVARCHAR (MAX) NOT NULL,
    [DateCreated]    DATETIME       NOT NULL,
    CONSTRAINT [PK_WebHookEntry] PRIMARY KEY CLUSTERED ([WebHookEntryId] ASC),
    CONSTRAINT [FK_WebHookEntry_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE
);





