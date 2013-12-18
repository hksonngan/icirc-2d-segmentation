using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class DistanceTransform : FrameProcessing
    {
        private double[] DistanceMap;
        public enum DistanceType { Euclidean, Chamfer };
        public DistanceType DisType;

        public DistanceTransform()
        {
            DisType = DistanceType.Euclidean;
        }

        public DistanceTransform(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
            DisType = DistanceType.Euclidean;
        }

        public double[] RunDistanceMap(byte[] Mask)
        {
            InputFrameMask = Mask;

            switch (DisType)
            {
                case DistanceType.Euclidean:
                    RunEuclideanDistance();
                    break;
                case DistanceType.Chamfer:
                    RunChamferDistance();
                    break;
            }

            ConvertToProbability(10.0);

            return DistanceMap;
        }

        private void ConvertToProbability(double Sigma)
        {
            int FramePixelNum = XNum * YNum;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (DistanceMap[i] < 0.0)
                    DistanceMap[i] = 0.0;
                double GaussianValue = Math.Exp(-(DistanceMap[i] * DistanceMap[i]) / (2.0 * Sigma * Sigma));
                DistanceMap[i] = GaussianValue / (Sigma * Math.Sqrt(2.0 * Math.PI));
            }
            double MaxProbability = 0.0;
            for (int i = 0; i < FramePixelNum; i++)
                MaxProbability = Math.Max(MaxProbability, DistanceMap[i]);
            for (int i = 0; i < FramePixelNum; i++)
                DistanceMap[i] /= (MaxProbability * 2.0);
        }

        private void RunEuclideanDistance()
        {
            int FramePixelNum = XNum * YNum;
            DistanceMap = new double[XNum * YNum];
            DistanceMap.Initialize();

            for (int i = 0; i < FramePixelNum; i++)
            {
                if (InputFrameMask[i] == Constants.LABEL_FOREGROUND)
                    DistanceMap[i] = 0.0;
                else
                    DistanceMap[i] = 1.0;
            }

            // Step 1-1: Forward scan
            for (int y = 0; y < YNum; y++)
            {
                int Offset = XNum;
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    if (DistanceMap[CurrentPixelIndex] > 0.0)
                        Offset++;
                    else
                        Offset = 0;
                    DistanceMap[CurrentPixelIndex] = Offset * Offset;
                }
            }
            // Step 1-2: Backward scan
            for (int y = 0; y < YNum; y++)
            {
                int Offset = XNum;
                for (int x = XNum - 1; x >= 0; x--)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    if (DistanceMap[CurrentPixelIndex] > 0.0)
                        Offset++;
                    else
                        Offset = 0;
                    DistanceMap[CurrentPixelIndex] = Math.Min(DistanceMap[CurrentPixelIndex], Offset * Offset);
                }
            }

            // Step 2
            double[] Buffer = new double[YNum];
            for (int x = 0; x < XNum; x++)
            {
                for (int y = 0; y < YNum; y++)
                    Buffer[y] = DistanceMap[y * XNum + x];
                for (int y = 0; y < YNum; y++)
                {
                    double Distance = Buffer[y];
                    if (Distance > 0.0)
                    {
                        int rMax = Convert.ToInt32(Math.Sqrt(Distance)) + 1;
                        int rStart = Math.Min(rMax, y);
                        int rEnd = Math.Min(rMax, YNum - 1 - y);
                        for (int i = -rStart; i <= rEnd; i++)
                            Distance = Math.Min(Distance, Buffer[y + i] + Convert.ToDouble(i * i));
                    }
                    DistanceMap[y * XNum + x] = Distance;
                }
            }

            // square root of the distance, m_pDBF
            for (int i = 0; i < FramePixelNum; i++)
                DistanceMap[i] = Math.Sqrt(DistanceMap[i]);
        }

        private void RunChamferDistance()
        {
            // Initialize 
            int FramePixelNum = XNum * YNum;
            DistanceMap = new double[XNum * YNum];
            DistanceMap.Initialize();
            double MaxDistance = Math.Sqrt(Convert.ToDouble(XNum * YNum));
            for (int x = 0; x < XNum; x++)
                DistanceMap[x] = DistanceMap[(YNum - 1) * XNum + x] = MaxDistance;
            for (int y = 0; y < YNum; y++)
                DistanceMap[y * XNum] = DistanceMap[y * XNum + (XNum - 1)] = MaxDistance;
            for (int y = 1; y < YNum - 1; y++)
            {
                for (int x = 1; x < XNum - 1; x++)
                {
                    if (InputFrameMask[(y - 1) * XNum + (x - 1)] == Constants.LABEL_FOREGROUND)
                        DistanceMap[y * XNum + x] = 0.0;
                    else
                        DistanceMap[y * XNum + x] = MaxDistance;
                }
            }

            // Step 1 : Scan Top-Left to Bottom-Right
            for (int y = 1; y < YNum - 1; y++)
            {
                for (int x = 1; x < XNum - 1; x++)
                {
                    double Distance = DistanceMap[y * XNum + x];
                    if (Distance <= -1.0)       // Inside
                    {
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[y * XNum + (x - 1)]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y - 1) * XNum + x]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y - 1) * XNum + (x - 1)]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y - 1) * XNum + (x + 1)]) - 1.0);
                    }
                    else if (Distance >= 1.0)   // Outside
                    {
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[y * XNum + (x - 1)]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y - 1) * XNum + x]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y - 1) * XNum + (x - 1)]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y - 1) * XNum + (x + 1)]) + 1.0);
                    }
                    DistanceMap[y * XNum + x] = Distance;
                }
            }

            // Step 2 : Scan Bottom-Right to Top-Left
            for (int y = YNum - 2; y > 0; y--)
            {
                for (int x = XNum - 2; x > 0; x--)
                {
                    double Distance = DistanceMap[y * XNum + x];
                    if (Distance <= -1.0)       // Inside
                    {
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[y * XNum + (x + 1)]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y + 1) * XNum + x]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y + 1) * XNum + (x + 1)]) - 1.0);
                        Distance = Math.Max(Distance, Math.Min(0.0, DistanceMap[(y + 1) * XNum + (x - 1)]) - 1.0);
                    }
                    else if (Distance >= 1.0)   // Outside
                    {
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[y * XNum + (x + 1)]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y + 1) * XNum + x]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y + 1) * XNum + (x + 1)]) + 1.0);
                        Distance = Math.Min(Distance, Math.Max(0.0, DistanceMap[(y + 1) * XNum + (x - 1)]) + 1.0);
                    }
                    DistanceMap[y * XNum + x] = Distance;
                }
            }
        }
    }
}
