using System.Collections.Generic;

namespace GeoBoardWebAPI.Models.Account
{
    public class ProfileAccountViewModel
    {
        public UserViewModel User { get; set; }

        public ICollection<ClaimViewModel> Claims { get; set; }

        public ProfileAccountViewModel()
        {
            Claims = new List<ClaimViewModel>();
        }
    }
}
