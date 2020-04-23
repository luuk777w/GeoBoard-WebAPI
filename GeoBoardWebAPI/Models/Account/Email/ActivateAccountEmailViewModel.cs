using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class ActivateAccountEmailViewModel
    {
        [AppRequired]
        public string Token { get; set; }

        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        [AppRequired]
        public string Username { get; set; }

        [AppRequired]
        public DateTime ValidTill { get; set; }
    }
}