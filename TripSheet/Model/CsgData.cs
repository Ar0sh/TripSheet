﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripSheet_SQLite.Model
{
    // Table for the casing data.
    [Table("CsgData")]
    public partial class CsgData
    {
        [Key]
        public string Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public decimal? CEDisplacement { get; set; }

        public decimal? OEDisplacement { get; set; }

        [StringLength(50)]
        public string Details { get; set; }

    }
}
