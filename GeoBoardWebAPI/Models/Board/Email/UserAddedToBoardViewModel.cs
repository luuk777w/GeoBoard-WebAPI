using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board.Email
{
    public class BoardMembershipChangedViewModel
    {
        /// <summary>
        /// The ID of the board.
        /// </summary>
        public Guid BoardId { get; set; }

        /// <summary>
        /// The name of the board.
        /// </summary>
        public string BoardName { get; set; }

        /// <summary>
        /// The name of the user who performed this mutation.
        /// </summary>
        public string MutatedBy { get; set; }
    }
}
