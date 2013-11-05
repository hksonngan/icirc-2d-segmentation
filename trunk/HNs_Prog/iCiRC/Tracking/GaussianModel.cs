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
            return 0.0;
        }

        public double GetGaussianProbability(double Intensity)
        {
            return 0.0;
        }
    }
}
