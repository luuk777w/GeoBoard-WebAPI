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

namespace GeoBoardWebAPI.Controllers
{
    [Route("boards")]
    public class BoardController : BaseController
    {
        private readonly BoardRepository BoardRepository;
        private readonly AppUserManager appUserManager;
        public IConfiguration Configuration { get; }



        public BoardController(
            IServiceProvider scopeFactory,
            BoardRepository boardRepository,
            AppUserManager appUserManager,
            IConfiguration configuration
        ) : base(scopeFactory)
        {
            BoardRepository = boardRepository;
            this.appUserManager = appUserManager;
            Configuration = configuration;

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
                .Include(b => b.Users)
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
                .AsQueryable()
                .AsNoTracking();

            if (includeElements)
            {
                data = data
                    .Include(b => b.Elements)
                        .ThenInclude(e => e.User);
            }

            var board = await data.SingleOrDefaultAsync(b => b.Id.Equals(boardId));

            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

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
                    _mapper.Map<List<BoardElementViewModel>>(board.Elements.OrderBy(e => e.CreatedAt))
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
        [HttpPost("{boardId}/createElement")]
        public async Task<IActionResult> CreateBoardElement([FromBody] CreateBoardElementViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dateTime = DateTime.Now;
            var dateTimeStringMili = dateTime.ToString("dd-MM-yyyyTHH.mm.ss.fff");
            var dateTimeString = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

            string filePath = Configuration.GetSection("ImageStoragePath").Value + "/" + dateTimeStringMili + ".jpg";
            System.IO.File.WriteAllBytes(filePath, Convert.FromBase64String(model.Image));

            return Ok();
        }

        [Authorize]
        [HttpDelete("{boardId}")]
        public async Task<IActionResult> RemoveBoard([FromRoute] Guid boardId)
        {
            var board = await BoardRepository.GetAll().AsNoTracking().SingleOrDefaultAsync(b => b.Id.Equals(boardId));
            if (board == null)
                return NotFound($"No board with ID {boardId} found.");

            BoardRepository.Remove(board);
            await BoardRepository.SaveChangesAsync();

            return NoContent();
        }
    }
}