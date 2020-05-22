using AutoMapper;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.Models.Account;
using GeoBoardWebAPI.Models.Board;
using GeoBoardWebAPI.Models.Country;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Util
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Country, CountryViewModel>();
            CreateMap<CountryMutateModel, Country>();

            CreateMap<User, UserViewModel>();
            CreateMap<User, BoardElementUserViewModel>();
            CreateMap<UserMutateModel, User>();

            CreateMap<Board, BoardViewModel>();
            CreateMap<CreateBoardMutateModel, Board>();

            CreateMap<BoardElement, BoardElementViewModel>();
            //CreateMap<CreateBoardElementMutateModel, BoardElement>();
        }
    }
}