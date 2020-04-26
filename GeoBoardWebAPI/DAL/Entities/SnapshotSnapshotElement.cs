using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// The connection between a <see cref="User"/> and a <see cref="Board"/>.
    /// </summary>
    public class SnapshotSnapshotElement : IAppEntity
    {
        /// <summary>
        /// The unique identifier of this pivot element.
        /// Does not act as primary key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        public Guid SnapshotId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Snapshot Snapshot { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        public Guid SnapshotElementId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ForeignKey(nameof(SnapshotElementId))]
        public SnapshotElement Element { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
