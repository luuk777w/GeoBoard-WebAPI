using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// An element that is part of a <see cref="Board"/>.
    /// </summary>
    public class BoardElement : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this board.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The board where this element belongs to.
        /// </summary>
        [Required]
        public Guid BoardId { get; set; }

        /// <summary>
        /// The index number indicating the order in which this element was created within the board.
        /// </summary>
        public int ElementNumber { get; set; }

        /// <summary>
        /// The board where this element belongs to.
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// The ID of the attached image when available.
        /// </summary>
        public Guid? ImageId { get; set; }

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
        public virtual User User { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
