﻿using System;
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
using Microsoft.Extensions.Configuration;

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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllBoards()
        {
            var boards = BoardRepository
                .GetAll()
                .AsNoTracking();

            if (! User.IsInRole("Administrator"))
            {
                boards = boards.Where(b => b.UserId == GetUserId());
            }

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

            return Ok(_mapper.Map<BoardViewModel>(board));
        }
    }
}