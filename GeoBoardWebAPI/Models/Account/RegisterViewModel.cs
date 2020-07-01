using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class RegisterViewModel
    {
        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        [AppRequired]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [AppRequired]
        [DataType(DataType.Text)]
        public string UserName { get; set; }

        public string Token { get; set; }
    }
}
