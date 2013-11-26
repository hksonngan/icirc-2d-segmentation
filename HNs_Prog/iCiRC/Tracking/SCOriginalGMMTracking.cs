using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using ManagedMRF;

namespace iCiRC.Tracking
{
    //---------------------------------------------------------------------------
    /** @class SCOriginalGMMTracking
        @author Hyunna Lee
        @date 2013.11.12
        @brief Spatial-color GMM tracking
    */
    //-------------------------------------------------------------------------
    public class SCOriginalGMMTracking : VesselTracking
    { 
        int BackModelNum, ForeModelNum;
        SpatialColorGaussianModel[] GMMComponent;

        public SCOriginalGMMTracking()
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

            const int EMIterNum = 5;
            const int StartFrameIndex = 30;

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, FrameNum);
            winProgress.Show();

            // For the first frame (Post-updating)
            SegmentationUsingVesselnessThresholding(StartFrameIndex);
            InitializeGMMModelParameters(StartFrameIndex);
            for (int iter = 0; iter < EMIterNum; iter++)        // EM interation
            {
                double[,] PosteriorProbability = ExpectationStepInPostUpdating(StartFrameIndex);
                MaximizationStepInPostUpdating(StartFrameIndex, PosteriorProbability);
            }
            winProgress.Increment(1);

            // For each frame 
            for (int f = StartFrameIndex + 1; f < StartFrameIndex + 2; f++)
            {
                SegmentationUsingDataCost(f);

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

        void InitializeGMMModelParameters(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentSliceFrameMask = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentSliceFrameMask[i] = FrameMask[CurrentFrameOffset + i];

            // SRG clustering
            SRGClustering ForeClustering = new SRGClustering(64, 32 * 32);
            ForeModelNum = ForeClustering.RunClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_FOREGROUND);
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (ForeClustering.ClusterLabel[i] == Constants.LABEL_BACKGROUND)
                    FrameMask[CurrentFrameOffset + i] = CurrentSliceFrameMask[i] = Constants.LABEL_BACKGROUND;
            }

            Vector[] CurrentSliceFeatureMask = new Vector[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
            {
                CurrentSliceFeatureMask[i] = new Vector(2);
                CurrentSliceFeatureMask[i][0] = Convert.ToDouble(i % XNum);
                CurrentSliceFeatureMask[i][1] = Convert.ToDouble(i / XNum);
            }

            // K-means clustering
            KmeansClustering BackClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_BACKGROUND);
            BackModelNum = BackClustering.RunClustering(1, CurrentSliceFeatureMask, 128);

            int TotalModelNum = BackModelNum + ForeModelNum;
            GMMComponent = new SpatialColorGaussianModel[TotalModelNum];
            for (int i = 0; i < TotalModelNum; i++)
                GMMComponent[i] = new SpatialColorGaussianModel();

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

        void SegmentationUsingVesselnessThresholding(int CurrentFrameIndex)
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

            // Thresholding
            const double VesselnessThreshold = 0.4;
            CurrentXraySlice.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (Vesselness[i] > VesselnessThreshold)
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

