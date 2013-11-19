using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC.Tracking
{
    public class VesselTracking
    {
        protected int XNum, YNum, FrameNum;
        protected ushort[] FrameIntensity;
        protected byte[] FrameMask;

        public VesselTracking()
        {
        }

        //---------------------------------------------------------------------------
        /** @brief Run a vessel tracking algorithm for a X-ray image sequence
            @author Hyunna Lee
            @date 2013.11.05
            @param paraXNum : the width of each frame
            @param paraYNum : the height of each frame
            @param paraZNum : the number of frames
            @param paraImageIntensity : the array of image intensity
            @return the array of labeling mask
        */
        //-------------------------------------------------------------------------
        public virtual byte[] RunTracking(int paraXNum, int paraYNum, int paraZNum, ushort[] paraImageIntensity)
        {
            return null;
        }
    }
}
