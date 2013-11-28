using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class FrameProcessing
    {
        protected int XNum, YNum;
        protected byte[] InputFrameIntensity;
        public byte[] OutputFrameIntensity;
        protected byte[] InputFrameMask;
        public byte[] OutputFrameMask;

        public FrameProcessing()
        {
        }

        public FrameProcessing(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
        }
    }
}
