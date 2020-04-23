using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class RegisterViewModel
    {
        [AppRequired]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [AppRequired]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [AppRequired]
        [DataType(DataType.Text)]
        public string Username { get; set; }

        public string Token { get; set; }
    }
}
