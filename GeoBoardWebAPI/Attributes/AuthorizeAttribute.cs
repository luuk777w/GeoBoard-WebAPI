using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ClaimMatchOneAuthorizeAttribute : Attribute
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public ClaimMatchOneAuthorizeAttribute(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
