using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class RequestPasswordResetViewModel
    {
        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        [AppRequired]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Token { get; set; }
    }
}