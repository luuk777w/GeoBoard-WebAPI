using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Hubs;
using GeoBoardWebAPI.Models.Board;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GeoBoardWebAPI.Controllers
{
    [Route("boards/elements")]
    public class BoardElementController : BaseController
    {
        private readonly ConnectionMapping ConnectionMapping;
        private readonly BoardElementRepository BoardElementRepository;
        private readonly IHubContext<BoardHub> _hubContext;

        public IConfiguration Configuration { get; }

        public BoardElementController(
            IServiceProvider scopeFactory,
            IConfiguration configuration,
            ConnectionMapping connectionMapping,
            BoardElementRepository boardElementRepository,
            IHubContext<BoardHub> hubContext
            ) : base(scopeFactory)
        {
            Configuration = configuration;
            ConnectionMapping = connectionMapping;
            BoardElementRepository = boardElementRepository;
            _hubContext = hubContext;
        }

        [Authorize]
        [HttpPost("uploadImage")]
        public async Task<IActionResult> UploadImage([FromBody] CreateBoardElementViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId().ToString();
            var boardId = ConnectionMapping.GetUserBoard(userId);
            var ImageId = Guid.NewGuid();

            string filePath = Configuration.GetSection("ImageStoragePath").Value + "/" + ImageId + ".jpg";
            await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(model.Image));

            var boardElement = await BoardElementRepository.GetAll().Where(x => x.Id == model.BoardElementId).FirstOrDefaultAsync();
            boardElement.ImageId = ImageId;

            BoardElementRepository.Update(boardElement);
            await BoardElementRepository.SaveChangesAsync();

            await _hubContext.Clients.Group(boardId).SendAsync("ReceiveImage", boardElement);

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBoardElement([FromBody] CreateBoardElementViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId().ToString();
            var boardId = ConnectionMapping.GetUserBoard(userId);

            if (boardId == null) return BadRequest();

            var number = 1;

            if (BoardElementRepository.GetAll().Where(x => x.BoardId == Guid.Parse(boardId)).Count() > 0)
            {
                number = BoardElementRepository.GetAll()
                    .Where(x => x.BoardId == Guid.Parse(boardId))
                    .OrderBy(x => x.ElementNumber)
                    .Last().ElementNumber + 1;
            }

            var boardElementId = Guid.NewGuid();

            BoardElementRepository.Add(new BoardElement
            {
                Id = boardElementId,
                BoardId = Guid.Parse(boardId),
                ElementNumber = number,
                Note = model.Note,
                UserId = GetUserId(),
                CreatedAt = DateTimeOffset.Now
            });
            await BoardElementRepository.SaveChangesAsync();

            var boardElement = BoardElementRepository.GetAll()
                .Include(x => x.User)
                .Where(x => x.Id == boardElementId)
                .FirstOrDefault();

            await _hubContext.Clients.Group(boardId).SendAsync("ReceiveElement", boardElement);

            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveBoardElement([FromRoute] Guid id)
        {
            var element = await BoardElementRepository.GetAll().AsNoTracking().SingleOrDefaultAsync(b => b.Id.Equals(id));
            if (element == null)
                return NotFound($"No board with ID {id} found.");

            var userId = GetUserId().ToString();
            var boardId = ConnectionMapping.GetUserBoard(userId);

            BoardElementRepository.Remove(element);
            await BoardElementRepository.SaveChangesAsync();

            await _hubContext.Clients.Group(boardId).SendAsync("RemoveElement", id);

            return NoContent();
        }

    }
}