using GeoBoardWebAPI.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Hubs
{
    public class ConnectionMapping
    {
        /// <summary>
        /// The collection of users with 1 assigned board.
        /// </summary>
        private IDictionary<string, string> UserBoard = new Dictionary<string, string>();

        /// <summary>
        /// The collection of user ID's and usernames who are registered as a viewer.
        /// </summary>
        private IDictionary<string, string> Users = new Dictionary<string, string>();

        /// <summary>
        /// Assigns the given board to the given user.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the given board should be assigned.</param>
        /// <param name="username">The username of the user whom the given board should be assigned.</param>
        /// <param name="boardId">The ID of the board to assign to the given user.</param>
        public void SetUserBoard(string userId, string username, string boardId)
        {
            this.Users[userId] = username;
            this.UserBoard[userId] = boardId;
        }

        /// <summary>
        /// Get the ID of the board that the given user is viewing (if any).
        /// </summary>
        /// <param name="userId">The user to get the the viewing board from.</param>
        public string GetUserBoard(string userId)
        {
            if (!this.UserBoard.ContainsKey(userId))
                return null;

            return this.UserBoard[userId];
        }

        /// <summary>
        /// Get a list of users who are viewing the given board.
        /// </summary>
        /// <param name="boardId">The board Id to get users for.</param>
        public IEnumerable<JoinedBoardUserViewModel> GetJoinedBoardUsers(string boardId)
        {
            return UserBoard
                .Where(ub => ub.Value.Equals(boardId)).Select(ub => new JoinedBoardUserViewModel
                {
                    Id = ub.Key,
                    Username = this.Users[ub.Key]
                }
            );
        }

        /// <summary>
        /// Remove the currently viewing board of the given user.
        /// </summary>
        /// <param name="userId">The user to remove.</param>
        public void RemoveUser(string userId)
        {
            this.Users.Remove(userId);
            this.UserBoard.Remove(userId);
        }
    }
}
