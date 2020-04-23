using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models
{
    public class RenewTokenViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Token { get; set; }

        [DataType(DataType.DateTime)]
        [AppRequired]
        public DateTime CreationDate { get; set; }
    }
}