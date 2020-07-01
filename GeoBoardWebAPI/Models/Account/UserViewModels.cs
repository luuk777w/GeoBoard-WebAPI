using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;
using GeoBoardWebAPI.Models.Person;
using GeoBoardWebAPI.Models.UserSettings;

namespace GeoBoardWebAPI.Models.Account
{
    public class UserViewModel
    {
        /// <summary>
        /// The unique identifier of this user.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The unique name of this user.
        /// </summary>
        [AppRequired]
        [Orderable, Searchable]
        public string UserName { get; set; }

        /// <summary>
        /// The user's email address used to login.
        /// </summary>
        [Orderable, Searchable]
        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        public DateTimeOffset CreationDateTime { get; set; }

        public ICollection<string> Roles { get; set; }

        [AppRequired]
        public bool IsLocked { get; set; }

        [Orderable]
        public bool EmailConfirmed { get; set; }

        [Orderable]
        public DateTimeOffset? LockoutEnd { get; set; }

    }
}
