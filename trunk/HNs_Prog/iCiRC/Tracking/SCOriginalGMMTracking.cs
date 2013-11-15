using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using ManagedMRF;

namespace iCiRC
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
            BackModelNum = 10;
            ForeModelNum = 20;
            GMMComponent = new SpatialColorGaussianModel[BackModelNum + ForeModelNum];
            for (int i = 0; i < BackModelNum + ForeModelNum; i++)
                GMMComponent[i] = new SpatialColorGaussianModel();
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

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, FrameNum);
            winProgress.Show();

            // For the first frame (Post-updating)
            int TotalModelNum = BackModelNum + ForeModelNum;
            double[][] PosteriorProbability = InitialExpectationStep();
            MaximizationStepInPostUpdating(0, PosteriorProbability);
            const int EMIterNum = 5;
            for (int iter = 1; iter < EMIterNum; iter++)
            {
                ExpectationStepInPostUpdating(0, ref PosteriorProbability);
                MaximizationStepInPostUpdating(0, PosteriorProbability);
            }
            winProgress.Increment(1);

            // For each frame 
            for (int f = 1; f < FrameNum; f++)
            {
                // Weight normalization (Fore/back seperated GMM -> Global GMM)
                WeightNormalization(f - 1);

                // Pre-updating EM
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    ExpectationStepInPreUpdating(f, ref PosteriorProbability);
                    MaximizationStepInPreUpdating(f, PosteriorProbability);
                }

                // Weight normalization (Global GMM -> Fore/back seperated GMM)
                WeightNormalization();

                // Segmentation
                SegmentationUsingGraphCut(f);

                // Post-undating EM
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    ExpectationStepInPostUpdating(f, ref PosteriorProbability);
                    MaximizationStepInPostUpdating(f, PosteriorProbability);
                }
                winProgress.Increment(1);
            }
            winProgress.Close();
            return FrameMask;
        }

        void SegmentationUsingThresholding(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFramePixelOffset + i]);

            // Homophrphic filtering
            HomomorphicFilter FilteringProcessor = new HomomorphicFilter(XNum, YNum);
            byte[] FilteredCurrentXraySlice = FilteringProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();

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
            FilteredCurrentXraySlice = MorphologicalProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();
            MorphologicalProcessor.FType = MorphologicalFilter.FilterType.Dilation;
            FilteredCurrentXraySlice = MorphologicalProcessor.RunFiltering(CurrentXraySlice);
            CurrentXraySlice = (byte[])FilteredCurrentXraySlice.Clone();

            for (int i = 0; i < FramePixelNum; i++)
            {
                if (CurrentXraySlice[i] == Constants.LABEL_FOREGROUND)
                    FrameMask[CurrentFramePixelOffset + i] = Constants.LABEL_FOREGROUND;
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
                    double ForeLikelihood = 0.0;
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
        /** @brief For the first frame, thresholding segmentation and assignment of the posterior probability  
            @author Hyunna Lee
            @date 2013.11.08
            @return uniformly assigned posterior probability 
        */
        //-------------------------------------------------------------------------
        double[][] InitialExpectationStep()
        {
            int TotalModelNum = BackModelNum + ForeModelNum;

            // Segmentation of the first frame using thresholding
            int FramePixelNum = XNum * YNum;
            const ushort VesselIntensityThresholdValue = 64;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameIntensity[i] < VesselIntensityThresholdValue)
                    FrameMask[i] = Constants.LABEL_FOREGROUND;
            }

            // 1st E-step
            double[][] PosteriorProbability = new double[FramePixelNum][];
            for (int i = 0; i < FramePixelNum; i++)
            {
                // Initialize the probability array
                PosteriorProbability[i] = new double[TotalModelNum];
                PosteriorProbability[i].Initialize();

                // For each pixel z, compute the posterior probability, P(k|z)
                if (FrameMask[i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                        PosteriorProbability[i][k] = 1.0 / Convert.ToDouble(BackModelNum);
                }
                else if (FrameMask[i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        PosteriorProbability[i][k] = 1.0 / Convert.ToDouble(ForeModelNum);
                }
            }
            return PosteriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For the first frame, thresholding segmentation and assignment of the posterior probability  
            @author Hyunna Lee
            @date 2013.11.08
            @return uniformly assigned posterior probability 
        */
        //-------------------------------------------------------------------------
        double[][] InitialExpectationStep(int CurrentFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            // K-means clustering
            int[] ClusterBuffer = new int[FramePixelNum];
            ClusterBuffer.Initialize();

            // Initial cluster assignment by Random()
            Random randomizer = new Random(Convert.ToInt32(DateTime.Now.Ticks));
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                    ClusterBuffer[i] = randomizer.Next(0, BackModelNum);
                else
                    ClusterBuffer[i] = randomizer.Next(BackModelNum, TotalModelNum);
            }



            // 1st E-step
            double[][] PosteriorProbability = new double[FramePixelNum][];
            for (int i = 0; i < FramePixelNum; i++)
            {
                // Initialize the probability array
                PosteriorProbability[i] = new double[TotalModelNum];
                PosteriorProbability[i].Initialize();

                // For each pixel z, compute the posterior probability, P(k|z)
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                        PosteriorProbability[i][k] = 1.0 / Convert.ToDouble(BackModelNum);
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        PosteriorProbability[i][k] = 1.0 / Convert.ToDouble(ForeModelNum);
                }
            }
            return PosteriorProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Pre-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
        */
        //-------------------------------------------------------------------------
        void ExpectationStepInPreUpdating(int CurrentFrameIndex, ref double[][] PosteriorProbability)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            double[] GMMProbability = new double[TotalModelNum];
            GMMProbability.Initialize();

            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);

                    double SumGMMProbability = 0.0;
                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        PosteriorProbability[CurrentPixelIndex][k] = 0.0;
                        GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y)
                                            * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        SumGMMProbability += GMMProbability[k];
                    }

                    for (int k = 0; k < BackModelNum; k++)
                        PosteriorProbability[CurrentPixelIndex][k] = GMMProbability[k] / SumGMMProbability;
                }
            }
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Post-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
        */
        //-------------------------------------------------------------------------
        void ExpectationStepInPostUpdating(int CurrentFrameIndex, ref double[][] PosteriorProbability)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            double[] GMMProbability = new double[TotalModelNum];
            GMMProbability.Initialize();

            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);

                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        PosteriorProbability[CurrentPixelIndex][k] = 0.0;
                        GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y)
                                            * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
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
                            PosteriorProbability[CurrentPixelIndex][k] = GMMProbability[k] / BackSumGMMProbability;
                    }
                    else if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                            PosteriorProbability[CurrentPixelIndex][k] = GMMProbability[k] / ForeSumGMMProbability;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, M-step of EM algorithm during Pre-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para CurrentFrameIndex : the index of the current frame
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
        */
        //-------------------------------------------------------------------------
        private void MaximizationStepInPreUpdating(int CurrentFrameIndex, double[][] PosteriorProbability)
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
                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[CurrentPixelIndex][k];
                        SumSpatial[k][0] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                        SumSpatial[k][1] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                        SumIntensity[k] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    }
                }
            }
            // Compute the mean of each Gaussian component
            double TotalSumPosterior = 0.0;
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].SpatialMean[0] = SumSpatial[k][0] / SumPosterior[k];
                GMMComponent[k].SpatialMean[1] = SumSpatial[k][1] / SumPosterior[k];
                GMMComponent[k].IntensityMean = SumIntensity[k] / SumPosterior[k];
                TotalSumPosterior += SumPosterior[k];
            }

            // Compute the sum of variance of spatial and intensity
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        Vector SpatialDifference = new Vector(2);
                        SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                        SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                        double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                        SumSpatialVariance[k][0, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                        SumSpatialVariance[k][0, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                        SumSpatialVariance[k][1, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                        SumSpatialVariance[k][1, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                        SumIntensityVariance[k] += PosteriorProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
                    }
                }
            }
            // Compute the variance and weight of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].SpatialCoVar[0, 0] = SumSpatialVariance[k][0, 0] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[0, 1] = SumSpatialVariance[k][0, 1] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[1, 0] = SumSpatialVariance[k][1, 0] / SumPosterior[k];
                GMMComponent[k].SpatialCoVar[1, 1] = SumSpatialVariance[k][1, 1] / SumPosterior[k];
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumPosterior[k];
                GMMComponent[k].Weight = SumPosterior[k] / TotalSumPosterior;
            }
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
        private void MaximizationStepInPostUpdating(int CurrentFrameIndex, double[][] PosteriorProbability)
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
                            SumPosterior[k] += PosteriorProbability[CurrentPixelIndex][k];
                            SumSpatial[k][0] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                            SumIntensity[k] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                        }
                    }
                    else if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                        {
                            SumPosterior[k] += PosteriorProbability[CurrentPixelIndex][k];
                            SumSpatial[k][0] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                            SumIntensity[k] += PosteriorProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
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
                            SumSpatialVariance[k][0, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += PosteriorProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
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
                            SumSpatialVariance[k][0, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += PosteriorProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += PosteriorProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
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
