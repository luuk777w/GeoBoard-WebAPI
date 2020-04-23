using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GeoBoardWebAPI.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using GeoBoardWebAPI.Services;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.Models;
using AutoMapper;
using GeoBoardWebAPI.Controllers;

namespace GeoBoardWebAPI.Hubs
{
    public class HomeHub : Hub
    {
        private readonly UserRepository UserRepository;
        private readonly IMapper _mapper;

        public HomeHub(UserRepository userRepository,
            IMapper mapper
)
        {
            UserRepository = userRepository;
            _mapper = mapper;
        }

        public override Task OnConnectedAsync()
        {
            var sessionId = "home";

            return Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
