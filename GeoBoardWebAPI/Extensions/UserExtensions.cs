using GeoBoardWebAPI.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Extensions
{
    public static class UserExtensions
    {
        public static bool HasPermissionToSwitchBoard(this User user, Board boardToBeSwitchedTo, UserManager<User> _userManager)
        {
            bool userAlreadyInBoard = boardToBeSwitchedTo.Users.Any(ub => ub.UserId.Equals(user.Id));
            bool isAdmin = Task.Run(async () => await _userManager.IsInRoleAsync(user, "Administrator")).Result;
            bool isPartOfBoard = boardToBeSwitchedTo.UserId.Equals(user.Id);

            return isPartOfBoard || userAlreadyInBoard || isAdmin;
        }
    }
}
