using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// The connection between a <see cref="User"/> and a <see cref="Board"/>.
    /// </summary>
    public class UserBoard : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this pivot element.
        /// Does not act as primary key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The user that is part of the <see cref="Board"/>.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The user that is part of the <see cref="Board"/>.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// The board that the <see cref="User"/> belongs to.
        /// </summary>
        [Required]
        public Guid BoardId { get; set; }

        /// <summary>
        /// The board that the <see cref="User"/> belongs to.
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// The creation date and time of when the user joined <see cref="Board"/>.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
