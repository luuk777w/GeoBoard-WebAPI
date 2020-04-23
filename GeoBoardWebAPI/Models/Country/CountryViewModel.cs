using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoBoardWebAPI.Attributes;

namespace GeoBoardWebAPI.Models.Country
{
    public class CountryViewModel
    {
        public DateTimeOffset CreationDateTime { get; set; }

        [Searchable, Orderable, Filterable]
        public string ShortCode { get; set; }

        [Searchable, Orderable, Filterable]
        public string LongCode { get; set; }

        [Searchable, Orderable, Filterable]
        public string LanguageCode { get; set; }

        public int ISOCode { get; set; }

        [Searchable, Orderable, Filterable]
        public string LongTerm { get; set; }

        [Searchable, Orderable, Filterable]
        public string ShortTerm { get; set; }

        [Searchable, Orderable, Filterable]
        public string Capital { get; set; }
    }
}
