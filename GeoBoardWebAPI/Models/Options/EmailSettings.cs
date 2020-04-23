using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Options
{
    public class EmailSettings
    {
        public string Transport { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
        public string FromDisplayName { get; set; }
        public string BounceAddress { get; set; }
    }
}
