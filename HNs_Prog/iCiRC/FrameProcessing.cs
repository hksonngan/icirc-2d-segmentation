﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class FrameProcessing
    {
        protected int XNum, YNum;
        protected ushort[] InputFrameIntensity;
        protected ushort[] OutputFrameIntensity;
        protected byte[] InputFrameMask;
        protected byte[] OutputFrameMask;

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
