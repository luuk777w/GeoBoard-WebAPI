using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.DAL.Entities
{
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Token { get; set; }

        public string JwtId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiryDate { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
