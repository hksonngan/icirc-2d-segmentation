using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    public class KmeansClustering : Clustering
    {
        private Vector[] ClusterMean;
        private Vector[] DataFeature;

        public KmeansClustering(int paraXNum, int paraYNum, byte[] paraImageMask, byte paraObjectLabel)
        {
            XNum = paraXNum; YNum = paraYNum;
            ImageMask = paraImageMask;
            ObjectLabel = paraObjectLabel;
            DataDimension = 1;
        }

        public int RunClustering(int Dimension, Vector[] FeatureMask, double NewClusterThreshold)
        {
            LabelNum = 0;
            ClusterLabel = new int[XNum * YNum];
            ClusterLabel.Initialize();

            DataFeature = FeatureMask;
            DataDimension = Dimension;
            LabelNum = InitialAssignment(NewClusterThreshold);

            for (int iter = 0; iter < 5; iter++)
            {
                UpdateClusterMean();
                UpdateAssignment();
            }
            return LabelNum;
        }

        private int InitialAssignment(double DistanceThreshold)
        {
            List<Vector> ClusterMeanList = new List<Vector>();
            for (int i = 0; i < XNum * YNum; i++)
            {
                if (ImageMask[i] == ObjectLabel)
                {
                    // Find the most close cluter
                    int ClosestClusterIndex = 0;
                    double MinCurrentDistance = DistanceThreshold;
                    for (int c = 0; c < ClusterMeanList.Count; c++)
                    {
                        double CurrentDistance = GetDistance(i, c, ClusterMeanList);
                        if (CurrentDistance < MinCurrentDistance)
                        {
                            MinCurrentDistance = CurrentDistance;
                            ClosestClusterIndex = c + 1;
                        }
                    }
                    if (ClosestClusterIndex == 0)
                    {
                        ClusterMeanList.Add(DataFeature[i]);
                        ClusterLabel[i] = ClusterMeanList.Count;
                    }
                    else
                        ClusterLabel[i] = ClosestClusterIndex;
                }
            }
            return ClusterMeanList.Count;
        }

        private void UpdateAssignment()
        {
            for (int i = 0; i < XNum * YNum; i++)
            {
                if (ImageMask[i] == ObjectLabel)
                {
                    // Find the most close cluter
                    int ClosestClusterIndex = 0;
                    double MinCurrentDistance = Double.MaxValue;
                    for (int c = 0; c < LabelNum; c++)
                    {
                        double CurrentDistance = GetDistance(i, c);
                        if (CurrentDistance < MinCurrentDistance)
                        {
                            MinCurrentDistance = CurrentDistance;
                            ClosestClusterIndex = c + 1;
                        }
                    }
                    ClusterLabel[i] = ClosestClusterIndex;
                }
            }
        }

        private void UpdateClusterMean()
        {
            ClusterMean = new Vector[LabelNum];
            int[] ClusterSize = new int[LabelNum];
            ClusterSize.Initialize();
            for (int i = 0; i < LabelNum; i++)
            {
                ClusterMean[i] = new Vector(DataDimension);
                for (int d = 0; d < DataDimension; d++)
                    ClusterMean[i][d] = 0.0;
            }

            for (int i = 0; i < XNum * YNum; i++)
            {
                if (ImageMask[i] == ObjectLabel)
                {
                    for (int d = 0; d < DataDimension; d++)
                        ClusterMean[ClusterLabel[i] - 1][d] += DataFeature[i][d];
                    ClusterSize[ClusterLabel[i] - 1]++;
                }
            }

            for (int i = 0; i < LabelNum; i++)
            {
                if (ClusterSize[i] > 0)
                {
                    for (int d = 0; d < DataDimension; d++)
                        ClusterMean[i][d] /= Convert.ToDouble(ClusterSize[i]);
                }
            }
        }

        private double GetDistance(int CurrentPixelIndex, int CurrentClusterIndex)
        {
            double Distance = 0.0;
            for (int d = 0; d < DataDimension; d++)
                Distance += (DataFeature[CurrentPixelIndex][d] - ClusterMean[CurrentClusterIndex][d]) * (DataFeature[CurrentPixelIndex][d] - ClusterMean[CurrentClusterIndex][d]);
            return Math.Sqrt(Distance);
        }

        private double GetDistance(int CurrentPixelIndex, int CurrentClusterIndex, List<Vector> ClusterMeanList)
        {
            double Distance = 0.0;
            for (int d = 0; d < DataDimension; d++)
                Distance += (DataFeature[CurrentPixelIndex][d] - ClusterMeanList[CurrentClusterIndex][d]) * (DataFeature[CurrentPixelIndex][d] - ClusterMeanList[CurrentClusterIndex][d]);
            return Math.Sqrt(Distance);
        }
    }
}
