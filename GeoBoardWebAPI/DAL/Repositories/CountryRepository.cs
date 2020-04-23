using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;

namespace GeoBoardWebAPI.DAL.Repositories
{
    public class CountryRepository : Repository<Country>
    {
        public CountryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
