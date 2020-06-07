using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board
{
    public class BoardViewModel
    {
        /// <summary>
        /// The unique identifier of this board.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of this board.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The user who created this board.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The user who created this board.
        /// </summary>
        public UserViewModel Owner { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Whether or not this board is locked for editing.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// The collection of elements on this board.
        /// </summary>
        public ICollection<BoardElementViewModel> Elements { get; set; }

        /// <summary>
        /// The collection of users who are part of this board.
        /// </summary>
        public ICollection<BoardUserViewModel> Users { get; set; }
    }
}
