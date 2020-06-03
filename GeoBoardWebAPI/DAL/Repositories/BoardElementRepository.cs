using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;

namespace GeoBoardWebAPI.DAL.Repositories
{
    public class BoardElementRepository : Repository<BoardElement>
    {
        public BoardElementRepository(ApplicationDbContext context) : base(context)
        {
        }

    }
}
