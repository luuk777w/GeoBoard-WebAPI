using AutoMapper;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Extensions;
using GeoBoardWebAPI.Models.Board;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

            if (!string.IsNullOrEmpty(userId))
            {
                // Remove the user from its assigned board (if any).
                await LeaveBoard(this.ConnectionMapping.GetUserBoard(userId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task CreateBoard(CreateBoardMutateModel model)
        {
            await Clients.All.SendAsync("BoardCreated", model);
        }

        /// <summary>
        /// Switch from the given board to another given board.
        /// </summary>
        /// <param name="boardIdToSwitchFrom">The BoardId to switch from (if any).</param>
        /// <param name="boardIdToSwitchTo">The BoardId to switch to.</param>
        /// <returns></returns>
        public async Task<BoardViewModel> SwitchBoard(Guid? boardIdToSwitchFrom, Guid boardIdToSwitchTo)
        {
            if (boardIdToSwitchFrom.Equals(boardIdToSwitchTo))
            {
                await LeaveBoard(boardIdToSwitchTo.ToString());
                await Clients.Caller.SendAsync("SwitchedBoard", null);

                return null;
            }

            User user = await _userManager.FindByIdAsync(GetUserId().ToString());
            var boardToSwitchTo = await BoardRepository.GetAll()
                .Include(b => b.Users)
                .Include(b => b.Elements)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(b => b.Id.Equals(boardIdToSwitchTo));


            boardToSwitchTo.Elements = boardToSwitchTo.Elements.OrderByDescending(e => e.ElementNumber).ToList();

            if (boardToSwitchTo == null || ! user.HasPermissionToSwitchBoard(boardToSwitchTo, _userManager)) 
            {
                await Clients.Caller.SendAsync("BoardNotFound", boardIdToSwitchTo);
                return null;
            }

            if (boardIdToSwitchFrom.HasValue) await LeaveBoard(boardIdToSwitchFrom.ToString());

            await JoinBoard(boardToSwitchTo.Id.ToString());
            await Clients.Caller.SendAsync("SwitchedBoard", _mapper.Map<BoardViewModel>(boardToSwitchTo));

            return _mapper.Map<BoardViewModel>(boardToSwitchTo);
        }

        private async Task JoinBoard(string boardId)
        {
            ConnectionMapping.SetUserBoard(GetUserId().ToString(), Context.User.Identity.Name, boardId);
            await Groups.AddToGroupAsync(Context.ConnectionId, boardId);

            await Clients.Group(boardId).SendAsync("UserJoinedBoard", new
            {
                UserId = GetUserId(),
                UserName = Context.User.Identity.Name,
                BoardId = boardId,
                JoinedUsers = ConnectionMapping.GetJoinedBoardUsers(boardId).OrderBy(bu => bu.UserName)
            });
        }

        private async Task LeaveBoard(string boardId)
        {
            if (boardId == null)
                return;

            // Remove the user from the group.
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId.ToString());

            // Remove the user.
            this.ConnectionMapping.RemoveUser(GetUserId().ToString());

            // Notify other users about this user leaving.
            await Clients.OthersInGroup(boardId).SendAsync("UserLeftBoard", new
            {
                UserId = GetUserId(),
                UserName = Context.User.Identity.Name,
                BoardId = boardId,
                JoinedUsers = ConnectionMapping.GetJoinedBoardUsers(boardId).OrderBy(bu => bu.UserName)
            });
        }

        private Guid? GetUserId()
        {
            return new Guid(Context.User.FindFirstValue(JwtRegisteredClaimNames.NameId));
        }
    }
}
