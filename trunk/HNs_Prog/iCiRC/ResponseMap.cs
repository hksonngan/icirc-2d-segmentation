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

            /*
            double MaxVesselness = 0.0;
            for (int i = 0; i < PixelNum; i++)
                MaxVesselness = Math.Max(MaxVesselness, Vesselness[i]);
            if (MaxVesselness == 0.0)
                return Vesselness;
            for (int i = 0; i < PixelNum; i++)
                Vesselness[i] /= MaxVesselness;
             * */

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
            const double gamma = 0.2, EdgeThreshold = 50.0;

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
                        double KrissianVesselness = Math.Min(Math.Abs(OrthogonalResponse1), Math.Abs(OrthogonalResponse2)) * Scale[s] / EdgeThreshold;
                        KrissianVesselness = 1.0 - Math.Exp(-(KrissianVesselness * KrissianVesselness) / (2.0 * gamma * gamma));
                        // Get the maximum filter response
                        Response[y * XNum + x] = Math.Max(Response[y * XNum + x], KrissianVesselness);
                    }
                }
            }

            // Response map normalization: [0, maximal] -> [0, 1]
            /*
            double MaxResponse = 0.0;
            for (int i = 0; i < XNum * YNum; i++)
                MaxResponse = Math.Max(MaxResponse, Response[i]);
            if (MaxResponse == 0.0)
                return Response;
            for (int i = 0; i < XNum * YNum; i++)
                Response[i] /= MaxResponse;
             * */

            return Response;
        }

        // Frangi et al.'s Hessian vesselness
        // Implemented by HNLee
        public double[] RunFrangiAndKrissianMethod2D(int XNum, int YNum, byte[] ImageIntensity, int SNum, double[] Scale)
        {
            if (ImageIntensity == null || XNum <= 0 || YNum <= 0 || Scale == null || SNum <= 0)
                return null;

            // Result buffer initialization
            int PixelNum = XNum * YNum;
            double[] Vesselness = new double[PixelNum];
            Vesselness.Initialize();
            const double alpha = 0.5, beta = 1.5, gamma = 0.2, EdgeThreshold = 50.0;
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
                        Vector OrthogonalVector = new Vector(2);
                        if (EigenDecom.RealEigenvalues[0] < Math.Abs(EigenDecom.RealEigenvalues[1]))
                        {
                            lambda1 = EigenDecom.RealEigenvalues[0];
                            lambda2 = EigenDecom.RealEigenvalues[1];
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 1];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 1];
                        }
                        else
                        {
                            lambda1 = EigenDecom.RealEigenvalues[1];
                            lambda2 = EigenDecom.RealEigenvalues[0];
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 0];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 0];
                        }
                        double RatioA = Math.Abs(lambda1) / Math.Abs(lambda2);
                        double Structureness = Math.Sqrt(lambda1 * lambda1 + lambda2 * lambda2);
                        OrthogonalVector = OrthogonalVector.Normalize();    // Get the unitary vector d, which is orthogonal to D

                        double FrangiVesselness = 0.0;
                        if (lambda2 > 0.0)
                            FrangiVesselness = Math.Exp(-(RatioA * RatioA) / (2 * alpha * alpha)) * (1 - Math.Exp(-(Structureness * Structureness) / (2 * beta * beta)));
                        Vesselness[y * XNum + x] = Math.Max(FrangiVesselness, Vesselness[y * XNum + x]);

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
                        double KrissianVesselness = Math.Min(Math.Abs(OrthogonalResponse1), Math.Abs(OrthogonalResponse2)) * Scale[s] / EdgeThreshold;
                        KrissianVesselness = 1.0 - Math.Exp(-(KrissianVesselness * KrissianVesselness) / (2.0 * gamma * gamma));
                        Vesselness[y * XNum + x] = Math.Max(KrissianVesselness, Vesselness[y * XNum + x]);
                    }
                }
            }

            /*
            double MaxVesselness = 0.0;
            for (int i = 0; i < PixelNum; i++)
                MaxVesselness = Math.Max(MaxVesselness, Vesselness[i]);
            if (MaxVesselness == 0.0)
                return Vesselness;
            for (int i = 0; i < PixelNum; i++)
                Vesselness[i] /= MaxVesselness;
             * */

            return Vesselness;
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
            byte[] TempImageIntensity = new byte[PixelNum];
            for (int i = 0; i < PixelNum; i++)
                SrcImage[i] = Convert.ToDouble(ImageIntensity[i]);
            DesImage = (double[])SrcImage.Clone();
            TempImageIntensity = (byte[])ImageIntensity.Clone();

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
                double FluxXMinus = 0.0;
                double[] FluxYMinus = new double[XNum];
                FluxYMinus.Initialize();
                for (int y = 2; y < YNum - 2; y++)
                {
                    for (int x = 2; x < XNum - 2; x++)
                    {
                        int CurrentPixelIndex = y * XNum + x;

                        // Get Hessian matrix
                        Matrix HessianMatrix = new Matrix(2, 2);
                        HessianMatrix[0, 0] = CentralDifferenceHessian[0].Run2D(XNum, YNum, TempImageIntensity, CurrentPixelIndex);
                        HessianMatrix[0, 1] =
                        HessianMatrix[1, 0] = CentralDifferenceHessian[1].Run2D(XNum, YNum, TempImageIntensity, CurrentPixelIndex);
                        HessianMatrix[1, 1] = CentralDifferenceHessian[2].Run2D(XNum, YNum, TempImageIntensity, CurrentPixelIndex);

                        // Compute the direction of maximal and minimal curvature
                        Vector OrthogonalVector = new Vector(2);
                        Vector EigenVector2 = new Vector(2);
                        EigenvalueDecomposition EigenDecom = new EigenvalueDecomposition(HessianMatrix);
                        if (EigenDecom.RealEigenvalues[0] < Math.Abs(EigenDecom.RealEigenvalues[1]))
                        {
                            EigenVector2[0] = EigenDecom.EigenVectors[0, 0];
                            EigenVector2[1] = EigenDecom.EigenVectors[1, 0];
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 1];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 1];
                        }
                        else
                        {
                            EigenVector2[0] = EigenDecom.EigenVectors[0, 1];
                            EigenVector2[1] = EigenDecom.EigenVectors[1, 1];
                            OrthogonalVector[0] = EigenDecom.EigenVectors[0, 0];
                            OrthogonalVector[1] = EigenDecom.EigenVectors[1, 0];
                        }

                        // Compute the F_x at the point (x + 1/2, y, z)
                        Vector GradientU = new Vector(2);
                        GradientU[0] = 0.5 * (SrcImage[CurrentPixelIndex + 1] - SrcImage[CurrentPixelIndex]); // u_x
                        GradientU[1] = 0.25 * (SrcImage[CurrentPixelIndex + XNum + 1] - SrcImage[CurrentPixelIndex - XNum + 1] // u_y
                                                + SrcImage[CurrentPixelIndex + XNum] - SrcImage[CurrentPixelIndex - XNum]);
                        double FluxXPlus = AnisotropicDF_PM(GradientU[0] * OrthogonalVector[0] + GradientU[1] * OrthogonalVector[1]) * OrthogonalVector[0]
                                        + (GradientU[0] * EigenVector2[0] + GradientU[1] * EigenVector2[1]) * 0.2 * EigenVector2[0];

                        // Compute the F_y at the point (x, y + 1/2, z)
                        GradientU[0] = 0.25 * (SrcImage[CurrentPixelIndex + XNum + 1] - SrcImage[CurrentPixelIndex + XNum - 1] // u_x
                                                + SrcImage[CurrentPixelIndex + 1] - SrcImage[CurrentPixelIndex - 1]);
                        GradientU[1] = 0.5 * (SrcImage[CurrentPixelIndex + XNum] - SrcImage[CurrentPixelIndex]); // u_y
                        double FluxYPlus = AnisotropicDF_PM(GradientU[0] * OrthogonalVector[0] + GradientU[1] * OrthogonalVector[1]) * OrthogonalVector[1]
                                        + (GradientU[0] * EigenVector2[0] + GradientU[1] * EigenVector2[1]) * 0.2 * EigenVector2[1];
                        /*
                        Vector gradient_u = new Vector(2);
                        gradient_u[0] = SrcImage[CurrentPixelIndex + 1] - SrcImage[CurrentPixelIndex]; // u_x
                        gradient_u[1] = 0.25 * (SrcImage[CurrentPixelIndex + XNum + 1] - SrcImage[CurrentPixelIndex - XNum + 1] // u_y
                                                + SrcImage[CurrentPixelIndex + XNum] - SrcImage[CurrentPixelIndex - XNum]);

                        Vector flux_plus = new Vector(2);
                        flux_plus[0] = AnisotropicDF_PM(gradient_u[0] * OrthogonalVector[0] + gradient_u[1] * OrthogonalVector[1]) * OrthogonalVector[0]
                                        + (gradient_u[0] * EigenVector2[0] + gradient_u[1] * EigenVector2[1]) * EigenVector2[0]; //flux_plus_x
                        flux_plus[1] = AnisotropicDF_PM(gradient_u[0] * OrthogonalVector[0] + gradient_u[1] * OrthogonalVector[1]) * OrthogonalVector[1]
                                        + (gradient_u[0] * EigenVector2[0] + gradient_u[1] * EigenVector2[1]) * EigenVector2[1]; //flux_plus_y;
                        */
                        if (x == 2)
                        {
                            GradientU[0] = 0.5 * (SrcImage[CurrentPixelIndex] - SrcImage[CurrentPixelIndex - 1]); // u_x
                            GradientU[1] = 0.25 * (SrcImage[CurrentPixelIndex + XNum] - SrcImage[CurrentPixelIndex - XNum] // u_y
                                                    + SrcImage[CurrentPixelIndex + XNum - 1] - SrcImage[CurrentPixelIndex - XNum - 1]);
                            FluxXMinus = AnisotropicDF_PM(GradientU[0] * OrthogonalVector[0] + GradientU[1] * OrthogonalVector[1]) * OrthogonalVector[0]
                                            + (GradientU[0] * EigenVector2[0] + GradientU[1] * EigenVector2[1]) * 0.2 * EigenVector2[0];
                        }
                        if (y == 2)
                        {
                            GradientU[0] = 0.25 * (SrcImage[CurrentPixelIndex + 1] - SrcImage[CurrentPixelIndex - 1] // u_x
                                                    + SrcImage[CurrentPixelIndex - XNum + 1] - SrcImage[CurrentPixelIndex - XNum - 1]);
                            GradientU[1] = 0.5 * (SrcImage[CurrentPixelIndex] - SrcImage[CurrentPixelIndex - XNum]); // u_y
                            FluxYMinus[x] = AnisotropicDF_PM(GradientU[0] * OrthogonalVector[0] + GradientU[1] * OrthogonalVector[1]) * OrthogonalVector[1]
                                            + (GradientU[0] * EigenVector2[0] + GradientU[1] * EigenVector2[1]) * 0.2 * EigenVector2[1];
                        }

                        double DeltaA = Convert.ToDouble(ImageIntensity[CurrentPixelIndex]) - SrcImage[CurrentPixelIndex];
                        double DeltaD = FluxXPlus - FluxXMinus + FluxYPlus - FluxYMinus[x];

                        DesImage[CurrentPixelIndex] = SrcImage[CurrentPixelIndex] + 0.1 * (DeltaD + 0.05 * DeltaA);

                        FluxXMinus = FluxXPlus;
                        FluxYMinus[x] = FluxYPlus;
                    }
                }
                for (int i = 0; i < PixelNum; i++)
                {
                    if (DesImage[i] < 0.0)
                        DesImage[i] = 0.0;
                    else if (DesImage[i] > 255.0)
                        DesImage[i] = 255.0;
                }
                SrcImage = (double[])DesImage.Clone();
                for (int i = 0; i < PixelNum; i++)
                    TempImageIntensity[i] = Convert.ToByte(DesImage[i]);
            }
            return DesImage;
        }

        private double AnisotropicDF_PM(double Gradient)
        {
            const double threshold = 10.0;
            double thresholdPower = threshold * threshold;
            double GradientPower = Gradient * Gradient;
            return Gradient * Math.Exp(-(GradientPower / thresholdPower));
        }

        /*
        private double EdgeStoppingFunction(double Gradient)
        {
            const double Sigma = 8.0;
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
        }*/

    }
}
