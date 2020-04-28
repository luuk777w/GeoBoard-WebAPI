using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class LoginViewModel
    {
        [AppRequired]
        public string Username { get; set; }

        [AppRequired]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        [AppRequired]
        public bool RememberMe { get; set; }
    }
}
