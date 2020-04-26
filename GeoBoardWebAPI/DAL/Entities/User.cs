using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using GeoBoardWebAPI.Models;

namespace GeoBoardWebAPI.DAL.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class User : IdentityUser<Guid>, IAppEntity
    {
        public static readonly string[] Roles = { "Administrator", "User" };
        public virtual Person Person { get; set; }
        public virtual UserSetting Settings { get; set; }

        /// <summary>
        /// Whether or not this account is (temporarily) locked.
        /// This value becomes true when the <see cref="this.LockoutEnd"/> is filled.
        /// </summary>
        public bool IsLocked => this.LockoutEnd != null;

        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The collection of boards this user owns.
        /// </summary>
        public ICollection<UserBoard> Boards { get; set; }

        /// <summary>
        /// The collection of snapshots this user owns.
        /// </summary>
        public ICollection<Snapshot> Snapshots { get; set; }

        public User()
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
