using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// The connection between a <see cref="User"/> and a <see cref="Snapshot"/>.
    /// </summary>
    public class UserSnapshot : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this pivot element.
        /// Does not act as primary key.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The user who made this snapshot <see cref="Snapshot"/>.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The user who made this snapshot <see cref="Snapshot"/>.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// The snapshot that the <see cref="User"/> owns.
        /// </summary>
        [Required]
        public Guid SnapshotId { get; set; }

        /// <summary>
        /// The snapshot that the <see cref="User"/> owns.
        /// </summary>
        public Snapshot Snapshot { get; set; }

        /// <summary>
        /// The creation date and time of when the user joined <see cref="Snapshot"/>.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
