using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class ResetPasswordEmailViewModel
    {
        [AppRequired]
        public string Token { get; set; }

        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        [AppRequired]
        public string ActivationUrl { get; set; }

        [AppRequired]
        public string ReturnUrl { get; set; }
    }
}