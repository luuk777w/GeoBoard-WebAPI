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
    public class BoardHub : Hub
    {
        private readonly IMapper _mapper;

        public BoardHub(
            IMapper mapper
)
        {
            _mapper = mapper;
        }

        public override Task OnConnectedAsync()
        {
            var sessionId = "board-abc";

            return Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
