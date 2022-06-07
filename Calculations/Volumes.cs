using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculations
{
    public class Volumes
    {
        public static decimal? TheoreticalVol(decimal? BitDepthNew, decimal? BitDepthBefore, decimal Displacement)
        {
            if(BitDepthNew != null && BitDepthBefore != null)
                return (BitDepthNew - BitDepthBefore) * (Displacement / 1000);
            return -9999;
        }

        public static decimal? ActualVolume(decimal? VolumeNew, decimal? VolumeBefore, decimal? EmptyFill)
        {
            if(VolumeNew != null && VolumeBefore != null && EmptyFill != null)
                return VolumeNew - VolumeBefore - EmptyFill;
            return -9999;
        }
    }
}
