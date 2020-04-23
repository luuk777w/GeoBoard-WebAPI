using AutoMapper;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.Models.Account;
using GeoBoardWebAPI.Models.Country;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Extensions
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Country, CountryViewModel>();
            CreateMap<CountryMutateModel, Country>();

            CreateMap<User, UserViewModel>();
            CreateMap<UserMutateModel, User>();
        }
    }
}