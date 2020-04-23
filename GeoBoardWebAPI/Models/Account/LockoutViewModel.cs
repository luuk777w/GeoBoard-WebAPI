using System.ComponentModel.DataAnnotations;

namespace GeoBoardWebAPI.Models.Account
{
    public class LockoutViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public bool Lockout { get; set; }
    }
}
