using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Account
{
    public class BoardElementUserViewModel
    {
        /// <summary>
        /// The user's unique identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The user's username
        /// </summary>
        public string UserName { get; set; }
    }
}
