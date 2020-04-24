using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// A snapshot represents a collection of <see cref="SnapshotElement"/>s that should be saved.
    /// </summary>
    public class Snapshot : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this snapshot.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The name of this snapshot.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The board of which a snapshot was taken.
        /// </summary>
        [Required]
        public Guid BoardId { get; set; }

        /// <summary>
        /// The board of which a snapshot was taken.
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// The creation date and time of which this snapshot was created.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The collection of elements on this board.
        /// </summary>
        public ICollection<SnapshotElement> Elements { get; set; }

        /// <summary>
        /// The collection of users who are part of this board.
        /// </summary>
        public ICollection<UserSnapshot> Users { get; set; }
    }
}
