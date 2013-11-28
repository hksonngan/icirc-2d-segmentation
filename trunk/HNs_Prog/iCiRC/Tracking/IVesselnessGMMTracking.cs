using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedMRF;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC.Tracking
{
    public class IVesselnessGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        IVesselnessGaussianModel[] GMMComponent;
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
            const int StartFrameIndex = 20;

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, 5);
            winProgress.Show();

            // For the first frame (Post-updating)
            ComputeVesselness(StartFrameIndex);
            SegmentationUsingVesselnessThresholding(StartFrameIndex);
            PostProcessingUsingCCL(StartFrameIndex, 20);
            InitializeGMMModelParameters(StartFrameIndex);
            for (int iter = 0; iter < EMIterNum; iter++)        // EM interation
            {
                double[,] PosteriorProbability = ExpectationStepInPostUpdating(StartFrameIndex);
                MaximizationStepInPostUpdating(StartFrameIndex, PosteriorProbability);
            }
            winProgress.Increment(1);

            // For each frame 
            for (int f = StartFrameIndex + 1; f < StartFrameIndex + 5; f++)
            {
                ComputeVesselness(f);
                SegmentationUsingDataCost(f);
                PostProcessingUsingCCL(f, 20);
                //SegmentationUsingGraphCut(f);

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
            const double VesselnessThreshold = 255.0 * 0.2;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            CurrentXraySlice.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameVesselness[CurrentFramePixelOffset + i] >= VesselnessThreshold)
                    CurrentXraySlice[i] = Constants.LABEL_FOREGROUND;
            }

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

            Vector[] CurrentSliceFeatureMask = new Vector[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
            {
                CurrentSliceFeatureMask[i] = new Vector(2);
                CurrentSliceFeatureMask[i][0] = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                CurrentSliceFeatureMask[i][1] = FrameVesselness[CurrentFrameOffset + i];
            }

            // K-means clustering
            KmeansClustering BackClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_BACKGROUND);
            BackModelNum = BackClustering.RunClustering(1, CurrentSliceFeatureMask, 50.0);
            KmeansClustering ForeClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_FOREGROUND);
            ForeModelNum = ForeClustering.RunClustering(1, CurrentSliceFeatureMask, 50.0);
            int TotalModelNum = BackModelNum + ForeModelNum;
            GMMComponent = new IVesselnessGaussianModel[TotalModelNum];
            for (int i = 0; i < TotalModelNum; i++)
                GMMComponent[i] = new IVesselnessGaussianModel();

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
                    GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, FrameVesselness[CurrentFrameOffset + i]);

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
            Vector[] SumIVesselness = new Vector[TotalModelNum];
            Matrix[] SumIVesselnessVariance = new Matrix[TotalModelNum];
            SumPosterior.Initialize();
            for (int k = 0; k < TotalModelNum; k++)
            {
                SumIVesselness[k] = new Vector(2);
                SumIVesselness[k][0] = SumIVesselness[k][1] = 0.0;
                SumIVesselnessVariance[k] = new Matrix(2, 2);
                SumIVesselnessVariance[k][0, 0] = SumIVesselnessVariance[k][0, 1] = SumIVesselnessVariance[k][1, 0] = SumIVesselnessVariance[k][1, 1] = 0.0;
            }

            // Compute the sum of posterior and intensity and vesselness
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelVesselness = FrameVesselness[CurrentFrameOffset + i];
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIVesselness[k][0] += PosteriorProbability[i, k] * CurrentPixelIntensity;
                        SumIVesselness[k][1] += PosteriorProbability[i, k] * CurrentPixelVesselness;
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIVesselness[k][0] += PosteriorProbability[i, k] * CurrentPixelIntensity;
                        SumIVesselness[k][1] += PosteriorProbability[i, k] * CurrentPixelVesselness;
                    }
                }
            }
            // Compute the mean of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].IVesselnessMean[0] = SumIVesselness[k][0] / SumPosterior[k];
                GMMComponent[k].IVesselnessMean[1] = SumIVesselness[k][1] / SumPosterior[k];
            }

            // Compute the sum of variance of spatial and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelVesselness = FrameVesselness[CurrentFrameOffset + i];
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        Vector IVesselnessDifference = new Vector(2);
                        IVesselnessDifference[0] = CurrentPixelIntensity - GMMComponent[k].IVesselnessMean[0];
                        IVesselnessDifference[1] = CurrentPixelVesselness - GMMComponent[k].IVesselnessMean[1];
                        SumIVesselnessVariance[k][0, 0] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][0, 1] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][1, 0] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][1, 1] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[1];
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        Vector IVesselnessDifference = new Vector(2);
                        IVesselnessDifference[0] = CurrentPixelIntensity - GMMComponent[k].IVesselnessMean[0];
                        IVesselnessDifference[1] = CurrentPixelVesselness - GMMComponent[k].IVesselnessMean[1];
                        SumIVesselnessVariance[k][0, 0] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][0, 1] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][1, 0] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][1, 1] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[1];
                    }
                }
            }
            // Compute the variance of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].IVesselnessCoVar[0, 0] = SumIVesselnessVariance[k][0, 0] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[0, 1] = SumIVesselnessVariance[k][0, 1] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[1, 0] = SumIVesselnessVariance[k][1, 0] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[1, 1] = SumIVesselnessVariance[k][1, 1] / SumPosterior[k];
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

        void SegmentationUsingDataCost(int CurrentFrameIndex)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            // Weight normalization
            int BackPixelCnt = 0;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset - FramePixelNum + i] == Constants.LABEL_BACKGROUND)
                    BackPixelCnt++;
            }
            double BackPriorProbability = Convert.ToDouble(BackPixelCnt) / Convert.ToDouble(FramePixelNum);
            double ForePriorProbability = 1.0 - BackPriorProbability;

            //using (new OpenMPStyleThread(0, FramePixelNum, 4,
            //    delegate(int start, int end)
            //    {
                    for (int i = 0; i < FramePixelNum; i++)
                    {
                        double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                        double CurrentPixelVesselness = FrameVesselness[CurrentFrameOffset + i];
                        // Likelihood
                        double BackLikelihood = 0.0;
                        double ForeLikelihood = 0.0;
                        for (int k = 0; k < BackModelNum; k++)
                        {
                            //if (GMMComponent[k].IVesselnessMean[1] < 255.0 * 0.3)
                            //{
                                double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelVesselness);
                                BackLikelihood += GMMLikelihood * BackPriorProbability;
                            //}
                        }
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                        {
                            //if (GMMComponent[k].IVesselnessMean[1] > 255.0 * 0.2)
                            //{
                                double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelVesselness);
                                ForeLikelihood += GMMLikelihood * ForePriorProbability;
                            //}
                        }

                        if (ForeLikelihood > BackLikelihood)
                            FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
                        else
                            FrameMask[CurrentFrameOffset + i] = Constants.LABEL_BACKGROUND;
                    }
                //})
            //);
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
                GraphCutWrap GraphCut = new GraphCutWrap(XNum, YNum, BufData, BufSmoothness, BufHSmoothness, BufVSmoothness, false);
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
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFrameOffset + i]);

            const int ScaleNum = 5;
            double[] ScaleArray = { 2.12, 2.72, 3.5, 4.0, 5.0 };
            ResponseMap map = new ResponseMap();
            double[] VesselnessMap = map.RunFrangiAndKrissianMethod2D(XNum, YNum, CurrentXraySlice, ScaleNum, ScaleArray);

            MorphologicalFilter FilteringProcessor = new MorphologicalFilter(XNum, YNum);
            FilteringProcessor.FType = MorphologicalFilter.FilterType.Median;
            double[] FrangiKrissianVesselenss = FilteringProcessor.RunFiltering(VesselnessMap);

            for (int i = 0; i < FramePixelNum; i++)
                FrameVesselness[CurrentFrameOffset + i] = FrangiKrissianVesselenss[i] * 255.0;
        }

        private void PostProcessingUsingCCL(int CurrentFrameIndex, int MinSize)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = FrameMask[CurrentFrameOffset + i];

            ConnectedComponentLabeling CCLProcessor = new ConnectedComponentLabeling(XNum, YNum);
            int LabelNum = CCLProcessor.RunCCL(CurrentXraySlice, MinSize, FramePixelNum);
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (CCLProcessor.OutputFrameMask[i] == Constants.LABEL_BACKGROUND)
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_BACKGROUND;
                else
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
            }
        }

        private double[] BuildDataEnergyArray(int CurrentFrameIndex)
        {
            const int LabelNum = 2;
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int CurrentFramePixelOffset = CurrentFrameIndex * FramePixelNum;

            // Weight normalization
            int BackPixelCnt = 0;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFramePixelOffset - FramePixelNum + i] == Constants.LABEL_BACKGROUND)
                    BackPixelCnt++;
            }
            double BackPriorProbability = Convert.ToDouble(BackPixelCnt) / Convert.ToDouble(FramePixelNum);
            double ForePriorProbability = 1.0 - BackPriorProbability;

            double[] DataCost = new double[FramePixelNum * LabelNum];
            DataCost.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFramePixelOffset + i]);
                double CurrentPixelVesselness = FrameVesselness[CurrentFramePixelOffset + i];
                // Likelihood
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelVesselness);
                    if (GMMComponent[k].IVesselnessMean[1] < 255.0 * 3)
                        DataCost[i * LabelNum] += GMMLikelihood * BackPriorProbability;
                }
                DataCost[i * LabelNum] = -Math.Log10(DataCost[i * LabelNum]);
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelVesselness);
                    //if (GMMComponent[k].IVesselnessMean[1] > 255.0 * 3)
                        DataCost[i * LabelNum + 1] += GMMLikelihood * ForePriorProbability;
                }
                DataCost[i * LabelNum + 1] = -Math.Log10(DataCost[i * LabelNum + 1]);
            }
            return DataCost;
        }

        private double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
            const double Lamda1 = 1.0, Lamda2 = 0.5;
            const double Sigma = 30.0;
            //int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int LabelNum = 2;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            double[] SmoothnessCost = new double[LabelNum * LabelNum];
            SmoothnessCost.Initialize();
            SmoothnessCost[1] = SmoothnessCost[2] = 1.0;

            // Horizontal 
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 1; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - 1]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    double VesselnessDifference = 0.0;// FrameVesselness[CurrentFrameOffset + CurrentPixelIndex] - FrameVesselness[CurrentFrameOffset + CurrentPixelIndex - 1];
                    double Difference = Math.Sqrt(IntensityDifference * IntensityDifference + VesselnessDifference * VesselnessDifference);
                    HSmoothness[CurrentPixelIndex - 1] = Lamda1 * (Math.Exp(-(Difference * Difference) / (2.0 * Sigma * Sigma)) + Lamda2);
                }
            }
            // Vertical
            for (int y = 1; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - XNum]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    double VesselnessDifference = 0.0;// FrameVesselness[CurrentFrameOffset + CurrentPixelIndex] - FrameVesselness[CurrentFrameOffset + CurrentPixelIndex - XNum];
                    double Difference = Math.Sqrt(IntensityDifference * IntensityDifference + VesselnessDifference * VesselnessDifference);
                    VSmoothness[CurrentPixelIndex - XNum] = Lamda1 * (Math.Exp(-(Difference * Difference) / (2.0 * Sigma * Sigma)) + Lamda2);
                }
            }
            return SmoothnessCost;
        }

        /*
        private double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
            const double Lamda = 1.0;
            const double Sigma = 50.0;
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int LabelNum = 2;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            double[] SmoothnessCost = new double[LabelNum * LabelNum];
            SmoothnessCost.Initialize();
            SmoothnessCost[1] = SmoothnessCost[2] = 1.0;

            // Horizontal 
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 1; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - 1]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    double VesselnessDifference = FrameVesselness[CurrentFrameOffset + CurrentPixelIndex] - FrameVesselness[CurrentFrameOffset + CurrentPixelIndex - 1];
                    double Difference = Math.Sqrt(IntensityDifference * IntensityDifference + VesselnessDifference * VesselnessDifference);
                    HSmoothness[CurrentPixelIndex - 1] = Lamda * Math.Exp(-(Difference * Difference) / (Sigma * Sigma));
                }
            }
            // Vertical
            for (int y = 1; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - XNum]);
                    double IntensityDifference = CurrentPixelIntensity - NeighborPixelIntensity;
                    double VesselnessDifference = FrameVesselness[CurrentFrameOffset + CurrentPixelIndex] - FrameVesselness[CurrentFrameOffset + CurrentPixelIndex - XNum];
                    double Difference = Math.Sqrt(IntensityDifference * IntensityDifference + VesselnessDifference * VesselnessDifference);
                    VSmoothness[CurrentPixelIndex - XNum] = Lamda * Math.Exp(-(Difference * Difference) / (Sigma * Sigma));
                }
            }
            return SmoothnessCost;
        }
         * */
    }
}
