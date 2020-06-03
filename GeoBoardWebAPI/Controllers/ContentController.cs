using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GeoBoardWebAPI.Controllers
{
    [Route("content")]
    public class ContentController : BaseController
    {
        public IConfiguration Configuration { get; }
        private readonly ConnectionMapping ConnectionMapping;
        private readonly BoardElementRepository BoardElementRepository;

        public ContentController(
            IServiceProvider scopeFactory,
            IConfiguration configuration,
            ConnectionMapping connectionMapping,
            BoardElementRepository boardElementRepository
            ) : base(scopeFactory)
        {
            Configuration = configuration;
            ConnectionMapping = connectionMapping;
            BoardElementRepository = boardElementRepository;
        }

        [AllowAnonymous]
        [HttpGet("{imageId}")]
        public async Task<IActionResult> Index([FromRoute] Guid imageId)
        {
            //var userId = GetUserId().ToString();
            //var boardId = ConnectionMapping.GetUserBoard(userId);

            //if (boardId == null) return NotFound();

            //if(Guid.Parse(boardId) != BoardElementRepository.GetAll().Where(x => x.ImageId == imageId).FirstOrDefault().BoardId)
            //{
            //    return NotFound();
            //}

            var path = Configuration.GetSection("ImageStoragePath").Value + "/" + imageId + ".jpg";
            var image = System.IO.File.OpenRead(path);
            return File(image, "image/jpeg");
        }
    }
}