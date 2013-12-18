using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC.Tracking
{
    //---------------------------------------------------------------------------
    /** @class IntensityGaussianModel
        @author Hyunna Lee
        @date 2013.11.19
        @brief GMM Model parameters for intensity component
    */
    //-------------------------------------------------------------------------
    class IntensityGaussianModel
    {
        public double IntensityMean;    ///< Mean of intensity component
        public double IntensityVar;     ///< Covariance of intensity component
        public double Weight;           ///< Weight of this GMM component in the mixture model

        public IntensityGaussianModel()
        {
            IntensityMean = 128.0;
            IntensityVar = 0.0;
            Weight = 0.0;
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for univariate
            @author Hyunna Lee
            @date 2013.11.19
            @param Intensity : Intensity component of the current instant 
            @return Probability of the current (intensity) instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(double Intensity)
        {
            double Difference = Intensity - IntensityMean;
            return Math.Exp(-(Difference * Difference) / (2.0 * IntensityVar)) / (Math.Sqrt(2.0 * Math.PI) * Math.Sqrt(IntensityVar));
        }
    }

    //---------------------------------------------------------------------------
    /** @class IntensityFrangiGaussianModel
        @author Hyunna Lee
        @date 2013.11.19
        @brief GMM Model parameters for intensity component
    */
    //-------------------------------------------------------------------------
    class IntensityVesselnessGaussianModel
    {
        public double IntensityMean;    ///< Mean of intensity component
        public double VesselnessMean;    ///< Mean of intensity component
        public double IntensityVar;     ///< Covariance of intensity component
        public double VesselnessVar;     ///< Covariance of intensity component
        public double Weight;           ///< Weight of this GMM component in the mixture model

        public IntensityVesselnessGaussianModel()
        {
            IntensityMean = 128.0;
            VesselnessMean = 0.5;
            IntensityVar = 0.0;
            VesselnessVar = 0.0;
            Weight = 0.0;
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for univariate
            @author Hyunna Lee
            @date 2013.11.19
            @param Intensity : intensity component of the current instant 
            @return Probability of the current (intensity) instant
        */
        //-------------------------------------------------------------------------
        public double GetIntensityGaussianProbability(double Intensity)
        {
            double Difference = Intensity - IntensityMean;
            return Math.Exp(-(Difference * Difference) / (2.0 * IntensityVar)) / (Math.Sqrt(2.0 * Math.PI) * Math.Sqrt(IntensityVar));
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for univariate
            @author Hyunna Lee
            @date 2013.11.19
            @param Vesselness : vesselness component of the current instant 
            @return Probability of the current (vesselness) instant
        */
        //-------------------------------------------------------------------------
        public double GetVesselnessGaussianProbability(double Vesselness)
        {
            double Difference = Vesselness - VesselnessMean;
            return Math.Exp(-(Difference * Difference) / (2.0 * VesselnessVar)) / (Math.Sqrt(2.0 * Math.PI) * Math.Sqrt(VesselnessVar));
        }
    }

    //---------------------------------------------------------------------------
    /** @class IVesselnessGaussianModel
        @author Hyunna Lee
        @date 2013.11.22
        @brief GMM Model parameters for intensity component
    */
    //-------------------------------------------------------------------------
    class IVesselnessGaussianModel
    {
        public Vector IVesselnessMean;      ///< Mean of intensity-vesselness component (I, V)
        public Matrix IVesselnessCoVar;     ///< Covariance matrix of intensity-vesselness component (II, IV; VI, VV)
        public double Weight;               ///< Weight of this GMM component in the mixture model

        public IVesselnessGaussianModel()
        {
            IVesselnessMean = new Vector(2);
            IVesselnessCoVar = new Matrix(2, 2);
            Weight = 0.0;
        }

        public IVesselnessGaussianModel(IVesselnessGaussianModel OldModel)
        {
            IVesselnessMean = new Vector(2);
            IVesselnessCoVar = new Matrix(2, 2);
            IVesselnessMean[0] = OldModel.IVesselnessMean[0];
            IVesselnessMean[1] = OldModel.IVesselnessMean[1];
            IVesselnessCoVar[0, 0] = OldModel.IVesselnessCoVar[0, 0];
            IVesselnessCoVar[0, 1] = OldModel.IVesselnessCoVar[0, 1];
            IVesselnessCoVar[1, 0] = OldModel.IVesselnessCoVar[1, 0];
            IVesselnessCoVar[1, 1] = OldModel.IVesselnessCoVar[1, 1];
            Weight = OldModel.Weight;
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for bivariate
            @author Hyunna Lee
            @date 2013.11.22
            @param Intensity : intensity component of the current instant 
            @param Vesselness : vesselness component of the current instant 
            @return Probability of the current instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(double Intensity, double Vesselness)
        {
            Vector DifferenceVector = new Vector(2);
            DifferenceVector[0] = Intensity - IVesselnessMean[0];
            DifferenceVector[1] = Vesselness - IVesselnessMean[1];
            Matrix InvCoVar = new Matrix(2, 2);
            InvCoVar = IVesselnessCoVar.Inverse();
            double det = IVesselnessCoVar.Determinant();
            double Difference = DifferenceVector[0] * (DifferenceVector[0] * InvCoVar[0, 0] + DifferenceVector[1] * InvCoVar[1, 0])
                              + DifferenceVector[1] * (DifferenceVector[0] * InvCoVar[0, 1] + DifferenceVector[1] * InvCoVar[1, 1]);
            return Math.Exp(-Difference / 2.0) / (2.0 * Math.PI * Math.Sqrt(det));
        }

        public void BlendingModel(IVesselnessGaussianModel BlendingModel, double BlendingWeight)
        {
            IVesselnessMean[0] = BlendingModel.IVesselnessMean[0] * BlendingWeight + IVesselnessMean[0] * (1.0 - BlendingWeight);
            IVesselnessMean[1] = BlendingModel.IVesselnessMean[1] * BlendingWeight + IVesselnessMean[1] * (1.0 - BlendingWeight);
            IVesselnessCoVar[0, 0] = BlendingModel.IVesselnessCoVar[0, 0] * BlendingWeight + IVesselnessCoVar[0, 0] * (1.0 - BlendingWeight);
            IVesselnessCoVar[0, 1] = BlendingModel.IVesselnessCoVar[0, 1] * BlendingWeight + IVesselnessCoVar[0, 1] * (1.0 - BlendingWeight);
            IVesselnessCoVar[1, 0] = BlendingModel.IVesselnessCoVar[1, 0] * BlendingWeight + IVesselnessCoVar[1, 0] * (1.0 - BlendingWeight);
            IVesselnessCoVar[1, 1] = BlendingModel.IVesselnessCoVar[1, 1] * BlendingWeight + IVesselnessCoVar[1, 1] * (1.0 - BlendingWeight);
            Weight = BlendingModel.Weight * BlendingWeight + Weight * (1.0 - BlendingWeight);
        }
    }

    //---------------------------------------------------------------------------
    /** @class IFKGaussianModel
        @author Hyunna Lee
        @date 2013.11.28
        @brief GMM Model parameters for intensity component
    */
    //-------------------------------------------------------------------------
    class IFKGaussianModel
    {
        public Vector IVesselnessMean;      ///< Mean of intensity-vesselness component (I, V)
        public Matrix IVesselnessCoVar;     ///< Covariance matrix of intensity-vesselness component (II, IV; VI, VV)
        public double Weight;               ///< Weight of this GMM component in the mixture model
        public bool IsBackComponent;        ///< 

        public IFKGaussianModel()
        {
            IVesselnessMean = new Vector(3);
            IVesselnessCoVar = new Matrix(3, 3);
            Weight = 0.0;
            IsBackComponent = true;
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for bivariate
            @author Hyunna Lee
            @date 2013.11.28
            @param Intensity : intensity component of the current instant 
            @param Frangi : Frangi's vesselness component of the current instant
            @param Krissian : Krissian's vesselness component of the current instant 
            @return Probability of the current instant
        */
        //-------------------------------------------------------------------------
        public double GetGaussianProbability(double Intensity, double Frangi, double Krissian)
        {
            /*
            if (!IsBackComponent)
            {
                Intensity = Math.Max(Intensity, IVesselnessMean[0]);
                Frangi = Math.Min(Frangi, IVesselnessMean[1]);
                Krissian = Math.Min(Krissian, IVesselnessMean[2]);
            }
             * */
            Vector DifferenceVector = new Vector(3);
            DifferenceVector[0] = Intensity - IVesselnessMean[0];
            DifferenceVector[1] = Frangi - IVesselnessMean[1];
            DifferenceVector[2] = Krissian - IVesselnessMean[2];
            Matrix InvCoVar = new Matrix(3, 3);
            InvCoVar = IVesselnessCoVar.Inverse();
            double det = IVesselnessCoVar.Determinant();
            double Difference = DifferenceVector[0] * (DifferenceVector[0] * InvCoVar[0, 0] + DifferenceVector[1] * InvCoVar[1, 0] + DifferenceVector[2] * InvCoVar[2, 0])
                              + DifferenceVector[1] * (DifferenceVector[0] * InvCoVar[0, 1] + DifferenceVector[1] * InvCoVar[1, 1] + DifferenceVector[2] * InvCoVar[2, 1])
                              + DifferenceVector[2] * (DifferenceVector[0] * InvCoVar[0, 2] + DifferenceVector[1] * InvCoVar[1, 2] + DifferenceVector[2] * InvCoVar[2, 2]);
            return Math.Exp(-Difference / 2.0) / (2.0 * Math.PI * Math.Sqrt(det));
        }
    }

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
            return Math.Exp(-Difference / 2.0) / (2.0 * Math.PI * Math.Sqrt(det));
        }

        //---------------------------------------------------------------------------
        /** @brief Gaussian probability density function for univariate
            @author Hyunna Lee
            @date 2013.11.07
            @param Intensity : Intensity component of the current instant 
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
