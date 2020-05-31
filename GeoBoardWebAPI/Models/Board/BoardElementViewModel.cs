using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.Models.Account;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board
{
    public class BoardElementViewModel
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The board where this element belongs to.
        /// </summary>
        public Guid BoardId { get; set; }

        /// <summary>
        /// The path to the image when available.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// An optional note for this element.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// The optional direction where the subject is taken from.
        /// </summary>
        public Direction? Direction { get; set; }

        /// <summary>
        /// The user who created this element.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// The user who created this element.
        /// </summary>
        public BoardElementUserViewModel User { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
