using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class ResetPasswordEmailViewModel
    {
        [AppRequired]
        public string UserName { get; set; }

        [AppRequired]
        public string Token { get; set; }

        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }
    }
}