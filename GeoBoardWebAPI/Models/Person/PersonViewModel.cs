using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.Attributes;
using GeoBoardWebAPI.Models.Country;

namespace GeoBoardWebAPI.Models.Person
{
    public class PersonViewModel
    {
        [Orderable, Searchable]
        public string Firstname { get; set; }

        [Orderable, Searchable]
        public string Lastname { get; set; }

        [Orderable, Searchable]
        public string Initials { get; set; }

        public CountryViewModel Country { get; set; }

        [Orderable]
        public Guid? CountryId { get; set; }
    }
}
