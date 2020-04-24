using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// A snapshot represents the copied values of a <see cref="BoardElement"/> that should be saved.
    /// </summary>
    public class SnapshotElement : IAppEntity
    {
        /// <summary>
        /// The unique identified of this snapshot element.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The snapshot where this element belongs to.
        /// </summary>
        [Required]
        public Guid SnapshotId { get; set; }

        /// <summary>
        /// The snapshot where this element belongs to.
        /// </summary>
        public Snapshot Snapshot { get; set; }

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
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The user who created this element.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
