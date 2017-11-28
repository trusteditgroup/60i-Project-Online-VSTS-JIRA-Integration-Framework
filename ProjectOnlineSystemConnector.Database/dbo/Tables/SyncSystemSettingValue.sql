CREATE TABLE [dbo].[SyncSystemSettingValue] (
    [SyncSystemSettingId] INT            IDENTITY (1, 1) NOT NULL,
    [SystemId]            INT            NOT NULL,
    [SettingId]           INT            NOT NULL,
    [SettingValue]        NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_SyncSystemSettingValue] PRIMARY KEY CLUSTERED ([SyncSystemSettingId] ASC),
    CONSTRAINT [FK_SyncSystemSettingValue_SyncSystem] FOREIGN KEY ([SystemId]) REFERENCES [dbo].[SyncSystem] ([SystemId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SyncSystemSettingValue_SyncSystemSetting] FOREIGN KEY ([SettingId]) REFERENCES [dbo].[SyncSystemSetting] ([SettingId]) ON DELETE CASCADE
);

