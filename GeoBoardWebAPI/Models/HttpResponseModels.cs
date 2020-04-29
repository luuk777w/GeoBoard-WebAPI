using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models
{
    public class CollectionHttpResponseModel<T>
    {
        public int Page { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalCount { get; set; }
        public int ResultCount { get; set; }
        public List<OrderByHttpRequestModel> OrderBy { get; set; }
        public List<FilterHttpRequestModel> Filter { get; set; }
        public string Search { get; set; }
        public IEnumerable<T> Items { get; set; }
    }

    public class BadRequestHttpResponseModelx
    {
        public IEnumerable<string> Errors { get; set; }

        public BadRequestHttpResponseModelx()
        {
            Errors = new List<string>();
        }

        public BadRequestHttpResponseModelx(string error)
        {
            Errors = new List<string>()
            {
                error
            };
        }

        public BadRequestHttpResponseModelx(IEnumerable<string> errors)
        {
            Errors = errors;
        }

        public BadRequestHttpResponseModelx(LocalizedString error)
        {
            Errors = new List<string>()
            {
                error.Value
            };
        }

        public BadRequestHttpResponseModelx(IEnumerable<LocalizedString> errors)
        {
            var errorList = new List<string>();

            foreach (var error in errors)
            {
                errorList.Add(error);
            }

            Errors = errorList;
        }
    }
}
