using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    public class ResponseMap
    {
        public ResponseMap()
        {
        }

        // Frangi et al.'s Hessian vesselness
        // Implemented by SHJung
        public double[] RunFrangiMethod2D(int XNum, int YNum, byte[] ImageIntensity, int SNum, double[] Scale)
        {
            if (ImageIntensity == null || XNum <= 0 || YNum <= 0 || Scale == null || SNum <= 0)
                return null;

            // Result buffer initialization
            int PixelNum = XNum * YNum;
            double[] Vesselness = new double[PixelNum];
            Vesselness.Initialize();
            const double alpha = 0.5, beta = 1.0;

            // Multi-scale
            for (int s = 0; s < SNum; s++)      // For each scale
            {
                // Filter generation for Gradient and Hessian with sigma
                Filters[] GaussianGradientFilters = new Filters[2];
                for (int i = 0; i < 2; i++)
                    GaussianGradientFilters[i] = new Filters();
                GaussianGradientFilters[0].GenerateGaussianGradientFilter2D(Scale[s], 15, 0);
                GaussianGradientFilters[1].GenerateGaussianGradientFilter2D(Scale[s], 15, 1);
                Filters[] GaussianHessianFilters = new Filters[3];
                for (int i = 0; i < 3; i++)
                    GaussianHessianFilters[i] = new Filters();
                GaussianHessianFilters[0].GenerateGaussianHessianFilter2D(Scale[s], 15, 0, 0);
                GaussianHessianFilters[1].GenerateGaussianHessianFilter2D(Scale[s], 15, 0, 1);
                GaussianHessianFilters[2].GenerateGaussianHessianFilter2D(Scale[s], 15, 1, 1);

                // For each pixel
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        // Get Hessian matrix
                        Matrix HessianMatrix = new Matrix(2, 2);
                        HessianMatrix[0, 0] = GaussianHessianFilters[0].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);
                        HessianMatrix[0, 1] =
                        HessianMatrix[1, 0] = GaussianHessianFilters[1].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);
                        HessianMatrix[1, 1] = GaussianHessianFilters[2].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);

                        EigenvalueDecomposition EigenDecom = new EigenvalueDecomposition(HessianMatrix);
                        double lambda1, lambda2;
                        if (EigenDecom.RealEigenvalues[0] < Math.Abs(EigenDecom.RealEigenvalues[1]))
                        {
                            lambda1 = EigenDecom.RealEigenvalues[0];
                            lambda2 = EigenDecom.RealEigenvalues[1];
                        }
                        else
                        {
                            lambda1 = EigenDecom.RealEigenvalues[1];
                            lambda2 = EigenDecom.RealEigenvalues[0];
                        }
                        double RatioA = Math.Abs(lambda1) / Math.Abs(lambda2);
                        double Structureness = Math.Sqrt(lambda1 * lambda1 + lambda2 * lambda2);

                        if (lambda2 > 0.0)
                        {
                            double TempVesselness = Math.Exp(-(RatioA * RatioA) / (2 * alpha * alpha)) *(1 - Math.Exp(-(Structureness * Structureness) / (2 * beta * beta)));
                            Vesselness[y * XNum + x] = Math.Max(TempVesselness, Vesselness[y * XNum + x]);
                        }

                    }
                }
            }
            double MaxVesselness = 0.0;
            for (int i = 0; i < PixelNum; i++)
                MaxVesselness = Math.Max(MaxVesselness, Vesselness[i]);
            if (MaxVesselness == 0.0)
                return Vesselness;
            for (int i = 0; i < PixelNum; i++)
                Vesselness[i] /= MaxVesselness;

            return Vesselness;
        }

        // Krissian et al.'s tubular structure detection
        // Implemented by HNLee
        public double[] RunKrissianModelMethod2D(int XNum, int YNum, byte[] ImageIntensity, int SNum, double[] Scale)
        {
            if (ImageIntensity == null || XNum <= 0 || YNum <= 0 || Scale == null || SNum <= 0)
                return null;

            // Result buffer initialization
            int PixelNum = XNum * YNum;
            double[] Response = new double[PixelNum];
            Response.Initialize();
            double Tau = Math.Sqrt(3.0);

            // Multi-scale
            for (int s = 0; s < SNum; s++)      // For each scale
            {
                // Filter generation for Gradient and Hessian with sigma
                Filters[] GaussianGradientFilters = new Filters[2];
                for (int i = 0; i < 2; i++)
                    GaussianGradientFilters[i] = new Filters();
                GaussianGradientFilters[0].GenerateGaussianGradientFilter2D(Scale[s], 15, 0);
                GaussianGradientFilters[1].GenerateGaussianGradientFilter2D(Scale[s], 15, 1);
                Filters[] GaussianHessianFilters = new Filters[3];
                for (int i = 0; i < 3; i++)
                    GaussianHessianFilters[i] = new Filters();
                GaussianHessianFilters[0].GenerateGaussianHessianFilter2D(Scale[s], 15, 0, 0);
                GaussianHessianFilters[1].GenerateGaussianHessianFilter2D(Scale[s], 15, 0, 1);
                GaussianHessianFilters[2].GenerateGaussianHessianFilter2D(Scale[s], 15, 1, 1);

                // For each voxel
                for (int y = 0; y < YNum; y++)
                {
                    for (int x = 0; x < XNum; x++)
                    {
                        // Get Hessian matrix
                        Matrix HessianMatrix = new Matrix(2, 2);
                        HessianMatrix[0, 0] = GaussianHessianFilters[0].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);
                        HessianMatrix[0, 1] =
                        HessianMatrix[1, 0] = GaussianHessianFilters[1].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);
                        HessianMatrix[1, 1] = GaussianHessianFilters[2].Run2D(XNum, YNum, ImageIntensity, y * XNum + x);

                        // Get the unitary vector d, which is orthogonal to D
                        Vector OrthogonalVector = new Vector(2);
                        EigenvalueDecomposition EigenDecom = new EigenvalueDecomposition(HessianMatrix);
                        if (EigenDecom.RealEigenvalues[0] < Math.Abs(EigenDecom.RealEigenvalues[1]))
                        {
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 1];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 1];
                        }
                        else
                        {
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 0];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 0];
                        }
                        OrthogonalVector = OrthogonalVector.Normalize();

                        // Get the filter response R(sigma, voxel)
                        Vector GradientVector = new Vector(2);
                        int OrthogonalPixelX = Convert.ToInt32(Convert.ToDouble(x) + Tau * Scale[s] * OrthogonalVector[0] + 0.5);
                        int OrthogonalPixelY = Convert.ToInt32(Convert.ToDouble(y) + Tau * Scale[s] * OrthogonalVector[1] + 0.5);
                        GradientVector[0] = GaussianGradientFilters[0].Run2D(XNum, YNum, ImageIntensity, OrthogonalPixelY * XNum + OrthogonalPixelX);
                        GradientVector[1] = GaussianGradientFilters[1].Run2D(XNum, YNum, ImageIntensity, OrthogonalPixelY * XNum + OrthogonalPixelX);
                        double OrthogonalResponse1 = GradientVector[0] * OrthogonalVector[0] + GradientVector[1] * OrthogonalVector[1];
                        OrthogonalPixelX = Convert.ToInt32(Convert.ToDouble(x) - Tau * Scale[s] * OrthogonalVector[0] - 0.5);
                        OrthogonalPixelY = Convert.ToInt32(Convert.ToDouble(y) - Tau * Scale[s] * OrthogonalVector[1] - 0.5);
                        GradientVector[0] = GaussianGradientFilters[0].Run2D(XNum, YNum, ImageIntensity, OrthogonalPixelY * XNum + OrthogonalPixelX);
                        GradientVector[1] = GaussianGradientFilters[1].Run2D(XNum, YNum, ImageIntensity, OrthogonalPixelY * XNum + OrthogonalPixelX);
                        double OrthogonalResponse2 = GradientVector[0] * OrthogonalVector[0] + GradientVector[1] * OrthogonalVector[1];
                        double FilterResponse = Math.Min(Math.Abs(OrthogonalResponse1), Math.Abs(OrthogonalResponse2));

                        // Get the maximum filter response
                        Response[y * XNum + x] = Math.Max(Response[y * XNum + x], FilterResponse * Scale[s]);
                    }
                }
            }

            // Response map normalization: [0, maximal] -> [0, 1]
            double MaxResponse = 0.0;
            for (int i = 0; i < XNum * YNum; i++)
                MaxResponse = Math.Max(MaxResponse, Response[i]);
            if (MaxResponse == 0.0)
                return Response;
            for (int i = 0; i < XNum * YNum; i++)
                Response[i] /= MaxResponse;

            return Response;
        }

        // Krissian et al.'s flux-based anisotropic diffusion
        // Implemented by SHJung 
        public double[] RunKrissianFluxMethod2D(int XNum, int YNum, byte[] ImageIntensity, int IterNum)
        {
            if (ImageIntensity == null || XNum <= 0 || YNum <= 0)
                return null;

            // Src & Des buffer initialization
            int PixelNum = XNum * YNum;
            double[] SrcImage = new double[PixelNum];
            double[] DesImage = new double[PixelNum];
            for (int i = 0; i < PixelNum; i++)
                SrcImage[i] = Convert.ToDouble(ImageIntensity[i]);
            DesImage = (double[])SrcImage.Clone();

            // 
            const double Lambda = 0.25;
            for (int iter = 0; iter < IterNum; iter++)  // Time step
            {
                // Filter generation for Central Difference Gradient and Central Difference Hessian
                Filters[] CentralDifferenceGradient = new Filters[2];
                for (int i = 0; i < 2; i++)
                    CentralDifferenceGradient[i] = new Filters();
                CentralDifferenceGradient[0].GenerateCentralDifferenceGradientFilter2D(0);
                CentralDifferenceGradient[1].GenerateCentralDifferenceGradientFilter2D(1);

                Filters[] CentralDifferenceHessian = new Filters[3];
                for (int i = 0; i < 3; i++)
                    CentralDifferenceHessian[i] = new Filters();
                CentralDifferenceHessian[0].GenerateCentralDifferenceHessianFilter2D(0,0);
                CentralDifferenceHessian[1].GenerateCentralDifferenceHessianFilter2D(0,1);
                CentralDifferenceHessian[2].GenerateCentralDifferenceHessianFilter2D(1,1);
                
                // For each pixel
                for (int y = 1; y < YNum - 1; y++)
                {
                    for (int x = 1; x < XNum - 1; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;
                        Vector GradientVector = new Vector(2);
                        GradientVector[0] = CentralDifferenceGradient[0].Run2D(XNum, YNum, ImageIntensity, CurrentPixelIndex);
                        GradientVector[1] = CentralDifferenceGradient[1].Run2D(XNum, YNum, ImageIntensity, CurrentPixelIndex);

                        // Get Hessian matrix
                        Matrix HessianMatrix = new Matrix(2, 2);
                        HessianMatrix[0, 0] = CentralDifferenceHessian[0].Run2D(XNum, YNum, ImageIntensity, CurrentPixelIndex);
                        HessianMatrix[0, 1] =
                        HessianMatrix[1, 0] = CentralDifferenceHessian[1].Run2D(XNum, YNum, ImageIntensity, CurrentPixelIndex);
                        HessianMatrix[1, 1] = CentralDifferenceHessian[2].Run2D(XNum, YNum, ImageIntensity, CurrentPixelIndex);

                        // Compute the direction of maximal and minimal curvature
                        Vector OrthogonalVector = new Vector(2);
                        EigenvalueDecomposition EigenDecom = new EigenvalueDecomposition(HessianMatrix);
                        if (EigenDecom.RealEigenvalues[0] < Math.Abs(EigenDecom.RealEigenvalues[1]))
                        {
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 1]; //minimal curvature
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 1]; //maximal curvature
                        }
                        else
                        {
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 0];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 0];
                        }

                        Vector gradient_u = new Vector(2);
                        gradient_u[0] = SrcImage[y * XNum + x + 1] - SrcImage[CurrentPixelIndex]; // u_x
                        gradient_u[1] = 0.25 * (SrcImage[(y + 1) * XNum + x + 1] - SrcImage[(y - 1) * XNum + x + 1] // u_y
                                                + SrcImage[(y + 1) * XNum + x] - SrcImage[(y - 1) * XNum + x]);

                        Vector gradient_u_minus = new Vector(2);
                        gradient_u_minus[0] = SrcImage[CurrentPixelIndex] - SrcImage[y * XNum + x-1]; // u_x
                        gradient_u_minus[1] = 0.25 * (SrcImage[(y + 1) * XNum + x] - SrcImage[(y - 1) * XNum + x] // u_y
                                                + SrcImage[(y + 1) * XNum + x-1] - SrcImage[(y - 1) * XNum + x-1]);

                        Vector flux_plus = new Vector(2);
                        flux_plus[0] = AnisotropicDF_PM(Vector.ScalarProduct(gradient_u, OrthogonalVector) * OrthogonalVector[0]) + (Vector.ScalarProduct(gradient_u, OrthogonalVector) * OrthogonalVector[0]); //flux_plus_x
                        flux_plus[1] = AnisotropicDF_PM(Vector.ScalarProduct(gradient_u, OrthogonalVector) * OrthogonalVector[1]) + (Vector.ScalarProduct(gradient_u, OrthogonalVector) * OrthogonalVector[1]); //flux_plus_y;

                        Vector flux_minus = new Vector(2);
                        flux_minus[0] = AnisotropicDF_PM(Vector.ScalarProduct(gradient_u_minus, OrthogonalVector) * OrthogonalVector[0]) + (Vector.ScalarProduct(gradient_u_minus, OrthogonalVector) * OrthogonalVector[0]); //flux_minus_x
                        flux_minus[1] = AnisotropicDF_PM(Vector.ScalarProduct(gradient_u_minus, OrthogonalVector) * OrthogonalVector[1]) + (Vector.ScalarProduct(gradient_u_minus, OrthogonalVector) * OrthogonalVector[1]); //flux_minus_y;

                        double delta_a = Convert.ToDouble(ImageIntensity) - SrcImage[CurrentPixelIndex];

                        Vector delta_d = new Vector(1);
                        delta_d[0] = flux_plus[0] - flux_minus[0] + flux_plus[1] - flux_minus[1];

                        DesImage[CurrentPixelIndex] = SrcImage[CurrentPixelIndex] + delta_a + Convert.ToDouble(delta_d);

                        flux_minus[0] = flux_plus[0];
                        flux_minus[1] = flux_plus[1];

                    }
                }
                SrcImage = (double[])DesImage.Clone();
            }
            return DesImage;
        }

        private double EdgeStoppingFunction(double Gradient)
        {
            const double Sigma = 9.0;
            double SigmaPower = Sigma * Sigma;
            double GradientPower = Gradient * Gradient;
            //if (GFunctionIndex == 1)
            return Gradient * Math.Exp(-(GradientPower / SigmaPower));
            /*
            else if (GFunctionIndex == 2)
                return Gradient / (1.0 + GradientPower / SigmaPower);
            else if (GFunctionIndex == 3 && Math.Abs(Gradient) <= Sigma)
                return (1.0 - GradientPower / SigmaPower) * (1.0 - GradientPower / SigmaPower) / 2.0;
            else if (GFunctionIndex == 4 && Math.Abs(Gradient) <= Sigma)
                return 1.0 / Sigma;
            else if (GFunctionIndex == 4)
                return Math.Sign(Gradient) / Gradient;
            else
                return 0.0;
             * */
        }

        private double AnisotropicDF_PM(double Gradient)
        {
            const double threshold = 8.0;
            double thresholdPower = threshold * threshold;
            double GradientPower = Gradient * Gradient;
            return Gradient * Math.Exp(-(GradientPower / thresholdPower));
        }
    }
}
