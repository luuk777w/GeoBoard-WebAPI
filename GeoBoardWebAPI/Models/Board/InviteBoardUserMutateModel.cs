using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board
{
    public class AddBoardUserMutateModel
    {
        /// <summary>
        /// The username to invite.
        /// </summary>
        [Required]
        public string UserName { get; set; }
    }
}
