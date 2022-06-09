using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculations
{
    public class Volumes
    {
        public decimal? TheoreticalVol(decimal? BitDepthNew, decimal? BitDepthBefore, decimal Displacement)
        {
            if (BitDepthNew != null && BitDepthBefore != null)
                return (BitDepthNew - BitDepthBefore) * (Displacement / 1000);
            return null;
        }
        public decimal? ActualVolume(decimal? VolumeNew, decimal? VolumeBefore, decimal? EmptyFill)
        {
            if (VolumeNew != null && VolumeBefore != null && EmptyFill != null)
                return VolumeNew - VolumeBefore - EmptyFill;
            return null;
        }
        public decimal? Subtract(decimal? a, decimal? b)
        {
            if (a != null && b != null) return a - b;
            return null;
        }
        public decimal? Addition(decimal? a, decimal? b)
        {
            if (a != null && b != null) return a + b;
            return null;
        }
        public decimal? GainLossTime(decimal? VolumeDiff, int? TimeDiff)
        {
            if (VolumeDiff != null && TimeDiff != null) return (VolumeDiff / TimeDiff) * 60 * 60;
            return null;
        }
    }
}
