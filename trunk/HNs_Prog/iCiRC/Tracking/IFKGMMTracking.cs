using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedMRF;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC.Tracking
{
    public class IFKGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        IFKGaussianModel[] GMMComponent;
        double[] FrameFrangi;
        double[] FrameKrissian;

        public IFKGMMTracking()
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
            FrameFrangi = new double[TotalPixelNum];
            FrameFrangi.Initialize();
            FrameKrissian = new double[TotalPixelNum];
            FrameKrissian.Initialize();

            const int EMIterNum = 5;
            const int StartFrameIndex = 30;

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, 5);
            winProgress.Show();

            // For the first frame (Post-updating)
            ComputeVesselness(StartFrameIndex);
            SegmentationUsingVesselnessThresholding(StartFrameIndex);
            //PostProcessingUsingSRG(StartFrameIndex);
            PostProcessingUsingCCL(StartFrameIndex, 40);
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
                //SegmentationUsingGraphCut(f);
                //PostProcessingUsingSRG(f);
                PostProcessingUsingCCL(f, 40);
                //CenterlineExtraction(f);
                
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
            const double VesselnessThreshold = 255.0 * 0.3;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (Math.Max(FrameFrangi[CurrentFramePixelOffset + i], FrameKrissian[CurrentFramePixelOffset + i]) >= VesselnessThreshold)
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
                CurrentSliceFeatureMask[i] = new Vector(3);
                CurrentSliceFeatureMask[i][0] = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                CurrentSliceFeatureMask[i][1] = FrameFrangi[CurrentFrameOffset + i];
                CurrentSliceFeatureMask[i][2] = FrameKrissian[CurrentFrameOffset + i];
            }

            // K-means clustering
            KmeansClustering BackClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_BACKGROUND);
            BackModelNum = BackClustering.RunClustering(1, CurrentSliceFeatureMask, 50.0);
            KmeansClustering ForeClustering = new KmeansClustering(XNum, YNum, CurrentSliceFrameMask, Constants.LABEL_FOREGROUND);
            ForeModelNum = ForeClustering.RunClustering(1, CurrentSliceFeatureMask, 50.0);
            int TotalModelNum = BackModelNum + ForeModelNum;
            GMMComponent = new IFKGaussianModel[TotalModelNum];
            for (int i = 0; i < TotalModelNum; i++)
                GMMComponent[i] = new IFKGaussianModel();
            for (int i = 0; i < BackModelNum; i++)
                GMMComponent[i].IsBackComponent = true;
            for (int i = BackModelNum; i < TotalModelNum; i++)
                GMMComponent[i].IsBackComponent = false;

            // 1st E-step
            double[,] PosteriorProbability = new double[FramePixelNum, TotalModelNum];
            PosteriorProbability.Initialize();
            /*
            for (int i = 0; i < FramePixelNum; i++)
            {
                // For each pixel z, compute the posterior probability, P(k|z)
                if (CurrentSliceFrameMask[i] == Constants.LABEL_BACKGROUND)
                    PosteriorProbability[i, BackClustering.ClusterLabel[i] - 1] = 1.0;
                else if (CurrentSliceFrameMask[i] == Constants.LABEL_FOREGROUND)
                    PosteriorProbability[i, BackModelNum + ForeClustering.ClusterLabel[i] - 1] = 1.0;
            }
            */
            for (int i = 0; i < FramePixelNum; i++)
            {
                // For each pixel z, compute the posterior probability, P(k|z)
                if (CurrentSliceFrameMask[i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                        PosteriorProbability[i, k] = 1.0 / Convert.ToDouble(BackModelNum);
                }
                else if (CurrentSliceFrameMask[i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        PosteriorProbability[i, k] = 1.0 / Convert.ToDouble(ForeModelNum);
                }
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
                double CurrentPixelFrangi = FrameFrangi[CurrentFrameOffset + i];
                double CurrentPixelKrissian = FrameKrissian[CurrentFrameOffset + i];
                for (int k = 0; k < TotalModelNum; k++)
                    GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelFrangi, CurrentPixelKrissian);

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
                SumIVesselness[k] = new Vector(3);
                SumIVesselness[k][0] = SumIVesselness[k][1] = SumIVesselness[k][2] = 0.0;
                SumIVesselnessVariance[k] = new Matrix(3, 3);
                SumIVesselnessVariance[k][0, 0] = SumIVesselnessVariance[k][0, 1] = SumIVesselnessVariance[k][0, 2] = 0.0;
                SumIVesselnessVariance[k][1, 0] = SumIVesselnessVariance[k][1, 1] = SumIVesselnessVariance[k][1, 2] = 0.0;
                SumIVesselnessVariance[k][2, 0] = SumIVesselnessVariance[k][2, 1] = SumIVesselnessVariance[k][2, 2] = 0.0;
            }

            // Compute the sum of posterior and intensity and vesselness
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelFrangi = FrameFrangi[CurrentFrameOffset + i];
                double CurrentPixelKrissian = FrameKrissian[CurrentFrameOffset + i];
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIVesselness[k][0] += PosteriorProbability[i, k] * CurrentPixelIntensity;
                        SumIVesselness[k][1] += PosteriorProbability[i, k] * CurrentPixelFrangi;
                        SumIVesselness[k][2] += PosteriorProbability[i, k] * CurrentPixelKrissian;
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        SumPosterior[k] += PosteriorProbability[i, k];
                        SumIVesselness[k][0] += PosteriorProbability[i, k] * CurrentPixelIntensity;
                        SumIVesselness[k][1] += PosteriorProbability[i, k] * CurrentPixelFrangi;
                        SumIVesselness[k][2] += PosteriorProbability[i, k] * CurrentPixelKrissian;
                    }
                }
            }
            // Compute the mean of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].IVesselnessMean[0] = SumIVesselness[k][0] / SumPosterior[k];
                GMMComponent[k].IVesselnessMean[1] = SumIVesselness[k][1] / SumPosterior[k];
                GMMComponent[k].IVesselnessMean[2] = SumIVesselness[k][2] / SumPosterior[k];
            }

            // Compute the sum of variance of spatial and intensity
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelFrangi = FrameFrangi[CurrentFrameOffset + i];
                double CurrentPixelKrissian = FrameKrissian[CurrentFrameOffset + i];
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_BACKGROUND)
                {
                    for (int k = 0; k < BackModelNum; k++)
                    {
                        Vector IVesselnessDifference = new Vector(3);
                        IVesselnessDifference[0] = CurrentPixelIntensity - GMMComponent[k].IVesselnessMean[0];
                        IVesselnessDifference[1] = CurrentPixelFrangi - GMMComponent[k].IVesselnessMean[1];
                        IVesselnessDifference[2] = CurrentPixelKrissian - GMMComponent[k].IVesselnessMean[2];
                        SumIVesselnessVariance[k][0, 0] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][0, 1] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][0, 2] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[2];
                        SumIVesselnessVariance[k][1, 0] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][1, 1] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][1, 2] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[2];
                        SumIVesselnessVariance[k][2, 0] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][2, 1] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][2, 2] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[2];
                    }
                }
                else if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                    {
                        Vector IVesselnessDifference = new Vector(3);
                        IVesselnessDifference[0] = CurrentPixelIntensity - GMMComponent[k].IVesselnessMean[0];
                        IVesselnessDifference[1] = CurrentPixelFrangi - GMMComponent[k].IVesselnessMean[1];
                        IVesselnessDifference[2] = CurrentPixelKrissian - GMMComponent[k].IVesselnessMean[2];
                        SumIVesselnessVariance[k][0, 0] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][0, 1] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][0, 2] += PosteriorProbability[i, k] * IVesselnessDifference[0] * IVesselnessDifference[2];
                        SumIVesselnessVariance[k][1, 0] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][1, 1] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][1, 2] += PosteriorProbability[i, k] * IVesselnessDifference[1] * IVesselnessDifference[2];
                        SumIVesselnessVariance[k][2, 0] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[0];
                        SumIVesselnessVariance[k][2, 1] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[1];
                        SumIVesselnessVariance[k][2, 2] += PosteriorProbability[i, k] * IVesselnessDifference[2] * IVesselnessDifference[2];
                    }
                }
            }
            // Compute the variance of each Gaussian component
            for (int k = 0; k < TotalModelNum; k++)
            {
                GMMComponent[k].IVesselnessCoVar[0, 0] = SumIVesselnessVariance[k][0, 0] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[0, 1] = SumIVesselnessVariance[k][0, 1] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[0, 2] = SumIVesselnessVariance[k][0, 2] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[1, 0] = SumIVesselnessVariance[k][1, 0] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[1, 1] = SumIVesselnessVariance[k][1, 1] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[1, 2] = SumIVesselnessVariance[k][1, 2] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[2, 0] = SumIVesselnessVariance[k][2, 0] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[2, 1] = SumIVesselnessVariance[k][2, 1] / SumPosterior[k];
                GMMComponent[k].IVesselnessCoVar[2, 2] = SumIVesselnessVariance[k][2, 2] / SumPosterior[k];
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

            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelFrangi = FrameFrangi[CurrentFrameOffset + i];
                double CurrentPixelKrissian = FrameKrissian[CurrentFrameOffset + i];
                // Likelihood
                double BackLikelihood = 0.0;
                double ForeLikelihood = 0.0;
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelFrangi, CurrentPixelKrissian);
                    BackLikelihood += GMMLikelihood * BackPriorProbability;
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    CurrentPixelIntensity = Math.Max(CurrentPixelIntensity, GMMComponent[k].IVesselnessMean[0]);
                    CurrentPixelFrangi = Math.Min(CurrentPixelFrangi, GMMComponent[k].IVesselnessMean[1]);
                    CurrentPixelKrissian = Math.Min(CurrentPixelKrissian, GMMComponent[k].IVesselnessMean[2]);
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelFrangi, CurrentPixelKrissian);
                    ForeLikelihood += GMMLikelihood * ForePriorProbability;
                }

                if (ForeLikelihood > BackLikelihood)
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
                else
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_BACKGROUND;
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
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFrameOffset + i]);

            const int ScaleNum = 5;
            double[] ScaleArray = { 2.12, 2.72, 3.5, 4.0, 5.0 };
            ResponseMap map = new ResponseMap();
            double[] FrangiVesselnessMap = map.RunFrangiMethod2D(XNum, YNum, CurrentXraySlice, ScaleNum, ScaleArray);
            double[] KrissianVesselnessMap = map.RunKrissianModelMethod2D(XNum, YNum, CurrentXraySlice, ScaleNum, ScaleArray);
            //double[] VesselnessMap = map.RunFrangiAndKrissianMethod2D(XNum, YNum, CurrentXraySlice, ScaleNum, ScaleArray);

            //MorphologicalFilter FilteringProcessor = new MorphologicalFilter(XNum, YNum);
            //FilteringProcessor.FType = MorphologicalFilter.FilterType.Median;
            //double[] FrangiKrissianVesselenss = FilteringProcessor.RunFiltering(VesselnessMap);

            for (int i = 0; i < FramePixelNum; i++)
            {
                FrameFrangi[CurrentFrameOffset + i] = FrangiVesselnessMap[i] * 255.0;
                FrameKrissian[CurrentFrameOffset + i] = KrissianVesselnessMap[i] * 255.0;
            }
        }

        private void PostProcessingUsingCCL(int CurrentFrameIndex, int MinSize)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = FrameMask[CurrentFrameOffset + i];

            // CCL
            ConnectedComponentLabeling CCLProcessor = new ConnectedComponentLabeling(XNum, YNum);
            int LabelNum = CCLProcessor.RunCCL(CurrentXraySlice, MinSize, FramePixelNum);
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (CCLProcessor.OutputFrameMask[i] == Constants.LABEL_BACKGROUND)
                {
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_BACKGROUND;
                    CurrentXraySlice[i] = Constants.LABEL_FOREGROUND;
                }
                else
                {
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
                    CurrentXraySlice[i] = Constants.LABEL_BACKGROUND;
                }
            }

            // Hole-filling
            LabelNum = CCLProcessor.RunCCL(CurrentXraySlice, 0, MinSize);
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (CCLProcessor.OutputFrameMask[i] != Constants.LABEL_BACKGROUND)
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
            }
        }

        private void PostProcessingUsingSRG(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(FrameIntensity[CurrentFrameOffset + i]);

            int[] NeiborOffset = new int[8];
            NeiborOffset[0] = -1;
            NeiborOffset[1] =  1;
            NeiborOffset[2] = -XNum;
            NeiborOffset[3] =  XNum;
            NeiborOffset[4] = -XNum - 1;
            NeiborOffset[5] = -XNum + 1;
            NeiborOffset[6] =  XNum - 1;
            NeiborOffset[7] =  XNum + 1;
            bool[] NeighborCheck = new bool[8];

            Filters[] GaussianDifferenceGradient = new Filters[2];
            for (int i = 0; i < 2; i++)
                GaussianDifferenceGradient[i] = new Filters();
            GaussianDifferenceGradient[0].GenerateGaussianGradientFilter2D(0.7, 7, 0);
            GaussianDifferenceGradient[1].GenerateGaussianGradientFilter2D(0.7, 7, 1);

            BallFilter Ball = new BallFilter(2.5);
            int BallFilterSize = Ball.GetInsidePoint() + Ball.GetBoundaryPoint();

            int ForegroundSize = 0;
            int ForegroundIntensitySum = 0;
            Queue<int> CandidatePointQueue = new Queue<int>();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[CurrentFrameOffset + i] == Constants.LABEL_FOREGROUND)
                {
                    CandidatePointQueue.Enqueue(i);
                    ForegroundSize++;
                    ForegroundIntensitySum += CurrentXraySlice[i];
                }
            }
            double ForegroundIntensity = Convert.ToDouble(ForegroundIntensitySum) / Convert.ToDouble(ForegroundSize);

            while(CandidatePointQueue.Count > 0)
            {
                int CurrentPixelIndex = CandidatePointQueue.Dequeue();
                int CurrentPixelIndexX = CurrentPixelIndex % XNum;
                int CurrentPixelIndexY = CurrentPixelIndex / XNum;

                for (int i = 0; i < 8; i++)
                    NeighborCheck[i] = true;
                if (CurrentPixelIndexX <= 0)
                    NeighborCheck[0] = false;   // Left
                if (CurrentPixelIndexX >= XNum - 1)
                    NeighborCheck[1] = false;   // Right
                if (CurrentPixelIndexY <= 0)
                    NeighborCheck[2] = false;   // Top
                if (CurrentPixelIndexY >= YNum - 1)
                    NeighborCheck[3] = false;   // Bottom
                if (!NeighborCheck[2] || !NeighborCheck[0])
                    NeighborCheck[4] = false;   // Top & Left
                if (!NeighborCheck[2] || !NeighborCheck[1])
                    NeighborCheck[5] = false;   // Top & Right
                if (!NeighborCheck[3] || !NeighborCheck[0])
                    NeighborCheck[6] = false;   // Bottom & Left
                if (!NeighborCheck[3] || !NeighborCheck[1])
                    NeighborCheck[7] = false;   // Bottom & Right

                for (int j = 0; j < 8; j++)
                {
                    if (NeighborCheck[j])
                    {
                        int NeiborPixelIndex = CurrentPixelIndex + NeiborOffset[j];
                        if (FrameMask[CurrentFrameOffset + NeiborPixelIndex] == Constants.LABEL_BACKGROUND)
                        {
                            Vector GradientVector = new Vector(2);
                            GradientVector[0] = GaussianDifferenceGradient[0].Run2D(XNum, YNum, CurrentXraySlice, NeiborPixelIndex);
                            GradientVector[1] = GaussianDifferenceGradient[1].Run2D(XNum, YNum, CurrentXraySlice, NeiborPixelIndex);
                            double GradientDifference = GradientVector.Norm();

                            //Vector NeighborDifference = new Vector(3);
                            //NeighborDifference[0] = ForegroundIntensity - Convert.ToDouble(FrameIntensity[CurrentFrameOffset + NeiborPixelIndex]);
                            //NeighborDifference[1] = 0.0;// FrameFrangi[CurrentFrameOffset + CurrentPixelIndex] - FrameFrangi[CurrentFrameOffset + NeiborPixelIndex];
                            //NeighborDifference[2] = 0.0;// FrameKrissian[CurrentFrameOffset + CurrentPixelIndex] - FrameKrissian[CurrentFrameOffset + NeiborPixelIndex];
                            //double Difference = NeighborDifference.Norm();
                            if (Convert.ToDouble(CurrentXraySlice[NeiborPixelIndex]) < ForegroundIntensity + 10.0 
                                && GradientDifference < 5.0)
                            {
                                int NeighborVesselNum = Ball.GetInsideObject(NeiborPixelIndex, XNum, YNum, CurrentXraySlice);
                                NeighborVesselNum += Ball.GetBoundaryObject(NeiborPixelIndex, XNum, YNum, CurrentXraySlice);
                                if (NeighborVesselNum > BallFilterSize / 2)
                                {
                                    FrameMask[CurrentFrameOffset + NeiborPixelIndex] = Constants.LABEL_FOREGROUND;
                                    CandidatePointQueue.Enqueue(NeiborPixelIndex);
                                }
                            }
                        }
                    }
                }
            }

            //for (int i = 0; i < FramePixelNum; i++)
            //    FrameMask[CurrentFrameOffset + i] = CurrentXraySlice[i];
        }

        private void CenterlineExtraction(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            byte[] CurrentXraySlice = new byte[FramePixelNum];
            for (int i = 0; i < FramePixelNum; i++)
                CurrentXraySlice[i] = FrameMask[CurrentFrameOffset + i];

            Skeletonization ThinningProcessor = new Skeletonization(XNum, YNum);
            byte[] ThinningMask = ThinningProcessor.RunSkeletonization(CurrentXraySlice);
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (ThinningMask[i] == Constants.LABEL_FOREGROUND)
                    FrameMask[CurrentFrameOffset + i] = 0x01;
            }
        }

        private double[] BuildDataEnergyArray(int CurrentFrameIndex)
        {
            const int LabelNum = 2;
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

            double[] DataCost = new double[FramePixelNum * LabelNum];
            DataCost.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                double CurrentPixelFrangi = FrameFrangi[CurrentFrameOffset + i];
                double CurrentPixelKrissian = FrameKrissian[CurrentFrameOffset + i];
                // Likelihood
                for (int k = 0; k < BackModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelFrangi, CurrentPixelKrissian);
                    DataCost[i * LabelNum] += GMMLikelihood * BackPriorProbability;
                }
                DataCost[i * LabelNum] = -Math.Log10(DataCost[i * LabelNum]);
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    double GMMLikelihood = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity, CurrentPixelFrangi, CurrentPixelKrissian);
                    DataCost[i * LabelNum + 1] += GMMLikelihood * ForePriorProbability;
                }
                DataCost[i * LabelNum + 1] = -Math.Log10(DataCost[i * LabelNum + 1]);
            }
            return DataCost;
        }

        private double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
            const double Lamda1 = 3.0, Lamda2 = 0.0;
            const double Sigma = 50.0;
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
                    //double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    //double NeighborPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - 1]);
                    Vector NeighborDifference = new Vector(3);
                    NeighborDifference[0] = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - 1]);
                    NeighborDifference[1] = 0.0;// (FrameFrangi[CurrentFrameOffset + CurrentPixelIndex] - FrameFrangi[CurrentFrameOffset + CurrentPixelIndex - 1]) / 2.0;
                    NeighborDifference[2] = 0.0;// (FrameKrissian[CurrentFrameOffset + CurrentPixelIndex] - FrameKrissian[CurrentFrameOffset + CurrentPixelIndex - 1]) / 2.0;
                    double Difference = NeighborDifference.Norm();
                    HSmoothness[CurrentPixelIndex - 1] = Lamda1 * (Math.Exp(-(Difference * Difference) / (2.0 * Sigma * Sigma)) + Lamda2);
                }
            }
            // Vertical
            for (int y = 1; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    Vector NeighborDifference = new Vector(3);
                    NeighborDifference[0] = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex - XNum]);
                    NeighborDifference[1] = 0.0;// (FrameFrangi[CurrentFrameOffset + CurrentPixelIndex] - FrameFrangi[CurrentFrameOffset + CurrentPixelIndex - XNum]) / 2.0;
                    NeighborDifference[2] = 0.0;// (FrameKrissian[CurrentFrameOffset + CurrentPixelIndex] - FrameKrissian[CurrentFrameOffset + CurrentPixelIndex - XNum]) / 2.0; ;
                    double Difference = NeighborDifference.Norm();
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
