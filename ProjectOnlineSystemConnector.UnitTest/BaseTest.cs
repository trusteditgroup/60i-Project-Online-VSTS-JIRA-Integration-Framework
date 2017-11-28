using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class BaseTest
    {
        public BaseTest()
        {
            ConfigureMapping();
        }

        private void ConfigureMapping()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<SyncSystem, SyncSystemDTO>();
                cfg.CreateMap<SyncSystemDTO, SyncSystem>();
                cfg.CreateMap<SyncSystemFieldMapping, SyncSystemFieldMappingDTO>();
                cfg.CreateMap<SyncSystemFieldMappingDTO, SyncSystemFieldMapping>();
                cfg.CreateMap<SyncSystemSetting, SyncSystemSettingDTO>();
                cfg.CreateMap<SyncSystemSettingDTO, SyncSystemSetting>();
                cfg.CreateMap<SyncSystemSettingValue, SyncSystemSettingValueDTO>();
                cfg.CreateMap<SyncSystemSettingValueDTO, SyncSystemSettingValue>();
                cfg.CreateMap<SyncSystemType, SyncSystemTypeDTO>();
                cfg.CreateMap<SyncSystemTypeDTO, SyncSystemType>();
                cfg.CreateMap<ProjectServerSystemLink, ProjectServerSystemLinkDTO>();
                cfg.CreateMap<ProjectServerSystemLinkDTO, ProjectServerSystemLink>();
                cfg.CreateMap<VProjectServerSystemLink, ProjectServerSystemLinkDTO>();
                cfg.CreateMap<ProjectServerSystemLinkDTO, VProjectServerSystemLink>();
            });
        }
    }
}