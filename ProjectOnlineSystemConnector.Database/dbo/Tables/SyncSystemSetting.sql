CREATE TABLE [dbo].[SyncSystemSetting] (
    [SettingId] INT            IDENTITY (1, 1) NOT NULL,
    [Setting]   NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_SyncSystemSetting] PRIMARY KEY CLUSTERED ([SettingId] ASC)
);

