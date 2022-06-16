using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLib.Model
{
    // Database model for Tripsheet application.
    // Description of database tables and precision of column values.
    public class TripSheetModel : DbContext
    {
        public TripSheetModel()
            : base(new SQLiteConnection()
            {
                ConnectionString = new SQLiteConnectionStringBuilder()
                {
                    DataSource = "Database\\TripSheet.sqlite",
                    //ForeignKeys = true
                }.ConnectionString
            }, true)
        {

        }

        public virtual DbSet<TripSheetData> TripSheetData { get; set; }
        public virtual DbSet<TripSheetDetail> TripSheetDetail { get; set; }
        public virtual DbSet<PipeData> PipeData { get; set; }
        public virtual DbSet<CsgData> CsgData { get; set; }

        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.TripVolume)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.EmptyFill)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.BDepth)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.Displacement_CE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.Displacement_OE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.ActualVolume)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.TheoreticalVol_CE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.Diff_CE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.TotDiff_CE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.TheoreticalVol_OE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.Diff_OE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.TotDiff_OE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.LossGainRate_OE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<TripSheetData>()
        //        .Property(e => e.LossGainRate_CE)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<PipeData>()
        //        .Property(e => e.OEDisplacement)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<PipeData>()
        //        .Property(e => e.CEDisplacement)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<CsgData>()
        //        .Property(e => e.OEDisplacement)
        //        .HasPrecision(8, 2);

        //    modelBuilder.Entity<CsgData>()
        //        .Property(e => e.CEDisplacement)
        //        .HasPrecision(8, 2);
        //}
    }
}
