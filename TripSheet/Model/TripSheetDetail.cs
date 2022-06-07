using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripSheet_SQLite.Model
{
    // Table for the trip sheet details.
    [Table("TripSheetDetail")]
    public partial class TripSheetDetail
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public string Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public string Details { get; set; }

        public string Wellbore { get; set; }

        public string Well { get; set; }

        public long TimeDate { get; set; }

    }
}
