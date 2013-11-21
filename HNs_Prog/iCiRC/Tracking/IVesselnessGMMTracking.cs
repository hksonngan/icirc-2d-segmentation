﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedMRF;

namespace iCiRC.Tracking
{
    public class IVesselnessGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        IntensityGaussianModel[] GMMComponent;
        double[] FrameVesselness;

        public IVesselnessGMMTracking()
        {
            BackModelNum = 0;
            ForeModelNum = 0;
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
            @todo To implement the part of segmentation using graph-cut
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
            FrameVesselness = new double[TotalPixelNum];
            FrameVesselness.Initialize();

            const int EMIterNum = 5;
            const int StartFrameIndex = 0;

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, FrameNum);
            winProgress.Show();

            // For the first frame (Post-updating)
            ComputeVesselness(StartFrameIndex);
            SegmentationUsingVesselnessThresholding(StartFrameIndex);
            InitializeGMMModelParameters(StartFrameIndex);
            for (int iter = 0; iter < EMIterNum; iter++)        // EM interation
            {
                double[,] PosteriorProbability = ExpectationStepInPostUpdating(StartFrameIndex);
                MaximizationStepInPostUpdating(StartFrameIndex, PosteriorProbability);
            }
            winProgress.Increment(1);

            // For each frame 
            for (int f = StartFrameIndex + 1; f < FrameNum; f++)
            {
                ComputeVesselness(f);
                SegmentationUsingDataCost(f);
                SegmentationUsingGraphCut(f);

                // Post-undating EM
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    double[,] PosteriorProbability = ExpectationStepInPostUpdating(f);
                    MaximizationStepInPostUpdating(f, PosteriorProbability);
                }
                winProgress.Increment(1);
            }
            winProgress.Close();

            return FrameMask;
        }

        void SegmentationUsingVesselnessThresholding(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            // Thresholding
            const double VesselnessThreshold = 0.4;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            CurrentXraySlice.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameVesselness[CurrentFramePixelOffset + i] > VesselnessThreshold)
                    CurrentXraySlice[i] = Constants.LABEL_FOREGROUND;
            }

            // Opening operation
            MorphologicalFilter MorphologicalProcessor = new MorphologicalFilter(XNum, YNum);
            MorphologicalProcessor.FType = MorphologicalFilter.FilterType.Erosion;
            byte[] FilteredCurrentXraySlice = MorphologicalProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();
            MorphologicalProcessor.FType = MorphologicalFilter.FilterType.Dilation;
            FilteredCurrentXraySlice = MorphologicalProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();
            FilteredCurrentXraySlice = MorphologicalProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();

            for (int i = 0; i < FramePixelNum; i++)
            {
                if (CurrentXraySlice[i] == Constants.LABEL_FOREGROUND)
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_FOREGROUND;
            }
        }

        void InitializeGMMModelParameters(int CurrentFrameIndex)
        {
            // K-means clustering
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentSliceFrameMask = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentSliceFrameMask[i] = FrameMask[CurrentFrameOffset + i];

            // K-means clustering
            KmeansClustering BackClustering = new KmeansClustering();
            BackModelNum = BackClustering.RunClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_BACKGROUND);
            KmeansClustering ForeClustering = new KmeansClustering();
            ForeModelNum = ForeClustering.RunClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_FOREGROUND);
            int TotalModelNum = BackModelNum + ForeModelNum;
            GMMComponent = new IntensityGaussianModel[TotalModelNum];
            for (int i = 0; i < TotalModelNum; i++)
                GMMComponent[i] = new IntensityGaussianModel();

            // 1st E-step
            double[,] PosteriorProbability = new double[FramePixelNum, TotalModelNum];
            PosteriorProbability.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                // For each pixel z, compute the posterior probability, P(k|z)
                if (CurrentSliceFrameMask[i] == Constants.LABEL_BACKGROUND)
                    PosteriorProbability[i, BackClustering.ClusterLabel[i] - 1] = 1.0;
                else if (CurrentSliceFrameMask[i] == Constants.LABEL_FOREGROUND)
                    PosteriorProbability[i, BackModelNum + ForeClustering.ClusterLabel[i] - 1] = 1.0;
            }

            MaximizationStepInPostUpdating(CurrentFrameIndex, PosteriorProbability);
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Post-updating  
            @author Hyunna Lee
            @date 2013.11.19
            @para CurrentFrameIndex : the index of the current frame
            @return posterior probability for each pixel
        */
        //-------------------------------------------------------------------------
        double[,] ExpectationStepInPostUpdating(int CurrentFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            double[,] PosteriorProbability = new double[FramePixelNum, TotalModelNum];
            double[] GMMProbability = new double[TotalModelNum];
            PosteriorProbability.Initialize();
            GMMProbability.Initialize();

            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                for (int k = 0; k < TotalModelNum; k++)
                    GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);

                double BackSumGMMProbability = 0.0;
                double ForeSumGMMProbability = 0.0;
                for (int k = 0; k < BackModelNum; k++)
                    BackSumGMMProbability += GMMProbability[k];
                for (int k = BackModelNum; k < TotalModelNum; k++)
                    ForeSumGMMProbability += GMMProbability[k];

                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                        PosteriorProbability[i, k] = GMMProbability[k] / BackSumGMMProbability;
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        PosteriorProbability[i, k] = GMMProbability[k] / ForeSumGMMProbability;
                }
            }
            return PosteriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, M-step of EM algorithm during Post-updating  
            @author Hyunna Lee
            @date 2013.11.19
            @para CurrentFrameIndex : the index of the current frame
            @para PosteriorProbability : posterior probability for each pixel
        */
        //-------------------------------------------------------------------------
        private void MaximizationStepInPostUpdating(int CurrentFrameIndex, double[,] PosteriorProbability)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            int TotalModelNum = BackModelNum + ForeModelNum;

            // Initialize the temporary arrays 
            double[] SumPosterior = new double[TotalModelNum];
            double[] SumIntensity = new double[TotalModelNum];
            double[] SumIntensityVariance = new double[TotalModelNum];
            SumPosterior.Initialize();
            SumIntensity.Initialize();
            SumIntensityVariance.Initialize();

            // Compute the sum of posterior and intensity and vesselness
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIntensity[k] += PosteriorProbability[i, k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIntensity[k] += PosteriorProbability[i, k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                    }
                }
            }
            // Compute the mean of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
                GMMComponent[k].IntensityMean = SumIntensity[k] / SumPosterior[k];

            // Compute the sum of variance of spatial and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]) - GMMComponent[k].IntensityMean;
                        SumIntensityVariance[k] += PosteriorProbability[i, k] * IntensityDifference * IntensityDifference;
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]) - GMMComponent[k].IntensityMean;
                        SumIntensityVariance[k] += PosteriorProbability[i, k] * IntensityDifference * IntensityDifference;
                    }
                }
            }
            // Compute the variance of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumPosterior[k];


            // Compute the sum of total posterior
            double BackSumPosterior = 0.0;
            double ForeSumPosterior = 0.0;
            for (int k = 0; k < BackModelNum; k++)
                BackSumPosterior += SumPosterior[k];
            for (int k = BackModelNum; k < TotalModelNum; k++)
                ForeSumPosterior += SumPosterior[k];
            // Compute the weight of each Gaussian component
            for (int k = 0; k < BackModelNum; k++)
                GMMComponent[k].Weight = SumPosterior[k] / BackSumPosterior;
            for (int k = BackModelNum; k < TotalModelNum; k++)
                GMMComponent[k].Weight = SumPosterior[k] / ForeSumPosterior;
        }

        void SegmentationUsingDataCost(int CurrentFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + i]);
                // Likelihood
                double BackLikelihood = 0.0;
                double ForeLikelihood = 0.0;
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity)
                                            * (1.0 - FrameVesselness[CurrentFramePixelOffset + i]);
                    BackLikelihood += Math.Log10(GMMLikelihood);
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity)
                                            * FrameVesselness[CurrentFramePixelOffset + i];
                    ForeLikelihood += Math.Log10(GMMLikelihood);
                }

                if (ForeLikelihood > BackLikelihood)
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_FOREGROUND;
                else
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_BACKGROUND;
            }
        }

        //---------------------------------------------------------------------------
        /** @brief Segmentation of the each frame using graph-cut algorithm
            @author Hyunna Lee
            @date 2013.11.12
            @param CurrentFrameIndex : the index of the current frame
            @todo To implement the graph-cut algorithm using ManagedMRF classes
        */
        //-------------------------------------------------------------------------
        unsafe void SegmentationUsingGraphCut(int CurrentFrameIndex)
        {
            const int GCIterNum = 5;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            double[] SmoothnessHorizontal = new double[FramePixelNum];
            double[] SmoothnessVertical = new double[FramePixelNum];
            SmoothnessHorizontal.Initialize();
            SmoothnessVertical.Initialize();
            double[] DataEnergy = BuildDataEnergyArray(CurrentFrameIndex);
            double[] SmoothnessEnergy = BuildSmoothnessEnergyArray(CurrentFrameIndex, ref SmoothnessHorizontal, ref SmoothnessVertical);

            fixed (double* BufData = DataEnergy, BufSmoothness = SmoothnessEnergy, BufHSmoothness = SmoothnessHorizontal, BufVSmoothness = SmoothnessVertical)
            {
                GraphCutWrap GraphCut = new GraphCutWrap(XNum, YNum, BufData, BufSmoothness, BufHSmoothness, BufVSmoothness, true);
                GraphCut.Initialize();
                GraphCut.ClearAnswer();

                double Energy = GraphCut.GetTotalEnergy();
                for (int iter = 0; iter < GCIterNum; iter++)
                {
                    GraphCut.OptimizeOneIteration();
                    Energy = GraphCut.GetTotalEnergy();
                }

                for (int i = 0; i < FramePixelNum; i++)
                {
                    if (GraphCut.GetLabel(i) == 0)
                        FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_BACKGROUND;
                    else
                        FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_FOREGROUND;
                }
            }
        }

        private void ComputeVesselness(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFramePixelOffset + i]);

            // Frangi's vesselness
            const int ScaleNum = 4;
            double[] ScaleArray = { 2.12, 2.72, 3.5, 4.0 };
            ResponseMap map = new ResponseMap();
            double[] Vesselness = map.RunFrangiMethod2D(XNum, YNum, CurrentXraySlice, ScaleNum, ScaleArray);

            for (int i = 0; i < FramePixelNum; i++)
                FrameVesselness[CurrentFramePixelOffset + i] = Vesselness[i];
        }

        private double[] BuildDataEnergyArray(int CurrentFrameIndex)
        {
            const int LabelNum = 2;
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            double[] DataCost = new double[FramePixelNum * LabelNum];
            DataCost.Initialize();

            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + i]);
                // Likelihood
                DataCost[i * LabelNum] = 0.0;
                DataCost[i * LabelNum + 1] = 0.0;
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity)
                        * (1.0 - FrameVesselness[CurrentFramePixelOffset + i]);
                    DataCost[i * LabelNum] -= Math.Log10(GMMLikelihood);
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity)
                        * FrameVesselness[CurrentFramePixelOffset + i];
                    DataCost[i * LabelNum + 1] -= Math.Log10(GMMLikelihood);
                }
            }
            return DataCost;
        }

        private double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
            const double Lamda = 1.0;
            const double Sigma = 50.0;
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int LabelNum = 2;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            double[] SmoothnessCost = new double[LabelNum * LabelNum];
            SmoothnessCost.Initialize();
            SmoothnessCost[1] = SmoothnessCost[2] = 1.0;

            // Horizontal 
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 1; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex - 1]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    HSmoothness[CurrentPixelIndex - 1] = Lamda * Math.Exp(-(IntensityDifference * IntensityDifference) / (Sigma * Sigma));
                }
            }
            // Vertical
            for (int y = 1; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex - XNum]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    VSmoothness[CurrentPixelIndex - XNum] = Lamda * Math.Exp(-(IntensityDifference * IntensityDifference) / (Sigma * Sigma));
                }
            }
            return SmoothnessCost;
        }
    }
}