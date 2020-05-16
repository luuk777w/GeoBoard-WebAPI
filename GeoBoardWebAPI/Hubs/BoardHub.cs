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

        private ConnectionMapping ConnectionMapping;

        public BoardHub(
            IMapper mapper,
            UserManager<User> userManager,
            BoardRepository boardRepository,
            ConnectionMapping connectionMapping
        )
        {
            _mapper = mapper;
            _userManager = userManager;
            BoardRepository = boardRepository;
            ConnectionMapping = connectionMapping;
        }

        public override async Task OnConnectedAsync()
        {
            // If a user is part of board, the currentBoard will be added as a query parameter by the client.
            var boardId = Context.GetHttpContext().Request.Query["currentBoard"].FirstOrDefault();

            // Proceed if the user has joined a board directly (from a previous session).
            if (!string.IsNullOrEmpty(boardId))
            {
                await JoinBoard(boardId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = GetUserId().ToString();

            // Remove the user from its assigned board (if any).
            await LeaveBoard(this.ConnectionMapping.GetUserBoard(userId));

            await base.OnDisconnectedAsync(exception);
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
            var newBoard = await BoardRepository
                .GetAll()
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.Id.Equals(toBoardId));

            // Check if the board is found.
            if (newBoard == null) {
                await Clients.Caller.SendAsync("BoardNotFound", toBoardId);

                return null;
            }

            // Check if the current user is part of the board.
            bool userIsPartOfBoard = newBoard.Users.Any(ub => ub.UserId.Equals(GetUserId()));

            // Get the user from the current request.
            User user = await _userManager.FindByIdAsync(GetUserId().ToString());

            // Check if the current user has permission to switch to the requested board.
            if (newBoard.UserId.Equals(GetUserId()) || userIsPartOfBoard || (await _userManager.IsInRoleAsync(user, "Administrator")))
            {
                // Remove the user from the current board (if any).
                if (fromBoardId != null)
                {
                    // Notify the other users about leaving the board.
                    await LeaveBoard(fromBoardId.ToString());
                }

                // Notify the other users.
                await JoinBoard(newBoard.Id.ToString());

                // Notify the listeners.
                await Clients.Caller.SendAsync("SwitchedBoard", _mapper.Map<BoardViewModel>(newBoard));

                // Return the board model for later usage.
                return _mapper.Map<BoardViewModel>(newBoard);
            }

            // Access denied, send a BoardNotFound to hide the existence of the board.
            await Clients.Caller.SendAsync("BoardNotFound", toBoardId);

            return null;
        }

        private async Task JoinBoard(string boardId)
        {
            // Set the current board for the current user.
            //Context.Items[GetUserId()] = boardId;

            this.ConnectionMapping.SetUserBoard(GetUserId().ToString(), Context.User.Identity.Name, boardId);

            // Add the user to the group named with the board id.
            await Groups.AddToGroupAsync(Context.ConnectionId, boardId);

            var test = this.ConnectionMapping.GetJoinedBoardUsers(boardId)
                    //.Where(bu => !bu.Id.Equals(GetUserId().ToString()))
                    .OrderBy(bu => bu.Username);

            // Notify other users about this user joining.
            await Clients.Group(boardId).SendAsync("UserJoinedBoard", new
            {
                UserId = GetUserId(),
                Username = Context.User.Identity.Name,
                BoardId = boardId,
                JoinedUsers = test
            });
        }

        private async Task LeaveBoard(string boardId)
        {
            if (boardId == null)
                return;

            // Remove the user from the group.
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId.ToString());

            // Notify other users about this user leaving.
            await Clients.OthersInGroup(boardId).SendAsync("UserLeftBoard", new
            {
                UserId = GetUserId(),
                Username = Context.User.Identity.Name,
                BoardId = boardId
            });

            // Remove the user.
            //Context.Items.Remove(GetUserId());
            this.ConnectionMapping.RemoveUser(GetUserId().ToString());
        }

        private Guid? GetUserId()
        {
            return new Guid(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
