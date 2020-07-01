using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board.Email
{
    public class UserAddedToBoardEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string AddedBy { get; set; }

        [Required]
        public string BoardId { get; set; }

        [Required]
        public string BoardName { get; set; }
    }
}
