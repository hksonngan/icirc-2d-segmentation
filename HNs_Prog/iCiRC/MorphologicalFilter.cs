using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class MorphologicalFilter : FrameProcessing
    {
        public enum FilterType { Median, Dilation, Erosion };
        public FilterType FType;

        public MorphologicalFilter()
        {
            FType = FilterType.Median;
        }

        public MorphologicalFilter(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
            FType = FilterType.Median;
        }

        public byte[] RunFiltering(byte[] Intensity)
        {
            InputFrameIntensity = Intensity;
            int FramePixelNum = XNum * YNum;
            OutputFrameIntensity = new byte[FramePixelNum];
            OutputFrameIntensity = (byte[])InputFrameIntensity.Clone();

            for (int y = 1; y < YNum - 1; y++)
            {
                for (int x = 1; x < XNum - 1; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    byte[] NeighborIntensity = new byte[9];
                    NeighborIntensity[0] = InputFrameIntensity[CurrentPixelIndex - XNum - 1];
                    NeighborIntensity[1] = InputFrameIntensity[CurrentPixelIndex - XNum    ];
                    NeighborIntensity[2] = InputFrameIntensity[CurrentPixelIndex - XNum + 1];
                    NeighborIntensity[3] = InputFrameIntensity[CurrentPixelIndex        - 1];
                    NeighborIntensity[4] = InputFrameIntensity[CurrentPixelIndex           ];
                    NeighborIntensity[5] = InputFrameIntensity[CurrentPixelIndex        + 1];
                    NeighborIntensity[6] = InputFrameIntensity[CurrentPixelIndex + XNum - 1];
                    NeighborIntensity[7] = InputFrameIntensity[CurrentPixelIndex + XNum    ];
                    NeighborIntensity[8] = InputFrameIntensity[CurrentPixelIndex + XNum + 1];

                    Array.Sort(NeighborIntensity);
                    if (FType == FilterType.Median)
                        OutputFrameIntensity[CurrentPixelIndex] = NeighborIntensity[4];
                    else if (FType == FilterType.Dilation)
                        OutputFrameIntensity[CurrentPixelIndex] = NeighborIntensity[8];
                    else if (FType == FilterType.Erosion)
                        OutputFrameIntensity[CurrentPixelIndex] = NeighborIntensity[0];
                }
            }

            return OutputFrameIntensity;
        }

        public double[] RunFiltering(double[] Intensity)
        {
            double[] OutputFrameMap = new double[XNum * YNum];
            OutputFrameMap = (double[])Intensity.Clone();

            for (int y = 1; y < YNum - 1; y++)
            {
                for (int x = 1; x < XNum - 1; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double[] NeighborIntensity = new double[9];
                    NeighborIntensity[0] = Intensity[CurrentPixelIndex - XNum - 1];
                    NeighborIntensity[1] = Intensity[CurrentPixelIndex - XNum];
                    NeighborIntensity[2] = Intensity[CurrentPixelIndex - XNum + 1];
                    NeighborIntensity[3] = Intensity[CurrentPixelIndex - 1];
                    NeighborIntensity[4] = Intensity[CurrentPixelIndex];
                    NeighborIntensity[5] = Intensity[CurrentPixelIndex + 1];
                    NeighborIntensity[6] = Intensity[CurrentPixelIndex + XNum - 1];
                    NeighborIntensity[7] = Intensity[CurrentPixelIndex + XNum];
                    NeighborIntensity[8] = Intensity[CurrentPixelIndex + XNum + 1];

                    Array.Sort(NeighborIntensity);
                    if (FType == FilterType.Median)
                        OutputFrameMap[CurrentPixelIndex] = NeighborIntensity[4];
                    else if (FType == FilterType.Dilation)
                        OutputFrameMap[CurrentPixelIndex] = NeighborIntensity[8];
                    else if (FType == FilterType.Erosion)
                        OutputFrameMap[CurrentPixelIndex] = NeighborIntensity[0];
                }
            }

            return OutputFrameMap;
        }
    }
}
