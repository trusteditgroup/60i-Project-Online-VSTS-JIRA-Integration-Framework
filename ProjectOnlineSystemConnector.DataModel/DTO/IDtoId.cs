using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public interface IDtoId
    {
        int Id { get; set; }
        // ReSharper disable once InconsistentNaming
        Guid __KEY__ { get; set; }
    }
}