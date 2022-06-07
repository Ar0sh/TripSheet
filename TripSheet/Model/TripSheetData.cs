using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripSheet_SQLite.Model
{

    // Table for the Tripsheet data.
    [Table("TripSheetData")]
    public partial class TripSheetData
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public string Id { get; set; }

        public string SheetId { get; set; }

        public string PipeId { get; set; }

        public long Time { get; set; }

        public decimal? TripVolume { get; set; }

        public decimal? EmptyFill { get; set; }

        public decimal? BDepth { get; set; }

        public decimal? Displacement_CE { get; set; }
        public decimal? Displacement_OE { get; set; }

        public decimal? ActualVolume { get; set; }

        public decimal? TheoreticalVol_CE { get; set; }

        public decimal? Diff_CE { get; set; }

        public decimal? TotDiff_CE { get; set; }

        public decimal? TheoreticalVol_OE { get; set; }

        public decimal? Diff_OE { get; set; }

        public decimal? TotDiff_OE { get; set; }

        public int? TimeDiffMin { get; set; }

        public decimal? LossGainRate_OE { get; set; }
        public decimal? LossGainRate_CE { get; set; }
    }
}
