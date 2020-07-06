using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;

namespace GeoBoardWebAPI.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IConfiguration _configuration;

        public HomeController(
            IServiceProvider scopeFactory,
            IConfiguration config
        ) : base(scopeFactory)
        {
            _configuration = config;
        }
        
        [AllowAnonymous]
        [HttpGet("/")]
        public IActionResult ApiRoot()
        {
            return Ok(new
            {
                Name = _configuration["Info:Name"],
                Version = _configuration["Info:Version"]
            });
        }
    }
}