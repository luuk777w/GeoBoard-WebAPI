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
using GeoBoardWebAPI.Models.Board;

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

        public async Task CreateBoard(CreateBoardMutateModel model)
        {
            await Clients.All.SendAsync("BoardCreated", model);
        }
    }
}
