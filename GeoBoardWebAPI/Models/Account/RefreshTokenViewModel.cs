using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class RefreshTokenViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}