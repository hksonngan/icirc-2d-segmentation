﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    //---------------------------------------------------------------------------
    /** @class SpatialColorGaussianModel
        @author Hyunna Lee
        @date 2013.11.07
        @brief GMM Model parameters for spatial and intensity components
    */
    //-------------------------------------------------------------------------
    class SpatialColorGaussianModel
    {
        public Vector SpatialMean;      ///< Mean of spatial component (x, y)
        public Matrix SpatialCoVar;     ///< Covariance matrix of spatial component (xx, xy; yx, yy)
        public double IntensityMean;    ///< Mean of intensity component
        public double IntensityVar;     ///< Covariance of intensity component
        public double Weight;           ///< Weight of this GMM component in the mixture model

        public SpatialColorGaussianModel()
        {
            SpatialMean = new Vector(2);
            SpatialCoVar = new Matrix(2, 2);
            IntensityMean = 128.0;
            IntensityVar = 0.0;
            Weight = 0.0;
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for bivariate
            @author Hyunna Lee
            @date 2013.11.07
            @param Spatial : Spatial component of the current instant 
            @return Probability of the current (spatial) instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(Vector Spatial)
        {
            Vector DifferenceVector = new Vector(2);
            DifferenceVector[0] = Spatial[0] - SpatialMean[0];
            DifferenceVector[1] = Spatial[1] - SpatialMean[1];
            Matrix InvCoVar = new Matrix(2, 2);
            InvCoVar = SpatialCoVar.Inverse();
            double det = SpatialCoVar.Determinant();
            double Difference = DifferenceVector[0] * (DifferenceVector[0] * InvCoVar[0, 0] + DifferenceVector[1] * InvCoVar[1, 0])
                              + DifferenceVector[1] * (DifferenceVector[0] * InvCoVar[0, 1] + DifferenceVector[1] * InvCoVar[1, 1]);
            return Math.Exp(-Difference / 2.0) / ((2.0 * Math.PI) * Math.Sqrt(det));
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for bivariate
            @author Hyunna Lee
            @date 2013.11.07
            @param SpatialX : Spatial X component of the current instant 
            @param SpatialY : Spatial Y component of the current instant 
            @return Probability of the current (spatial) instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(int SpatialX, int SpatialY)
        {
            Vector DifferenceVector = new Vector(2);
            DifferenceVector[0] = Convert.ToDouble(SpatialX) - SpatialMean[0];
            DifferenceVector[1] = Convert.ToDouble(SpatialY) - SpatialMean[1];
            Matrix InvCoVar = new Matrix(2, 2);
            InvCoVar = SpatialCoVar.Inverse();
            double det = SpatialCoVar.Determinant();
            double Difference = DifferenceVector[0] * (DifferenceVector[0] * InvCoVar[0, 0] + DifferenceVector[1] * InvCoVar[1, 0])
                              + DifferenceVector[1] * (DifferenceVector[0] * InvCoVar[0, 1] + DifferenceVector[1] * InvCoVar[1, 1]);
            return Math.Exp(-Difference / 2.0) / ((2.0 * Math.PI) * Math.Sqrt(det));
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for univariate
            @author Hyunna Lee
            @date 2013.11.07
            @param Spatial : Intensity component of the current instant 
            @return Probability of the current (intensity) instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(double Intensity)
        {
            double Difference = Intensity - IntensityMean;
            return Math.Exp(-(Difference * Difference) / (2.0 * IntensityVar)) / (Math.Sqrt(2.0 * Math.PI) * Math.Sqrt(IntensityVar));
        }
    }
}