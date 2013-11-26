using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC.Tracking
{
    //---------------------------------------------------------------------------
    /** @class IntensityGMMTracking
        @author Hyunna Lee
        @date 2013.11.19
        @brief Intensity-only GMM tracking
    */
    //-------------------------------------------------------------------------
    public class IntensityGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        IntensityGaussianModel[] GMMComponent;

        public IntensityGMMTracking()
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
            const int StartFrameIndex = 20;

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
            for (int f = StartFrameIndex + 1; f < 40; f++)
            {
                // Pre-updating EM
                /*
                WeightNormalization(true);
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    double[,] PosteriorProbability = ExpectationStepInPreUpdating(f);
                    MaximizationStepInPreUpdating(f, PosteriorProbability);
                }
                WeightNormalization(false);
                 * */
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

        void SegmentationUsingVesselnessThresholding(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFramePixelOffset + i]);

            // Frangi's vesselness
            const int ScaleNum = 4;
            double[] ScaleArray = {2.12, 2.72, 3.5, 4.0};
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
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + i]);
                // Likelihood
                double BackLikelihood = 0.0;
                double ForeLikelihood = 0.0;
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                    BackLikelihood += Math.Log10(GMMLikelihood);
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                    ForeLikelihood += Math.Log10(GMMLikelihood);
                }

                if (ForeLikelihood > BackLikelihood)
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_FOREGROUND;
                else
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_BACKGROUND;
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
            Vector[] CurrentSliceFeatureMask = new Vector[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
            {
                CurrentSliceFeatureMask[i] = new Vector(1);
                CurrentSliceFeatureMask[i][0] = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
            }

            // K-means clustering
            KmeansClustering BackClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_BACKGROUND);
            BackModelNum = BackClustering.RunClustering(1, CurrentSliceFeatureMask, 100.0);
            KmeansClustering ForeClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_FOREGROUND);
            ForeModelNum = ForeClustering.RunClustering(1, CurrentSliceFeatureMask, 100.0);
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
                    PosteriorProbability[i,BackClustering.ClusterLabel[i] - 1] = 1.0;
                else if (CurrentSliceFrameMask[i] == Constants.LABEL_FOREGROUND)
                    PosteriorProbability[i,BackModelNum + ForeClustering.ClusterLabel[i] - 1] = 1.0;
            }

            MaximizationStepInPostUpdating(CurrentFrameIndex, PosteriorProbability);
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Pre-updating  
            @author Hyunna Lee
            @date 2013.11.19
            @para CurrentFrameIndex : the index of the current frame
            @return posterior probability for each pixel
        */
        //-------------------------------------------------------------------------
        double[,] ExpectationStepInPreUpdating(int CurrentFrameIndex)
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

                double SumGMMProbability = 0.0;
                for (int k = 0; k < TotalModelNum; k++)
                    SumGMMProbability += GMMProbability[k];

                for (int k = 0; k < TotalModelNum; k++)
                    PosteriorProbability[i, k] = GMMProbability[k] / SumGMMProbability;
            }
            return PosteriorProbability;
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
                        PosteriorProbability[i,k] = GMMProbability[k] / BackSumGMMProbability;
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        PosteriorProbability[i,k] = GMMProbability[k] / ForeSumGMMProbability;
                }
            }
            return PosteriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, M-step of EM algorithm during Pre-updating  
            @author Hyunna Lee
            @date 2013.11.19
            @para CurrentFrameIndex : the index of the current frame
            @para PosteriorProbability : posterior probability for each pixel
        */
        //-------------------------------------------------------------------------
        private void MaximizationStepInPreUpdating(int CurrentFrameIndex, double[,] PosteriorProbability)
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

            // Compute the sum of posterior and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                for (int k = 0; k < TotalModelNum; k++)
                {
                    SumPosterior[k] += PosteriorProbability[i, k];
                    SumIntensity[k] += PosteriorProbability[i, k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                }
            }
            // Compute the mean of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
                GMMComponent[k].IntensityMean = SumIntensity[k] / SumPosterior[k];

            // Compute the sum of variance of spatial and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                for (int k = 0; k < TotalModelNum; k++)
                {
                    double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]) - GMMComponent[k].IntensityMean;
                    SumIntensityVariance[k] += PosteriorProbability[i, k] * IntensityDifference * IntensityDifference;
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

            // Compute the sum of posterior and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i,k];
                        SumIntensity[k] += PosteriorProbability[i,k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i,k];
                        SumIntensity[k] += PosteriorProbability[i,k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
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
                        SumIntensityVariance[k] += PosteriorProbability[i,k] * IntensityDifference * IntensityDifference;
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]) - GMMComponent[k].IntensityMean;
                        SumIntensityVariance[k] += PosteriorProbability[i,k] * IntensityDifference * IntensityDifference;
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

        //---------------------------------------------------------------------------
        /** @brief Normalization of the Gaussian component weights seperating/combining background and foreground
            @author Hyunna Lee
            @date 2013.11.08
            @param PreviousFrameIndex : the index of the previous frame
        */
        //-------------------------------------------------------------------------
        void WeightNormalization(bool IsCombining)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            if (IsCombining)
            {
                for (int k = 0; k < TotalModelNum; k++)
                    GMMComponent[k].Weight /= 2.0;
            }
            else
            {
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

        }
    }
}
