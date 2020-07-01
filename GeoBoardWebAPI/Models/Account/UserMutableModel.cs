using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class UserMutableModel
    {
        public string Id { get; set; }

        [AppRequired]
        public string UserName { get; set; }

        [AppRequired]
        [EmailAddress]
        public string Email { get; set; }

        public DateTimeOffset CreationDateTime { get; set; }

        [AppRequired]
        public Guid PersonaliaId { get; set; }

        public bool IsLocked { get; set; }
    }
}
