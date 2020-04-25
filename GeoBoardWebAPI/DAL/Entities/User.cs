﻿using System;
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
        public virtual UserSetting Settings { get; set; }

        /// <summary>
        /// Notitie: Calculated property van maken a.d.v. de LockoutEnd? Scheelt weer een kolom ;-)
        /// </summary>
        public bool IsLocked { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The collection of boards this user has created.
        /// </summary>
        public ICollection<Board> CreatedBoards { get; set; }

        /// <summary>
        /// The collection of boards this user owns.
        /// </summary>
        public ICollection<UserBoard> Boards { get; set; }

        /// <summary>
        /// The collection of snapshots this user has created.
        /// </summary>
        public ICollection<Snapshot> CreatedSnapshots { get; set; }

        /// <summary>
        /// The collection of snapshots this user owns.
        /// </summary>
        public ICollection<UserSnapshot> Snapshots { get; set; }

        public User()
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
