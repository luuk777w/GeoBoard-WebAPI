using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class RefreshTokenViewModel
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public Guid RefreshToken { get; set; }
    }
}