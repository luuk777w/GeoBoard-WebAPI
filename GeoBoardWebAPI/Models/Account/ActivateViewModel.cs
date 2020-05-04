using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class ActivateViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }

        [DataType(DataType.Url)]
        public string ReturnUrl { get; set; }
    }
}