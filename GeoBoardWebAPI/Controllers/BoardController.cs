using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Extensions.Authorization;
using GeoBoardWebAPI.Models.Board;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoBoardWebAPI.Controllers
{
    public class BoardController : BaseController
    {
        private readonly BoardRepository BoardRepository;
        private readonly AppUserManager appUserManager;

        public BoardController(
            IServiceProvider scopeFactory,
            BoardRepository boardRepository,
            AppUserManager appUserManager
        ) : base(scopeFactory)
        {
            BoardRepository = boardRepository;
            this.appUserManager = appUserManager;
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateBoard([FromBody] CreateBoardMutateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var board = _mapper.Map<Board>(model);

            board.UserId = (await this.appUserManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier))).Id;

            BoardRepository.Add(board);
            await BoardRepository.SaveChangesAsync();

            return Ok(_mapper.Map<BoardViewModel>(board));
        }
    }
}