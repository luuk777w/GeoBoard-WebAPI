using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Util.Authorization;
using GeoBoardWebAPI.Models.Board;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using GeoBoardWebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Hangfire;
using GeoBoardWebAPI.Models.Board.Email;
using GeoBoardWebAPI.Services;
using GeoBoardWebAPI.Models.Account;

namespace GeoBoardWebAPI.Controllers
{
    [Route("boards")]
    public class BoardController : BaseController
    {
        private readonly BoardRepository BoardRepository;
        private readonly AppUserManager appUserManager;
        private readonly IHubContext<BoardHub> _hubContext;
        private readonly ConnectionMapping ConnectionMapping;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IEmailService _emailService;
        public IConfiguration Configuration { get; }

        public BoardController(
            IServiceProvider scopeFactory,
            BoardRepository boardRepository,
            AppUserManager appUserManager,
            IConfiguration configuration,
            IHubContext<BoardHub> hubContext,
            ConnectionMapping connectionMapping,
            IBackgroundJobClient backgroundJobs,
            IEmailService emailService
        ) : base(scopeFactory)
        {
            BoardRepository = boardRepository;
            this.appUserManager = appUserManager;
            Configuration = configuration;
            _hubContext = hubContext;
            ConnectionMapping = connectionMapping;
            _backgroundJobs = backgroundJobs;
            _emailService = emailService;
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> GetAllBoards()
        {
            var boards = BoardRepository
                .GetAll()
                .AsNoTracking();

            return Ok(
                _mapper.Map<List<BoardViewModel>>(await boards.ToListAsync())
            );
        }

        [Authorize]
        [Route("/player-boards")]
        public async Task<IActionResult> GetActiveBoards()
        {
            var boards = BoardRepository
                .GetAll()
                .Include(b => b.Owner)
                // The user must either be the owner of the board, or be part of the board as a player.
                .Where(b => b.UserId.Equals(GetUserId()) || b.Users.Any(ub => ub.UserId.Equals(GetUserId())))
                .AsNoTracking();

            return Ok(
                _mapper.Map<List<BoardViewModel>>(await boards.ToListAsync())
            );
        }

        [Authorize]
        [HttpGet("{boardId}")]
        public async Task<IActionResult> GetBoard([FromRoute] Guid boardId, [FromQuery] bool includeElements)
        {
            var data = BoardRepository
                .GetAll()
                .Include(b => b.Users)
                .AsQueryable();

            if (includeElements)
            {
                data = data
                    .Include(b => b.Elements)
                        .ThenInclude(e => e.User);
            }

            var board = await data.SingleOrDefaultAsync(b => b.Id.Equals(boardId));

            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

            if (board.UserId.Equals(GetUserId()))
            {
                BoardRepository.GetContext().Entry(board)
                    .Collection(b => b.Users)
                    .Query()
                    .Include(ub => ub.User)
                    .Load();
            }

            bool userIsPartOfBoard = board.Users.Any(ub => ub.UserId.Equals(GetUserId()));

            if (board.UserId.Equals(GetUserId()) || userIsPartOfBoard || User.IsInRole("Administrator"))
            {
                return Ok(
                    _mapper.Map<BoardViewModel>(board)
                );
            }

            return Forbid();
        }

        [Authorize]
        [HttpGet("{boardId}/elements")]
        public async Task<IActionResult> GetBoardElements([FromRoute] Guid boardId)
        {
            var board = await BoardRepository
                .GetAll()
                .Include(b => b.Users)
                .Include(b => b.Elements)
                    .ThenInclude(e => e.User)
                .AsNoTracking()
                .SingleOrDefaultAsync(b => b.Id.Equals(boardId));

            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

            bool userIsPartOfBoard = board.Users.Any(ub => ub.UserId.Equals(GetUserId()));

            if (board.UserId.Equals(GetUserId()) || userIsPartOfBoard || User.IsInRole("Administrator"))
            {
                return Ok(
                    _mapper.Map<List<BoardElementViewModel>>(board.Elements.OrderByDescending(e => e.ElementNumber))
                );
            }

            return Forbid();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] CreateBoardMutateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var board = _mapper.Map<Board>(model);

            board.UserId = GetUserId().Value;

            BoardRepository.Add(board);
            await BoardRepository.SaveChangesAsync();

            board = await BoardRepository.GetAll().AsNoTracking().Include(x => x.Owner).Where(x => x.Id == board.Id).FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetBoard), _mapper.Map<BoardViewModel>(board));
        }

        [Authorize]
        [HttpPut("{boardId}")]
        public async Task<IActionResult> EditBoard([FromRoute] Guid boardId, [FromBody] EditBoardMutateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var board = await BoardRepository.GetAll().SingleOrDefaultAsync(b => b.Id.Equals(boardId));
            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

            board.Name = model.Name.Trim();
            await BoardRepository.SaveChangesAsync();

            return Ok(
                _mapper.Map<BoardViewModel>(board)    
            );
        }

        [Authorize]
        [HttpDelete("{boardId}")]
        public async Task<IActionResult> RemoveBoard([FromRoute] Guid boardId)
        {
            var board = await BoardRepository.GetAll().AsNoTracking().SingleOrDefaultAsync(b => b.Id.Equals(boardId));
            if (board == null)
                return BadRequest($"No board with ID {boardId} found.");

            BoardRepository.Remove(board);
            await BoardRepository.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPost("{boardId}/users")]
        public async Task<IActionResult> AddUser([FromRoute] Guid boardId, [FromBody] AddBoardUserMutateModel model)
        {
            var board = await BoardRepository
                .GetAll()
                .Include(b => b.Users)
                    .ThenInclude(ub => ub.User)
                .SingleOrDefaultAsync(b => b.Id.Equals(boardId));
            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

            var user = await appUserManager.FindByNameAsync(model.UserName);
            if (user == null)
                return BadRequest($"No user found with the name {model.UserName}");

            if (board.Users.Any(item => item.User.UserName.Equals(user.UserName)))
                return BadRequest($"The given user is already part of this board");

            if (model.UserName.Equals(GetUsername()))
                return BadRequest($"The given user is already part of this board");

            board.Users.Add(new UserBoard
            {
                Board = board,
                User = user,
                CreatedAt = DateTimeOffset.Now
            });

            await BoardRepository.SaveChangesAsync();

            var emailModel = new UserAddedToBoardEmailViewModel
            {
                AddedBy = GetUsername(),
                UserName = user.UserName,
                Email = user.Email,
                BoardId = board.Id.ToString(),
                BoardName = board.Name
            };

            _backgroundJobs.Enqueue(() => SendUserAddedToBoardEmail(emailModel));

            return Ok(
                _mapper.Map<List<BoardUserViewModel>>(board.Users)    
            );
        }

        [Authorize]
        [HttpDelete("{boardId}/users/{userId}")]
        public async Task<IActionResult> RemoveUser([FromRoute] Guid boardId, [FromRoute] Guid userId)
        {
            var board = await BoardRepository
                .GetAll()
                .Include(b => b.Users)
                    .ThenInclude(ub => ub.User)
                .SingleOrDefaultAsync(b => b.Id.Equals(boardId));
            if (board == null)
                return BadRequest($"No board with ID {boardId} found.");

            var user = board.Users
                .Where(bu => bu.BoardId.Equals(boardId))
                .Where(bu => bu.UserId.Equals(userId))
                .FirstOrDefault();

            if (user == null)
                return BadRequest($"User not found");

            board.Users.Remove(user);
            await BoardRepository.SaveChangesAsync();

            return Ok(
                _mapper.Map<List<BoardUserViewModel>>(board.Users)
            );
        }

        [NonAction]
        public async Task SendUserAddedToBoardEmail(UserAddedToBoardEmailViewModel emailModel)
        {
            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["You have been added to the '{0}' board", emailModel.BoardName], emailModel, "Email/UserAddedToBoard");
        }
    }
}