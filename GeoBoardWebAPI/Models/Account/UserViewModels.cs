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
        public string Id { get; set; }

        [AppRequired]
        [Orderable, Searchable]
        public string Username { get; set; }

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
