using System.Collections.Generic;

namespace GeoBoardWebAPI.Models
{
    public class OrderByHttpRequestModel
    {
        public string Key { get; set; }
        public string Direction { get; set; }

        public OrderByHttpRequestModel()
        {
            Direction = "ASC";
        }
    }

    public class FilterHttpRequestModel
    {
        public string Key { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }

        public List<FilterHttpRequestModel> Ors { get; set; }
        public List<FilterHttpRequestModel> Ands { get; set; }

        public FilterHttpRequestModel()
        {
            Ors = new List<FilterHttpRequestModel>();
            Ands = new List<FilterHttpRequestModel>();
        }
    }
}
