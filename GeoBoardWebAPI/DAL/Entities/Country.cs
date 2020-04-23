using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.DAL.Entities
{
    public class Country : IAppEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public DateTimeOffset CreationDateTime { get; set; }

        [Required]
        public string ShortCode { get; set; }

        [Required]
        public string LongCode { get; set; }

        [Required]
        public string LanguageCode { get; set; }

        [Required]
        public int ISOCode { get; set; }

        public string LongTerm { get; set; }

        [Required]
        public string ShortTerm { get; set; }

        public string Capital { get; set; }
    }
}
