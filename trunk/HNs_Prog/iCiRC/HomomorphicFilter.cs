using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class HomomorphicFilter : FrameProcessing
    {
        public enum HighpassFilterType { Average, Gaussian, Butterworth, Chebychev };
        public HighpassFilterType HFilterType;

        public HomomorphicFilter()
        {
            HFilterType = HighpassFilterType.Average;
        }

        public HomomorphicFilter(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
            HFilterType = HighpassFilterType.Average;
        }

        public ushort[] RunFiltering(ushort[] Intensity)
        {
            int FramePixelNum = XNum * YNum;
            InputFrameIntensity = Intensity;
            OutputFrameIntensity = new ushort[FramePixelNum];
            OutputFrameIntensity.Initialize();

            // Take log
            double[] LogIntensity = new double[FramePixelNum];
            LogIntensity.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
                LogIntensity[i] = Math.Log(Convert.ToDouble(Intensity[i]));

            // High-pass filtering
            double[] FilteredLogIntensity = new double[FramePixelNum];
            FilteredLogIntensity.Initialize();
            Filters AverageFilter = new Filters();
            if (HFilterType == HighpassFilterType.Average)
                AverageFilter.GenerateAverageFilter2D(3);
            else if (HFilterType == HighpassFilterType.Gaussian)
                AverageFilter.GenerateGaussianFilter2D(1.0, 7);
            for (int i = 0; i < FramePixelNum; i++)
                FilteredLogIntensity[i] = AverageFilter.Run2D(XNum, YNum, LogIntensity, i);

            // Take exp
            for (int i = 0; i < FramePixelNum; i++)
                OutputFrameIntensity[i] = Convert.ToUInt16(Math.Exp(FilteredLogIntensity[i]));

            return OutputFrameIntensity;
        }
    }
}
