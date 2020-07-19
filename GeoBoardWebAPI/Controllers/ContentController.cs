using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Hubs;
using GeoBoardWebAPI.Models.Content;
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
        public IActionResult Index([FromRoute] Guid imageId)
        {
            var storagePathSection = Configuration.GetSection("ImageStoragePath");
            if (storagePathSection == null)
                return BadRequest("Something went wrong while fetchhing the image.");

            string path = $"{storagePathSection.Value}/{imageId}.jpg";

            try
            {
                FileStream image = System.IO.File.OpenRead(path);

                return File(image, MediaTypeNames.Image.Jpeg);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"The file '{imageId}' could not be found.");
            }
            catch (Exception)
            {
                return Problem($"Something went wrong while fetching the requested content ({imageId}).", 500);
            }
        }

        [AllowAnonymous]
        [HttpGet("static/backgrounds")]
        public IActionResult Backgrounds([FromRoute] string name)
        {
            var storagePathSection = Configuration.GetSection("StaticStoragePath");
            if (storagePathSection == null)
                return BadRequest("Something went wrong while fetchhing the image.");

            string path = $"{storagePathSection.Value}/backgrounds";

            try
            {
                List<FileViewModel> backgroundPaths = new List<FileViewModel>();

                foreach (string background in Directory.EnumerateFiles(path, "*.jpg"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(background);

                    backgroundPaths.Add(new FileViewModel
                    {
                        Name = fileName,
                        Path = Url.Action(nameof(Background), new { Name = fileName })
                    });
                }

                return Ok(backgroundPaths);
            }
            catch (DirectoryNotFoundException)
            {
                return NotFound($"Could not fetch background files.");
            }
            catch (Exception)
            {
                return Problem($"Something went wrong while fetching the backgrounds", 500);
            }
        }

        [AllowAnonymous]
        [HttpGet("static/backgrounds/{name}")]
        public IActionResult Background([FromRoute] string name)
        {
            var storagePathSection = Configuration.GetSection("StaticStoragePath");
            if (storagePathSection == null)
                return BadRequest("Something went wrong while fetchhing the image.");

            string path = $"{storagePathSection.Value}/backgrounds/{name}.jpg";

            try
            {
                FileStream image = System.IO.File.OpenRead(path);

                return File(image, MediaTypeNames.Image.Jpeg);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"Background '{name}' could not be found.");
            }
            catch (Exception)
            {
                return Problem($"Something went wrong while fetching the requested background ({name}).", 500);
            }
        }
    }
}