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
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GeoBoardWebAPI.Hubs
{
    [Authorize]
    public class BoardHub : Hub
    {
        private readonly IMapper _mapper;

        private readonly BoardRepository BoardRepository;

        public BoardHub(
            IMapper mapper,
            BoardRepository boardRepository
        )
        {
            _mapper = mapper;
            BoardRepository = boardRepository;
        }

        public async Task CreateBoard(CreateBoardMutateModel model)
        {
            await Clients.All.SendAsync("BoardCreated", model);
        }

        public async Task<BoardViewModel> SwitchBoard(Guid? fromBoardId, Guid toBoardId)
        {
            // Attemp to find the board.
            var board = await BoardRepository.GetAll().FirstOrDefaultAsync(b => b.Id.Equals(toBoardId));

            if (board == null) {
                await Clients.Caller.SendAsync("BoardNotFound", toBoardId);

                return null;
            }

            var userId = new Guid(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (fromBoardId != null)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, fromBoardId.ToString());

            await Groups.AddToGroupAsync(Context.ConnectionId, board.Id.ToString());

            await Clients.Caller.SendAsync("SwitchedBoard", _mapper.Map<BoardViewModel>(board));

            return _mapper.Map<BoardViewModel>(board);
        }
    }
}
