﻿using System.ComponentModel.DataAnnotations;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Account
{
    public class InviteActivateViewModel
    {
        [DataType(DataType.EmailAddress)]
        [AppRequired]
        public string Email { get; set; }

        [DataType(DataType.Text)]
        [AppRequired]
        public string Token { get; set; }

        [AppRequired]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}