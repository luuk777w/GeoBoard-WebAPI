using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class ResetPasswordViewModel
    {
        [AppRequired]
        public string Username { get; set; }

        [DataType(DataType.Url)]
        public string ReturnUrl { get; set; }
    }

    public class ResetPasswordReturnViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
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