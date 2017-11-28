CREATE TABLE [dbo].[SyncSystemType] (
    [SystemTypeId]   INT            IDENTITY (1, 1) NOT NULL,
    [SystemTypeName] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_SyncSystemType] PRIMARY KEY CLUSTERED ([SystemTypeId] ASC)
);

