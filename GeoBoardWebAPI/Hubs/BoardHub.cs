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
using Microsoft.AspNetCore.Identity;

namespace GeoBoardWebAPI.Hubs
{
    [Authorize]
    public class BoardHub : Hub
    {
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;

        private readonly BoardRepository BoardRepository;

        public BoardHub(
            IMapper mapper,
            UserManager<User> userManager,
            BoardRepository boardRepository
        )
        {
            _mapper = mapper;
            _userManager = userManager;
            BoardRepository = boardRepository;
        }

        public async Task CreateBoard(CreateBoardMutateModel model)
        {
            await Clients.All.SendAsync("BoardCreated", model);
        }

        /// <summary>
        /// Switch from the given board to another given board.
        /// </summary>
        /// <param name="fromBoardId">The board that is switched from (if any)</param>
        /// <param name="toBoardId">The board to switch to.</param>
        /// <returns></returns>
        public async Task<BoardViewModel> SwitchBoard(Guid? fromBoardId, Guid toBoardId)
        {
            // Attemp to find the board.
            var board = await BoardRepository
                .GetAll()
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.Id.Equals(toBoardId));

            // Check if the board is found.
            if (board == null) {
                await Clients.Caller.SendAsync("BoardNotFound", toBoardId);

                return null;
            }

            // Check if the current user is part of the board.
            bool userIsPartOfBoard = board.Users.Any(ub => ub.UserId.Equals(GetUserId()));

            // Get the user from the current request.
            User user = await _userManager.FindByIdAsync(GetUserId().ToString());

            // Check if the current user has permission to switch to the requested board.
            if (board.UserId.Equals(GetUserId()) || userIsPartOfBoard || (await _userManager.IsInRoleAsync(user, "Administrator")))
            {
                // Remove the user from the current board (if any).
                if (fromBoardId != null)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, fromBoardId.ToString());

                // Add the user to the requested board.
                await Groups.AddToGroupAsync(Context.ConnectionId, board.Id.ToString());

                // Notify the listeners.
                await Clients.Caller.SendAsync("SwitchedBoard", _mapper.Map<BoardViewModel>(board));

                // Return the board model for later usage.
                return _mapper.Map<BoardViewModel>(board);
            }

            // Access denied, send a BoardNotFound to hide the existence of the board.
            await Clients.Caller.SendAsync("BoardNotFound", toBoardId);

            return null;
        }

        private Guid? GetUserId()
        {
            return new Guid(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
