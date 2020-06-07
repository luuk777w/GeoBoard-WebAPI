using GeoBoardWebAPI.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Account
{
    public class BoardUserViewModel
    {
        /// <summary>
        /// The user's unique identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The user's username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The creation date and time of when the user joined the board.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
