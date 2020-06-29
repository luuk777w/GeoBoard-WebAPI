using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Account
{
    public class AuthenticationResultViewModel
    {
        public string Token { get; set; }

        public Guid RefreshToken { get; set; }
    }
}
