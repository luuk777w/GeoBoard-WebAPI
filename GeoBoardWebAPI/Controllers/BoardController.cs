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
using Microsoft.EntityFrameworkCore;

namespace GeoBoardWebAPI.Controllers
{
    [Route("boards")]
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
        [HttpGet]
        public async Task<IActionResult> GetAllBoard()
        {
            var boards = BoardRepository.GetAll();

            if (! User.IsInRole("Administrator"))
            {
                boards = boards.Where(b => b.UserId == GetUserId());
            }

            return Ok(
                _mapper.Map<List<BoardViewModel>>(await boards.ToListAsync())    
            );
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

            return Ok(_mapper.Map<BoardViewModel>(board));
        }
    }
}