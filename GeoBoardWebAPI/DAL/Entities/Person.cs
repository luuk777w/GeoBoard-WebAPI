using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoBoardWebAPI.DAL.Entities
{
    public class Person : IAppEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public virtual Country Country { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Insertions { get; set; }
        public string Initials { get; set; }
    }
}