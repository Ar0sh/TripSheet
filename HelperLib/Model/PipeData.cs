using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLib.Model
{
    // Table for the pipe data.
    [Table("PipeData")]
    public partial class PipeData
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