        void SegmentationUsingDataCost(int CurrentFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex]);
                    // Likelihood
                    double BackLikelihood = 0.0;
                    double ForeLikelihood = 0.0;
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        //double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        double GMMLikelihood = 0.0;
                        if (GMMComponent[k].GetGaussianProbability(x, y) > 0.2)
                            GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        BackLikelihood += Math.Log10(GMMLikelihood);
                    }
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        //double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        double GMMLikelihood = 0.0;
                        if (GMMComponent[k].GetGaussianProbability(x, y) > 0.2)
                            GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        ForeLikelihood += Math.Log10(GMMLikelihood);
                    }

                    if (ForeLikelihood > BackLikelihood)
                        FrameMask[CurrentFramePixelOffset + CurrentPixelIndex] = Constants.LABEL_FOREGROUND;
                    else
                        FrameMask[CurrentFramePixelOffset + CurrentPixelIndex] = Constants.LABEL_BACKGROUND;
                }
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

        double[] BuildDataEnergyArray(int CurrentFrameIndex)
        {
            const int LabelNum = 2;
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            double[] DataCost = new double[FramePixelNum * LabelNum];
            DataCost.Initialize();

            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + CurrentPixelIndex]);
                    // Likelihood
                    DataCost[CurrentPixelIndex * LabelNum] = 0.0;
                    DataCost[CurrentPixelIndex * LabelNum + 1] = 0.0;
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        DataCost[CurrentPixelIndex * LabelNum] -= Math.Log10(GMMLikelihood);
                    }
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        DataCost[CurrentPixelIndex * LabelNum + 1] -= Math.Log10(GMMLikelihood);
                    }
                }
            }
            return DataCost;
        }

        double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
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
                    HSmoothness[CurrentPixelIndex - 1] = Math.Exp(-(IntensityDifference * IntensityDifference) / (Sigma * Sigma));
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
                    VSmoothness[CurrentPixelIndex - XNum] = Math.Exp(-(IntensityDifference * IntensityDifference) / (Sigma * Sigma));
                }
            }
            return SmoothnessCost;
        }

        //---------------------------------------------------------------------------
        /** @brief Normalization of the Gaussian component weights based on the prior probability of the class
            @author Hyunna Lee
            @date 2013.11.08
            @param PreviousFrameIndex : the index of the previous frame
        */
        //-------------------------------------------------------------------------
        void WeightNormalization(int PreviousFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            double TotalWeight = 0.0;
            for (int k = 0; k < TotalModelNum; k++)
                TotalWeight += GMMComponent[k].Weight;
            if (TotalWeight <= 1.0)
                return;

            int FramePixelNum = XNum * YNum;
            int PreviousFramePixelOffset = PreviousFrameIndex * FramePixelNum;
            int BackgroundPixelCnt = 0;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[PreviousFramePixelOffset + i] == Constants.LABEL_BACKGROUND)
                    BackgroundPixelCnt++;
            }
            double BackgroundPriorProbability = Convert.ToDouble(BackgroundPixelCnt) / Convert.ToDouble(FramePixelNum);
            double ForegroundPriorProbability = 1.0 - BackgroundPriorProbability;
            BackgroundPriorProbability = 0.5;
            ForegroundPriorProbability = 0.5;

            for (int k = 0; k < BackModelNum; k++)
                GMMComponent[k].Weight *= BackgroundPriorProbability;
            for (int k = BackModelNum; k < TotalModelNum; k++)
                GMMComponent[k].Weight *= ForegroundPriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief Normalization of the Gaussian component weights seperating background and foreground
            @author Hyunna Lee
            @date 2013.11.08
            @param PreviousFrameIndex : the index of the previous frame
        */
        //-------------------------------------------------------------------------
        void WeightNormalization()
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            double BackgroundPriorProbability = 0.0;
            double ForegroundPriorProbability = 0.0;
            for (int k = 0; k < BackModelNum; k++)
                BackgroundPriorProbability += GMMComponent[k].Weight;
            for (int k = BackModelNum; k < TotalModelNum; k++)
                ForegroundPriorProbability += GMMComponent[k].Weight;

            if (BackgroundPriorProbability == 0.0 || ForegroundPriorProbability == 0.0)
                return;

            for (int k = 0; k < BackModelNum; k++)
                GMMComponent[k].Weight /= BackgroundPriorProbability;
            for (int k = BackModelNum; k < TotalModelNum; k++)
                GMMComponent[k].Weight /= ForegroundPriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Post-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
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

            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        if (GMMComponent[k].GetGaussianProbability(x, y) > 0.2)
                            GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        else
                            GMMProbability[k] = 0.0;
                    }

                    double BackSumGMMProbability = 0.0;
                    double ForeSumGMMProbability = 0.0;
                    for (int k = 0; k < BackModelNum; k++)
                        BackSumGMMProbability += GMMProbability[k];
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        ForeSumGMMProbability += GMMProbability[k];

                    if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                    {
                        for (int k = 0; k < BackModelNum; k++)
                            PosteriorProbability[CurrentPixelIndex,k] = GMMProbability[k] / BackSumGMMProbability;
                    }
                    else if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                            PosteriorProbability[CurrentPixelIndex,k] = GMMProbability[k] / ForeSumGMMProbability;
                    }
                }
            }
            return PosteriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, M-step of EM algorithm during Post-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para CurrentFrameIndex : the index of the current frame
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
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
            Vector[] SumSpatial = new Vector[TotalModelNum];
            Matrix[] SumSpatialVariance = new Matrix[TotalModelNum];
            SumPosterior.Initialize();
            SumIntensity.Initialize();
            SumIntensityVariance.Initialize();
            for (int k = 0; k < TotalModelNum; k++)
            {
                SumSpatial[k] = new Vector(2);
                SumSpatial[k][0] = SumSpatial[k][1] = 0.0;
                SumSpatialVariance[k] = new Matrix(2, 2);
                SumSpatialVariance[k][0, 0] = SumSpatialVariance[k][0, 1] = SumSpatialVariance[k][1, 0] = SumSpatialVariance[k][1, 1] = 0.0;
            }

            // Compute the sum of posterior, spatial, and intensity
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                    {
                        for (int k = 0; k < BackModelNum; k++)
                        {
                            SumPosterior[k] += PosteriorProbability[CurrentPixelIndex,k];
                            SumSpatial[k][0] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(y);
                            SumIntensity[k] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                        }
                    }
                    else if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                        {
                            SumPosterior[k] += PosteriorProbability[CurrentPixelIndex,k];
                            SumSpatial[k][0] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(y);
                            SumIntensity[k] += PosteriorProbability[CurrentPixelIndex,k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                        }
                    }
                }
            }
            // Compute the mean of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].SpatialMean[0] = SumSpatial[k][0] / SumPosterior[k];
                GMMComponent[k].SpatialMean[1] = SumSpatial[k][1] / SumPosterior[k];
                GMMComponent[k].IntensityMean = SumIntensity[k] / SumPosterior[k];
            }

            // Compute the sum of variance of spatial and intensity
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                    {
                        for (int k = 0; k < BackModelNum; k++)
                        {
                            Vector SpatialDifference = new Vector(2);
                            SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                            SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                            double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                            SumSpatialVariance[k][0, 0] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += PosteriorProbability[CurrentPixelIndex,k] * IntensityDifference * IntensityDifference;
                        }
                    }
                    else if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                        {
                            Vector SpatialDifference = new Vector(2);
                            SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                            SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                            double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                            SumSpatialVariance[k][0, 0] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += PosteriorProbability[CurrentPixelIndex,k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += PosteriorProbability[CurrentPixelIndex,k] * IntensityDifference * IntensityDifference;
                        }
                    }
                }
            }
            // Compute the variance of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].SpatialCoVar[0, 0] = SumSpatialVariance[k][0, 0] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[0, 1] = SumSpatialVariance[k][0, 1] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[1, 0] = SumSpatialVariance[k][1, 0] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[1, 1] = SumSpatialVariance[k][1, 1] / SumPosterior[k];
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumPosterior[k];
            }

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
    }
}
