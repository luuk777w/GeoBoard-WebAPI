using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    /// <summary>
    /// Represents the personal details of a <see cref="User"/>.
    /// </summary>
    public class Person : IAppEntity
    {
        /// <summary>
        /// The identifier of these personal details.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The first name of this person.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The last name of this person.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The country in which this person lives.
        /// </summary>
        public virtual Country Country { get; set; }

        /// <summary>
        /// The data and time of when this person was created [not born ;-)].
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}