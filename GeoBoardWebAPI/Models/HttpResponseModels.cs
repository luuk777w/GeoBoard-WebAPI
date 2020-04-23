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

    public class BadRequestHttpResponseModel
    {
        public IEnumerable<string> Errors { get; set; }

        public BadRequestHttpResponseModel()
        {
            Errors = new List<string>();
        }

        public BadRequestHttpResponseModel(string error)
        {
            Errors = new List<string>()
            {
                error
            };
        }

        public BadRequestHttpResponseModel(IEnumerable<string> errors)
        {
            Errors = errors;
        }

        public BadRequestHttpResponseModel(LocalizedString error)
        {
            Errors = new List<string>()
            {
                error.Value
            };
        }

        public BadRequestHttpResponseModel(IEnumerable<LocalizedString> errors)
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
