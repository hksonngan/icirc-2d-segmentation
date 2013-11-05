using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class SCOriginalGMMTracking : VesselTracking
    {
        public override byte[] RunTracking(int XNum, int YNum, int ZNum, ushort[] ImageIntensity)
        {
            if (ImageIntensity == null || XNum <= 0 || YNum <= 0 || ZNum <= 0)
                return null;

            // Result buffer initialization
            int SlicePixelNum = XNum * YNum;
            int TotalPixelNum = SlicePixelNum * ZNum;
            byte[] ResultMask = new byte[TotalPixelNum];
            ResultMask.Initialize();

            // For each frame
            for (int z = 0; z < ZNum; z++)
            {

            }

            return ResultMask;
        }
    }
}
