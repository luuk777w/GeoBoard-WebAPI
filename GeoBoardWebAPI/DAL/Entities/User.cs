using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using GeoBoardWebAPI.Models;

namespace GeoBoardWebAPI.DAL.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class User : IdentityUser, IAppEntity
    {
        public static readonly string[] Roles = { "Administrator", "User" };

        Guid IAppEntity.Id
        {
            get { return new Guid(Id); }
            set { this.Id = value.ToString(); }
        }
        
        public virtual Person Person { get; set; }
        public virtual UserSettings Settings { get; set; }

        public bool IsLocked { get; set; }

        public DateTimeOffset CreationDateTime { get; set; }

        public User()
        {
            CreationDateTime = DateTimeOffset.UtcNow;
        }
    }
}
