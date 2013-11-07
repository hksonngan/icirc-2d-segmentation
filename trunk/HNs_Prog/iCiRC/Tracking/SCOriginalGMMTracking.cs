﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    public class SCOriginalGMMTracking : VesselTracking
    {
        int BackModelNum, ForeModelNum;
        SpatialColorGaussianModel[] GMMComponent;

        int XNum, YNum, FrameNum;
        ushort[] FrameIntensity;
        byte[] FrameMask;

        //---------------------------------------------------------------------------
        /** @brief Run a vessel tracking algorithm for a X-ray image sequence
            @author Hyunna Lee
            @date 2013.11.05
            @param paraXNum : the width of each frame
            @param paraYNum : the height of each frame
            @param paraZNum : the number of frames
            @param paraImageIntensity : the array of image intensity
            @return the array of labeling mask
            @todo To implement the whole algorithm
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

            // For the first frame
            InitializeGMMModel();

            // For each frame 
            for (int f = 1; f < FrameNum; f++)
            {

            }

            return FrameMask;
        }

        private void InitializeGMMModel()
        {
            BackModelNum = 15;
            ForeModelNum = 15;
            int TotalModelNum = BackModelNum + ForeModelNum;
            GMMComponent = new SpatialColorGaussianModel[TotalModelNum];

            // Initial segmentation using thresholding
            int FramePixelNum = XNum * YNum;
            const ushort VesselIntensityThresholdValue = 128;
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (FrameIntensity[i] < VesselIntensityThresholdValue)
                    FrameMask[i] = 0xff;
            }

            // 1st E-step
            double[][] AssignmentProbability = new double[FramePixelNum][];
            for (int i = 0; i < FramePixelNum; i++)
            {
                AssignmentProbability[i] = new double[TotalModelNum];
                AssignmentProbability[i].Initialize();
                for (int k = 0; k < BackModelNum; k++)
                {
                    if (FrameMask[i] == 0x00)
                        AssignmentProbability[i][k] = 1.0 / Convert.ToDouble(BackModelNum);
                }
                for (int k = BackModelNum; k < TotalModelNum; k++)
                {
                    if (FrameMask[i] == 0xff)
                        AssignmentProbability[i][k] = 1.0 / Convert.ToDouble(ForeModelNum);
                }
            }
            // 1st M-step
            MaximizationStep(0, AssignmentProbability);

            const int EMIterNum = 30;
            for (int iter = 1; iter < EMIterNum; iter++)
            {
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

                MaximizationStep(0, AssignmentProbability);
            }

        }
        
        private void MaximizationStep(int CurrentFrameIndex, double[][] AssignmentProbability)
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
    }
}