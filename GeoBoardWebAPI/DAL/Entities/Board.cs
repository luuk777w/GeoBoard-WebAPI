using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// Represents the board that contains the different elements to share.
    /// </summary>
    public class Board : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this board.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The name of this board.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The user who created this board.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The user who created this board.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User Owner { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Whether or not this board is locked for editing.
        /// </summary>
        [Required]
        public bool IsLocked { get; set; }

        /// <summary>
        /// The collection of elements on this board.
        /// </summary>
        public ICollection<BoardElement> Elements { get; set; }
        
        /// <summary>
        /// The collection of users who are part of this board.
        /// </summary>
        public ICollection<UserBoard> Users { get; set; }
    }
}
