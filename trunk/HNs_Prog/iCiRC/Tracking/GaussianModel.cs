using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    class SpatialColorGaussianModel
    {
        public Vector SpatialMean;
        public Matrix SpatialCoVar;
        public double IntensityMean;
        public double IntensityVar;
        public double Weight;

        public SpatialColorGaussianModel()
        {
            SpatialMean = new Vector(2);
            SpatialCoVar = new Matrix(2, 2);
            IntensityMean = 128.0;
            IntensityVar = 0.0;
            Weight = 0.0;
        }

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

        public double GetGaussianProbability(double Intensity)
        {
            double Difference = Intensity - IntensityMean;
            return Math.Exp(-(Difference * Difference) / (2.0 * IntensityVar)) / (Math.Sqrt(2.0 * Math.PI) * Math.Sqrt(IntensityVar));
        }
    }
}
