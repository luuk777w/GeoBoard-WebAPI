using System;
using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class UserPropertyMutateModel
    {
        public string Id { get; set; }
        public DateTimeOffset CreationDateTime { get; set; }
    }
}
