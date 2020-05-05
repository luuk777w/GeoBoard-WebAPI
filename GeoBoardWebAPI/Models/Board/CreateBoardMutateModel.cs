﻿using GeoBoardWebAPI.Models.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Models.Board
{
    public class CreateBoardMutateModel
    {
        /// <summary>
        /// The name of this board.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The date and time of when this board was created.
        /// </summary>
        public DateTimeOffset CreatedAt => DateTimeOffset.Now;
    }
}
