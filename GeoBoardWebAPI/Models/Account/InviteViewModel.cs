using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class InviteViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Firstname { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Lastname { get; set; }

        [DataType(DataType.Url)]
        [AppRequired]
        public string ReturnUrl { get; set; }
    }
}