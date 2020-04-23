using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.DAL.Entities
{
    public interface IAppEntity
    {
        Guid Id { get; set; }
        DateTimeOffset CreationDateTime { get; set; }
    }
}
