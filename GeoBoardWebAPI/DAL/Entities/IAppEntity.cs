using System;

namespace GeoBoardWebAPI.DAL.Entities
{
    public interface IAppEntity
    {
        Guid Id { get; set; }

        DateTimeOffset CreatedAt { get; set; }
    }
}
