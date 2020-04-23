using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class ActivateReturnViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Token { get; set; }
    }
}
