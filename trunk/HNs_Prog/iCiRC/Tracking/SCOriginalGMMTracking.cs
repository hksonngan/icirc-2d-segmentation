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
            BackModelNum = 15;
            ForeModelNum = 15;
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

            // For the first frame (Post-updating)
            int TotalModelNum = BackModelNum + ForeModelNum;
            double[][] AssignmentProbability = InitialExpectationStep();
            MaximizationStepInPostUpdating(0, AssignmentProbability);
            const int EMIterNum = 5;
            for (int iter = 1; iter < EMIterNum; iter++)
            {
                ExpectationStepInPostUpdating(ref AssignmentProbability);
                MaximizationStepInPostUpdating(0, AssignmentProbability);
            }

            // For each frame 
            for (int f = 1; f < FrameNum; f++)
            {
                // Weight calibration (Fore/back seperated GMM -> Global GMM)
                GMMComponentWeightCalibration(f - 1);

                // Pre-updating EM
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    ExpectationStepInPreUpdating(ref AssignmentProbability);
                    MaximizationStepInPreUpdating(f, AssignmentProbability);
                }

                // Segmentation
                SegmentationUsingGraphCut(f);

                // Post-undating EM
                for (int iter = 0; iter < EMIterNum; iter++)
                {
                    ExpectationStepInPostUpdating(ref AssignmentProbability);
                    MaximizationStepInPostUpdating(f, AssignmentProbability);
                }
            }

            return FrameMask;
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
            int TotalModelNum = BackModelNum + ForeModelNum;
            int FramePixelNum = XNum * YNum;
            int LabelNum = 2;
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
                    double BackLikelihood = 0.0;
                    double ForeLikelihood = 0.0;
                    for (int k = 0; k < BackModelNum; k++)
                        BackLikelihood += GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                    for (int k = BackModelNum; k < TotalModelNum; k++)
                        ForeLikelihood += GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(x, y) * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);

                    DataCost[CurrentPixelIndex * LabelNum] = BackLikelihood;       
                    DataCost[CurrentPixelIndex * LabelNum + 1] = ForeLikelihood;   
                }
            }
            return DataCost;
        }

        double[] BuildSmoothnessEnergyArray(int CurrentFrameIndex, ref double[] HSmoothness, ref double[] VSmoothness)
        {
            const double Sigma = 80.0;
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
        /** @brief Calibration of the Gaussian component weights based on the prior probability of the class
            @author Hyunna Lee
            @date 2013.11.08
            @param PreviousFrameIndex : the index of the previous frame
        */
        //-------------------------------------------------------------------------
        void GMMComponentWeightCalibration(int PreviousFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int BackgroundPixelCnt = 0;
            int PreviousFramePixelOffset = PreviousFrameIndex * FramePixelNum;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameMask[PreviousFramePixelOffset + i] == Constants.LABEL_BACKGROUND)
                    BackgroundPixelCnt++;
            }
            double BackgroundPriorProbability = Convert.ToDouble(BackgroundPixelCnt) / Convert.ToDouble(FramePixelNum);
            double ForegroundPriorProbability = 1.0 - BackgroundPriorProbability;

            int TotalModelNum = BackModelNum + ForeModelNum;
            for (int k = 0; k < BackModelNum; k++)
                GMMComponent[k].Weight *= BackgroundPriorProbability;
            for (int k = BackModelNum; k < TotalModelNum; k++)
                GMMComponent[k].Weight *= ForegroundPriorProbability;
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

            // Initial segmentation using thresholding
            int FramePixelNum = XNum * YNum;
            const ushort VesselIntensityThresholdValue = 128;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameIntensity[i] < VesselIntensityThresholdValue)
                    FrameMask[i] = Constants.LABEL_FOREGROUND;
            }

            // 1st E-step
            double[][] AssignmentProbability = new double[FramePixelNum][];
            for (int i = 0; i < FramePixelNum; i++)
            {
                AssignmentProbability[i] = new double[TotalModelNum];
                AssignmentProbability[i].Initialize();
                for (int k = 0; k < BackModelNum; k++)
                {
                    if (FrameMask[i] == Constants.LABEL_BACKGROUND)
                        AssignmentProbability[i][k] = 1.0 / Convert.ToDouble(BackModelNum);
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    if (FrameMask[i] == Constants.LABEL_FOREGROUND)
                        AssignmentProbability[i][k] = 1.0 / Convert.ToDouble(ForeModelNum);
                }
            }
            return AssignmentProbability;
        }

        //---------------------------------------------------------------------------
        /** @brief For each frame, E-step of EM algorithm during Pre-updating  
            @author Hyunna Lee
            @date 2013.11.08
            @para AssignmentProbability : posterior probability for each pixel
            @return uniformly assigned 
        */
        //-------------------------------------------------------------------------
        void ExpectationStepInPreUpdating(ref double[][] AssignmentProbability)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            // E-step: Update AssignmentProbability
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double[] GMMProbability = new double[TotalModelNum];
                    GMMProbability.Initialize();
                    double SumGMMProbability = 0.0;
                    Vector CurrrentPixelSpatial = new Vector(2);
                    CurrrentPixelSpatial[0] = Convert.ToDouble(x);
                    CurrrentPixelSpatial[1] = Convert.ToDouble(y);
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentPixelIndex]);
                    for (int k = 0; k < TotalModelNum; k++)
                    {
                        GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrrentPixelSpatial)
                                            * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                        SumGMMProbability += GMMProbability[k];
                    }
                    for (int k = 0; k < TotalModelNum; k++)
                        AssignmentProbability[CurrentPixelIndex][k] = GMMProbability[k] / SumGMMProbability;
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
        void ExpectationStepInPostUpdating(ref double[][] AssignmentProbability)
        {
            int TotalModelNum = BackModelNum + ForeModelNum;
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    double[] GMMProbability = new double[TotalModelNum];
                    GMMProbability.Initialize();
                    double SumGMMProbability = 0.0;
                    Vector CurrrentPixelSpatial = new Vector(2);
                    CurrrentPixelSpatial[0] = Convert.ToDouble(x);
                    CurrrentPixelSpatial[1] = Convert.ToDouble(y);
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentPixelIndex]);

                    if (FrameMask[CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                    {
                        for (int k = 0; k < BackModelNum; k++)
                        {
                            GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrrentPixelSpatial)
                                                * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                            SumGMMProbability += GMMProbability[k];
                        }
                        for (int k = 0; k < BackModelNum; k++)
                            AssignmentProbability[CurrentPixelIndex][k] = GMMProbability[k] / SumGMMProbability;
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                            AssignmentProbability[CurrentPixelIndex][k] = 0.0;
                    }
                    else if (FrameMask[CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                    {
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                        {
                            GMMProbability[k] = GMMComponent[k].Weight * GMMComponent[k].GetGaussianProbability(CurrrentPixelSpatial)
                                                * GMMComponent[k].GetGaussianProbability(CurrentPixelIntensity);
                            SumGMMProbability += GMMProbability[k];
                        }
                        for (int k = 0; k < BackModelNum; k++)
                            AssignmentProbability[CurrentPixelIndex][k] = 0.0;
                        for (int k = BackModelNum; k < TotalModelNum; k++)
                            AssignmentProbability[CurrentPixelIndex][k] = GMMProbability[k] / SumGMMProbability;
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
        private void MaximizationStepInPreUpdating(int CurrentFrameIndex, double[][] AssignmentProbability)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex + FramePixelNum;
            int TotalModelNum = BackModelNum + ForeModelNum;
            double[] SumProbability = new double[TotalModelNum];
            Vector[] SumSpatial = new Vector[TotalModelNum];
            double[] SumIntensity = new double[TotalModelNum];
            SumProbability.Initialize();
            double TotalSumAssignmentProbability = 0.0;
            SumIntensity.Initialize();

            for (int k = 0; k < TotalModelNum; k++)
            {
                // Compute the Mean 
                SumSpatial[k] = new Vector(2);
                SumSpatial[k][0] = SumSpatial[k][1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        SumProbability[k] += AssignmentProbability[CurrentPixelIndex][k];
                        SumSpatial[k][0] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                        SumSpatial[k][1] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                        SumIntensity[k] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                    }
                }
                GMMComponent[k].SpatialMean[0] = SumSpatial[k][0] / SumProbability[k];
                GMMComponent[k].SpatialMean[1] = SumSpatial[k][1] / SumProbability[k];

                TotalSumAssignmentProbability += SumProbability[k];
            }

            Matrix[] SumSpatialVariance = new Matrix[TotalModelNum];
            double[] SumIntensityVariance = new double[TotalModelNum];
            SumIntensityVariance.Initialize();
            for (int k = 0; k < TotalModelNum; k++)
            {
                // Compute the Variance
                SumSpatialVariance[k] = new Matrix(2, 2);
                SumSpatialVariance[k][0, 0] = SumSpatialVariance[k][0, 1] = SumSpatialVariance[k][1, 0] = SumSpatialVariance[k][1, 1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        Vector SpatialDifference = new Vector(2);
                        SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                        SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                        double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                        SumSpatialVariance[k][0, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                        SumSpatialVariance[k][0, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                        SumSpatialVariance[k][1, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                        SumSpatialVariance[k][1, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                        SumIntensityVariance[k] += AssignmentProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
                    }
                }
                GMMComponent[k].SpatialCoVar[0, 0] = SumSpatialVariance[k][0, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[0, 1] = SumSpatialVariance[k][0, 1] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 0] = SumSpatialVariance[k][1, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 1] = SumSpatialVariance[k][1, 1] / SumProbability[k];
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumProbability[k];

                // Compute the Weight
                GMMComponent[k].Weight = SumProbability[k] / TotalSumAssignmentProbability;
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
        private void MaximizationStepInPostUpdating(int CurrentFrameIndex, double[][] AssignmentProbability)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex + FramePixelNum;
            int TotalModelNum = BackModelNum + ForeModelNum;
            double[] SumProbability = new double[TotalModelNum];
            Vector[] SumSpatial = new Vector[TotalModelNum];
            double[] SumIntensity = new double[TotalModelNum];
            SumProbability.Initialize();
            SumIntensity.Initialize();
            double TotalSumAssignmentProbability = 0.0;
            Matrix[] SumSpatialVariance = new Matrix[TotalModelNum];
            double[] SumIntensityVariance = new double[TotalModelNum];
            SumIntensityVariance.Initialize();

            // For background GMM Model
            for (int k = 0; k < BackModelNum; k++)
            {
                // Compute the Mean 
                SumSpatial[k] = new Vector(2);
                SumSpatial[k][0] = SumSpatial[k][1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                        {
                            SumProbability[k] += AssignmentProbability[CurrentPixelIndex][k];
                            SumSpatial[k][0] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                            SumIntensity[k] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                        }
                    }
                }
                GMMComponent[k].SpatialMean[0] = SumSpatial[k][0] / SumProbability[k];
                GMMComponent[k].SpatialMean[1] = SumSpatial[k][1] / SumProbability[k];

                TotalSumAssignmentProbability += SumProbability[k];
            }
            for (int k = 0; k < BackModelNum; k++)
            {
                // Compute the Variance
                SumSpatialVariance[k] = new Matrix(2, 2);
                SumSpatialVariance[k][0, 0] = SumSpatialVariance[k][0, 1] = SumSpatialVariance[k][1, 0] = SumSpatialVariance[k][1, 1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                        {
                            Vector SpatialDifference = new Vector(2);
                            SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                            SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                            double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                            SumSpatialVariance[k][0, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += AssignmentProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
                        }
                    }
                }
                GMMComponent[k].SpatialCoVar[0, 0] = SumSpatialVariance[k][0, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[0, 1] = SumSpatialVariance[k][0, 1] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 0] = SumSpatialVariance[k][1, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 1] = SumSpatialVariance[k][1, 1] / SumProbability[k];
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumProbability[k];

                // Compute the Weight
                GMMComponent[k].Weight = SumProbability[k] / TotalSumAssignmentProbability;
            }

            // For foreground GMM model
            TotalSumAssignmentProbability = 0.0;
            for (int k = BackModelNum; k < TotalModelNum; k++)
            {
                // Compute the Mean 
                SumSpatial[k] = new Vector(2);
                SumSpatial[k][0] = SumSpatial[k][1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                        {
                            SumProbability[k] += AssignmentProbability[CurrentPixelIndex][k];
                            SumSpatial[k][0] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(x);
                            SumSpatial[k][1] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(y);
                            SumIntensity[k] += AssignmentProbability[CurrentPixelIndex][k] * Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]);
                        }
                    }
                }
                GMMComponent[k].SpatialMean[0] = SumSpatial[k][0] / SumProbability[k];
                GMMComponent[k].SpatialMean[1] = SumSpatial[k][1] / SumProbability[k];

                TotalSumAssignmentProbability += SumProbability[k];
            }
            for (int k = BackModelNum; k < TotalModelNum; k++)
            {
                // Compute the Variance
                SumSpatialVariance[k] = new Matrix(2, 2);
                SumSpatialVariance[k][0, 0] = SumSpatialVariance[k][0, 1] = SumSpatialVariance[k][1, 0] = SumSpatialVariance[k][1, 1] = 0.0;
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        if (FrameMask[CurrentFrameOffset + CurrentPixelIndex] == Constants.LABEL_FOREGROUND)
                        {
                            Vector SpatialDifference = new Vector(2);
                            SpatialDifference[0] = Convert.ToDouble(x) - GMMComponent[k].SpatialMean[0];
                            SpatialDifference[1] = Convert.ToDouble(y) - GMMComponent[k].SpatialMean[1];
                            double IntensityDifference = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + CurrentPixelIndex]) - GMMComponent[k].IntensityMean;
                            SumSpatialVariance[k][0, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[0];
                            SumSpatialVariance[k][0, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[0] * SpatialDifference[1];
                            SumSpatialVariance[k][1, 0] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[0];
                            SumSpatialVariance[k][1, 1] += AssignmentProbability[CurrentPixelIndex][k] * SpatialDifference[1] * SpatialDifference[1];
                            SumIntensityVariance[k] += AssignmentProbability[CurrentPixelIndex][k] * IntensityDifference * IntensityDifference;
                        }
                    }
                }
                GMMComponent[k].SpatialCoVar[0, 0] = SumSpatialVariance[k][0, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[0, 1] = SumSpatialVariance[k][0, 1] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 0] = SumSpatialVariance[k][1, 0] / SumProbability[k];
                GMMComponent[k].SpatialCoVar[1, 1] = SumSpatialVariance[k][1, 1] / SumProbability[k];
                GMMComponent[k].IntensityVar = SumIntensityVariance[k] / SumProbability[k];

                // Compute the Weight
                GMMComponent[k].Weight = SumProbability[k] / TotalSumAssignmentProbability;
            }
        }
    }
}
