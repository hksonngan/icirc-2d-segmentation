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

        public KmeansClustering()
        {
            CriterionType = DistanceCriterion.Intensity;
        }

        public int RunClustering(int paraXNum, int paraYNum, byte[] paraImageMask, byte paraObjectLabel)
        {
            if (paraXNum <= 0 || paraYNum <= 0)
                return -1;

            XNum = paraXNum; YNum = paraYNum;
            ImageMask = paraImageMask;
            ObjectLabel = paraObjectLabel;

            LabelNum = 0;
            ClusterLabel = new int[XNum * YNum];
            ClusterLabel.Initialize();

            if (CriterionType == DistanceCriterion.Intensity)
            {
                LabelNum = InitialAssignment(20.0);
            }
            else if (CriterionType == DistanceCriterion.Position)
            {
                if (paraObjectLabel == Constants.LABEL_BACKGROUND)
                    LabelNum = InitialAssignment(200.0);
                else if (paraObjectLabel == Constants.LABEL_FOREGROUND)
                    LabelNum = InitialAssignment(10.0);
            }

            /*
            for (int iter = 0; iter < 5; iter++)
            {
                UpdateClusterMean();
                UpdateAssignment();
            }
             * */
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
                        if (CriterionType == DistanceCriterion.Intensity)
                        {
                            Vector CurrentPixel = new Vector(1);
                            CurrentPixel[0] = Convert.ToDouble(ImageMask[i]);
                            ClusterMeanList.Add(CurrentPixel);
                        }
                        else if (CriterionType == DistanceCriterion.Position)
                        {
                            Vector CurrentPixel = new Vector(2);
                            CurrentPixel[0] = Convert.ToDouble(i % XNum);
                            CurrentPixel[1] = Convert.ToDouble(i / XNum);
                            ClusterMeanList.Add(CurrentPixel);
                        }
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
            if (CriterionType == DistanceCriterion.Intensity)
            {
                for (int i = 0; i < LabelNum; i++)
                {
                    ClusterMean[i] = new Vector(1);
                    ClusterMean[i][0] = 0.0;
                }

                for (int i = 0; i < XNum * YNum; i++)
                {
                    if (ImageMask[i] == ObjectLabel)
                    {
                        ClusterMean[ClusterLabel[i] - 1][0] += Convert.ToDouble(ImageMask[i]);
                        ClusterSize[ClusterLabel[i] - 1]++;
                    }
                }
                for (int i = 0; i < LabelNum; i++)
                {
                    if (ClusterSize[i] > 0)
                        ClusterMean[i][0] /= Convert.ToDouble(ClusterSize[i]);
                }
            }
            else if (CriterionType == DistanceCriterion.Position)
            {
                for (int i = 0; i < LabelNum; i++)
                {
                    ClusterMean[i] = new Vector(2);
                    ClusterMean[i][0] = ClusterMean[i][1] = 0.0;
                }

                for (int i = 0; i < XNum * YNum; i++)
                {
                    if (ImageMask[i] == ObjectLabel)
                    {
                        ClusterMean[ClusterLabel[i] - 1][0] += Convert.ToDouble(i % XNum);
                        ClusterMean[ClusterLabel[i] - 1][1] += Convert.ToDouble(i / XNum);
                        ClusterSize[ClusterLabel[i] - 1]++;
                    }
                }
                for (int i = 0; i < LabelNum; i++)
                {
                    if (ClusterSize[i] > 0)
                    {
                        ClusterMean[i][0] /= Convert.ToDouble(ClusterSize[i]);
                        ClusterMean[i][1] /= Convert.ToDouble(ClusterSize[i]);
                    }
                }
            }
        }

        private double GetDistance(int CurrentPixelIndex, int CurrentClusterIndex)
        {
            if (CriterionType == DistanceCriterion.Position)
            {
                double CurrentPixelX = Convert.ToDouble(CurrentPixelIndex % XNum);
                double CurrentPixelY = Convert.ToDouble(CurrentPixelIndex / XNum);
                double Distance = (CurrentPixelX - ClusterMean[CurrentClusterIndex][0]) * (CurrentPixelX - ClusterMean[CurrentClusterIndex][0])
                    + (CurrentPixelY - ClusterMean[CurrentClusterIndex][1]) * (CurrentPixelY - ClusterMean[CurrentClusterIndex][1]);
                return Math.Sqrt(Distance);
            }
            else if (CriterionType == DistanceCriterion.Intensity)
            {
                double CurrentPixelIntensity = Convert.ToDouble(ImageMask[CurrentPixelIndex]);
                return Math.Abs(CurrentPixelIntensity - ClusterMean[CurrentClusterIndex][0]);
            }
            return -1.0;
        }

        private double GetDistance(int CurrentPixelIndex, int CurrentClusterIndex, List<Vector> ClusterMeanList)
        {
            if (CriterionType == DistanceCriterion.Position)
            {
                double CurrentPixelX = Convert.ToDouble(CurrentPixelIndex % XNum);
                double CurrentPixelY = Convert.ToDouble(CurrentPixelIndex / XNum);
                double Distance = (CurrentPixelX - ClusterMeanList[CurrentClusterIndex][0]) * (CurrentPixelX - ClusterMeanList[CurrentClusterIndex][0])
                    + (CurrentPixelY - ClusterMeanList[CurrentClusterIndex][1]) * (CurrentPixelY - ClusterMeanList[CurrentClusterIndex][1]);
                return Math.Sqrt(Distance);
            }
            else if (CriterionType == DistanceCriterion.Intensity)
            {
                double CurrentPixelIntensity = Convert.ToDouble(ImageMask[CurrentPixelIndex]);
                return Math.Abs(CurrentPixelIntensity - ClusterMeanList[CurrentClusterIndex][0]);
            }
            return -1.0;
        }
    }
}
