using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;

namespace GeoBoardWebAPI.DAL.Repositories
{
    public class BoardRepository : Repository<Board>
    {
        public BoardRepository(ApplicationDbContext context) : base(context)
        {
        }

    }
}
