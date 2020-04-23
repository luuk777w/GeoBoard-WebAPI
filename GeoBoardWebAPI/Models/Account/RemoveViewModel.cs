using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class RemoveViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }
    }
}
