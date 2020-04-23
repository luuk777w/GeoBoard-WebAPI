using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.Models.Country;

namespace GeoBoardWebAPI.Models.UserSettings
{
    public class UserSettingsViewModel
    {
        public DateTimeOffset CreationDateTime { get; set; }

        public CountryViewModel Language { get; set; }
    }
}
