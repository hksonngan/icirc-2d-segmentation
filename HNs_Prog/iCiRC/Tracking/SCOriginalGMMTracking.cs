using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class SCOriginalGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        SpatialGaussianModel[] BackSpatialComponent;
        SpatialGaussianModel[] ForeSpatialComponent;

        int XNum, YNum, FrameNum;
        ushort[] FrameIntensity;
        byte[] FrameMask;

        //---------------------------------------------------------------------------
        /** @brief Run a vessel tracking algorithm for a X-ray image sequence
            @authur Hyunna Lee
            @date 2013.11.05
            @param paraXNum : the width of each frame
            @param paraYNum : the height of each frame
            @param paraZNum : the number of frames
            @param paraImageIntensity : the array of image intensity
            @return the array of labeling mask
            @todo To implement the whole algorithm
        */
        //-------------------------------------------------------------------------
        public override byte[] RunTracking(int paraXNum, int paraYNum, int paraZNum, ushort[] paraImageIntensity)
        {
            if (paraImageIntensity == null || paraXNum <= 0 || paraYNum <= 0 || paraZNum <= 0)
                return null;

            XNum = paraXNum;
            YNum = paraYNum;
            FrameNum = paraZNum;
            FrameIntensity = paraImageIntensity;

            // Result buffer initialization
            int FramePixelNum = XNum * YNum;
            int TotalPixelNum = FramePixelNum * FrameNum;
            FrameMask = new byte[TotalPixelNum];
            FrameMask.Initialize();

            // For the first frame
            InitializeGMMModel();

            // For each frame 
            for (int f = 1; f < FrameNum; f++)
            {

            }

            return FrameMask;
        }

        private void InitializeGMMModel()
        {
            int FramePixelNum = XNum * YNum;

            // Initial segmentation using thresholding
            const ushort VesselIntensityThresholdValue = 128;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameIntensity[i] < VesselIntensityThresholdValue)
                    FrameMask[i] = 0xff;
            }
        }
    }
}
