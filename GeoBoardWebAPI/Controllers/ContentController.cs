using System;
using System.Collections.Generic;
using System.IO;
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
            string path = Configuration.GetSection("ImageStoragePath").Value + "/" + imageId + ".jpg";
            FileStream image = System.IO.File.OpenRead(path);

            return File(image, "image/jpeg");
        }
    }
}