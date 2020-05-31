using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board
{
    public class CreateBoardElementViewModel
    {
        public string Note { get; set; }

        public string Image { get; set; }
    }
}
