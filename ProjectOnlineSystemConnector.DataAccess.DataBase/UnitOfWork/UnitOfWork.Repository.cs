using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity;

namespace ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork
{
    public partial class UnitOfWork
    {
        public GenericRepository<T> GetGenericRepository<T>() where T : class
        {
            return new GenericRepository<T>(Context);
        }

        #region VProjectServerSystemLinkRepository

        private VProjectServerSystemLinkRepository vProjectServerSystemLinkRepository;
        public VProjectServerSystemLinkRepository VProjectServerSystemLinkRepository => vProjectServerSystemLinkRepository ?? (vProjectServerSystemLinkRepository = new VProjectServerSystemLinkRepository(Context));

        #endregion

        #region MasterRepository

        private MasterRepository masterRepository;
        public MasterRepository MasterRepository => masterRepository ?? (masterRepository = new MasterRepository(Context));

        #endregion

        #region MasterHistoryRepository

        private MasterHistoryRepository masterHistoryRepository;
        public MasterHistoryRepository MasterHistoryRepository => masterHistoryRepository ?? (masterHistoryRepository = new MasterHistoryRepository(Context));

        #endregion

        #region MasterWorklogRepository

        private MasterWorklogRepository masterWorklogRepository;
        public MasterWorklogRepository MasterWorklogRepository => masterWorklogRepository ?? (masterWorklogRepository = new MasterWorklogRepository(Context));

        #endregion

        #region MasterFieldMappingValueRepository

        private MasterFieldMappingValueRepository masterFieldMappingValueRepository;
        public MasterFieldMappingValueRepository MasterFieldMappingValueRepository => masterFieldMappingValueRepository ?? (masterFieldMappingValueRepository = new MasterFieldMappingValueRepository(Context));

        #endregion

        #region MasterHistoryFieldMappingValueRepository

        private MasterHistoryFieldMappingValueRepository masterHistoryFieldMappingValueRepository;
        public MasterHistoryFieldMappingValueRepository MasterHistoryFieldMappingValueRepository => masterHistoryFieldMappingValueRepository ?? (masterHistoryFieldMappingValueRepository = new MasterHistoryFieldMappingValueRepository(Context));

        #endregion

        #region StagingRepository

        private StagingRepository stagingRepository;
        public StagingRepository StagingRepository => stagingRepository ?? (stagingRepository = new StagingRepository(Context));

        #endregion

        #region StagingFieldMappingValueRepository

        private StagingFieldMappingValueRepository stagingFieldMappingValueRepository;
        public StagingFieldMappingValueRepository StagingFieldMappingValueRepository => stagingFieldMappingValueRepository ?? (stagingFieldMappingValueRepository = new StagingFieldMappingValueRepository(Context));

        #endregion

        #region VStagingFieldMappingValueRepository

        private VStagingFieldMappingValueRepository vStagingFieldMappingValueRepository;
        public VStagingFieldMappingValueRepository VStagingFieldMappingValueRepository => vStagingFieldMappingValueRepository ?? (vStagingFieldMappingValueRepository = new VStagingFieldMappingValueRepository(Context));

        #endregion

        //#region WebHookEntryRepository

        //private WebHookEntryRepository webHookEntryRepository;
        //public WebHookEntryRepository WebHookEntryRepository => webHookEntryRepository ?? (webHookEntryRepository = new WebHookEntryRepository(Context));

        //#endregion

        #region SyncSystemFieldMappingRepository

        private SyncSystemFieldMappingRepository syncSystemFieldMappingRepository;
        public SyncSystemFieldMappingRepository SyncSystemFieldMappingRepository => syncSystemFieldMappingRepository ?? (syncSystemFieldMappingRepository = new SyncSystemFieldMappingRepository(Context));

        #endregion

        #region SyncSystemSettingRepository

        private SyncSystemSettingRepository syncSystemSettingRepository;
        public SyncSystemSettingRepository SyncSystemSettingRepository => syncSystemSettingRepository ?? (syncSystemSettingRepository = new SyncSystemSettingRepository(Context));

        #endregion

        #region SyncSystemSettingValueRepository

        private SyncSystemSettingValueRepository syncSystemSettingValueRepository;
        public SyncSystemSettingValueRepository SyncSystemSettingValueRepository => syncSystemSettingValueRepository ?? (syncSystemSettingValueRepository = new SyncSystemSettingValueRepository(Context));

        #endregion

        #region SyncSystemRepository

        private SyncSystemRepository syncSystemRepository;
        public SyncSystemRepository SyncSystemRepository => syncSystemRepository ?? (syncSystemRepository = new SyncSystemRepository(Context));

        #endregion

        #region SyncSystemTypeRepository

        private SyncSystemTypeRepository syncSystemTypeRepository;
        public SyncSystemTypeRepository SyncSystemTypeRepository => syncSystemTypeRepository ?? (syncSystemTypeRepository = new SyncSystemTypeRepository(Context));

        #endregion

        #region ProjectServerSystemLinkRepository

        private ProjectServerSystemLinkRepository projectServerSystemLinkRepository;
        public ProjectServerSystemLinkRepository ProjectServerSystemLinkRepository => projectServerSystemLinkRepository ?? (projectServerSystemLinkRepository = new ProjectServerSystemLinkRepository(Context));

        #endregion

        #region Stored procedures

        public int CleanMasterAndMasterWorklog()
        {
            return Context.CallCleanMasterAndMasterWorklog();
        }

        #endregion
    }
}