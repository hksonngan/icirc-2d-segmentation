using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace iCiRC
{
    class Filters
    {
        public double[] Filter;
        public Point3D FilterSize;

        public Filters()
        {
            Filter = null;
            FilterSize = null;
        }

        public void GenerateCentralDifferenceGradientFilter2D(int Dimension)
        {
            const int Size = 3;
            Filter = new double[Size * Size];
            Filter.Initialize();
            FilterSize = new Point3D(Size, Size, 1);

            if (Dimension == 0)         // Gradient_X
            {
                Filter[3] = -0.5;
                Filter[5] = 0.5;
            }
            else if (Dimension == 1)    // Gradient_Y
            {
                //
                // TO DO...
                //
            }
        }

        //
        // TO DO...
        //
        public void GenerateCentralDifferenceHessianFilter2D(int Dimension_1, int Dimension_2)
        {
            const int Size = 3;
            Filter = new double[Size * Size];
            Filter.Initialize();
            FilterSize = new Point3D(Size, Size, 1);

            if (Dimension_1 == 0 && Dimension_2 ==0)         // Hessian_XX
            {
            }
            else if ((Dimension_1 == 0 && Dimension_2 == 1) || (Dimension_1 == 1 && Dimension_2 == 0))    // Hessian_XY or Hessian_YX
            {
            }
            else if (Dimension_1 == 1 && Dimension_2 == 1)    // Hessian_YY
            {
            }
        }

        public void GenerateGaussianFilter2D(double Sigma, int Size)
        {
            Filter = new double[Size * Size];
            FilterSize = new Point3D(Size, Size, 1);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY;
            int CenterIndex = HalfSize * Size + HalfSize;

            for (int y = 0; y <= HalfSize; y++)
            {
                GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                for (int x = 0; x <= HalfSize; x++)
                {
                    GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                    Filter[CenterIndex + y * Size + x] =
                    Filter[CenterIndex + y * Size - x] =
                    Filter[CenterIndex - y * Size + x] =
                    Filter[CenterIndex - y * Size - x] = GaussianX * GaussianY;
                }
            }
        }

        public void GenerateGaussianGradientFilter2D(double Sigma, int Size, int Dimension)
        {
            Filter = new double[Size * Size];
            FilterSize = new Point3D(Size, Size, 1);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY;
            int CenterIndex = HalfSize * Size + HalfSize;

            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                if (Dimension == 1)
                    GaussianY *= -y / (Sigma * Sigma);

                for (int x = -HalfSize; x <= HalfSize; x++)
                {
                    GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                    if (Dimension == 0)
                        GaussianX *= -x / (Sigma * Sigma);

                    Filter[(y + HalfSize) * Size + (x + HalfSize)] = GaussianX * GaussianY;
                }
            }
        }

        public void GenerateGaussianHessianFilter2D(double Sigma, int Size, int Dimension_1, int Dimension_2)
        {
            Filter = new double[Size * Size];
            FilterSize = new Point3D(Size, Size, 1);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                if (Dimension_1 == 1 && Dimension_2 == 1)
                    GaussianY *= ((y - Sigma) * (y + Sigma)) / (Sigma * Sigma * Sigma * Sigma);
                else if (Dimension_1 == 1 || Dimension_2 == 1)
                    GaussianY *= -y / (Sigma * Sigma);

                for (int x = -HalfSize; x <= HalfSize; x++)
                {
                    GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                    if (Dimension_1 == 0 && Dimension_2 == 0)
                        GaussianX *= ((x - Sigma) * (x + Sigma)) / (Sigma * Sigma * Sigma * Sigma);
                    else if (Dimension_1 == 0 || Dimension_2 == 0)
                        GaussianX *= -x / (Sigma * Sigma);

                    Filter[(y + HalfSize) * Size + (x + HalfSize)] = GaussianX * GaussianY;
                }
            }
        }

        public double Run2D(int XNum, int YNum, byte[] ImageIntensity, int CurrentPixelIndex)
        {
            if (ImageIntensity == null || Filter == null)
                return 0.0;

            int CurrentPixelX = CurrentPixelIndex % XNum;
            int CurrentPixelY = CurrentPixelIndex / XNum;
            double NewVoxelDensity = 0.0;
            int FilterIndex, PixelIndex;

            for (int y = -FilterSize.y / 2; y <= FilterSize.y / 2; y++)
            {
                if (CurrentPixelY + y < 0 || CurrentPixelY + y > YNum - 1)
                    continue;
                for (int x = -FilterSize.x / 2; x <= FilterSize.x / 2; x++)
                {
                    if (CurrentPixelX + x < 0 || CurrentPixelX + x > XNum - 1)
                        continue;
                    FilterIndex = (y + FilterSize.y / 2) * FilterSize.x + (x + FilterSize.x / 2);
                    PixelIndex = (CurrentPixelY + y) * XNum + (CurrentPixelX + x);

                    NewVoxelDensity += Convert.ToDouble(ImageIntensity[PixelIndex]) * Filter[FilterIndex];
                }
            }

            return NewVoxelDensity;
        }

        public void GenerateGaussianFilter3D(double Sigma, int Size)
        {
            Filter = new double[Size * Size * Size];
            FilterSize = new Point3D(Size, Size, Size);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY, GaussianZ;
            int CenterIndex = HalfSize * Size * Size + HalfSize * Size + HalfSize;

            for (int z = 0; z <= HalfSize; z++)
            {
                GaussianZ = GaussianFunction(Convert.ToDouble(z), Sigma);
                for (int y = 0; y <= HalfSize; y++)
                {
                    GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                    for (int x = 0; x <= HalfSize; x++)
                    {
                        GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                        Filter[CenterIndex + z * Size * Size + y * Size + x] =
                        Filter[CenterIndex + z * Size * Size + y * Size - x] =
                        Filter[CenterIndex + z * Size * Size - y * Size + x] =
                        Filter[CenterIndex + z * Size * Size - y * Size - x] = 
                        Filter[CenterIndex - z * Size * Size + y * Size + x] =
                        Filter[CenterIndex - z * Size * Size + y * Size - x] =
                        Filter[CenterIndex - z * Size * Size - y * Size + x] =
                        Filter[CenterIndex - z * Size * Size - y * Size - x] = GaussianX * GaussianY * GaussianZ;
                    }
                }
            }
        }

        public void GenerateGaussianGradientFilter3D(double Sigma, int Size, int Dimension)
        {
            Filter = new double[Size * Size * Size];
            FilterSize = new Point3D(Size, Size, Size);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY, GaussianZ;
            int CenterIndex = HalfSize * Size * Size + HalfSize * Size + HalfSize;
            for (int z = -HalfSize; z <= HalfSize; z++)
            {
                GaussianZ = GaussianFunction(Convert.ToDouble(z), Sigma);
                if (Dimension == 2)
                    GaussianZ *= -z / (Sigma * Sigma);

                for (int y = -HalfSize; y <= HalfSize; y++)
                {
                    GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                    if (Dimension == 1)
                        GaussianY *= -y / (Sigma * Sigma);

                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                        if (Dimension == 0)
                            GaussianX *= -x / (Sigma * Sigma);

                        Filter[(z + HalfSize) * Size * Size + (y + HalfSize) * Size + (x + HalfSize)] = GaussianX * GaussianY * GaussianZ;
                    }
                }
            }
        }

        public void GenerateGaussianHessianFilter3D(double Sigma, int Size, int Dimension_1, int Dimension_2)
        {
            Filter = new double[Size * Size * Size];
            FilterSize = new Point3D(Size, Size, Size);

            int HalfSize = Size / 2;
            double GaussianX, GaussianY, GaussianZ;
            for (int z = -HalfSize; z <= HalfSize; z++)
            {
                GaussianZ = GaussianFunction(Convert.ToDouble(z), Sigma);
                if (Dimension_1 == 2 && Dimension_2 == 2)
                    GaussianZ *= ((z - Sigma) * (z + Sigma)) / (Sigma * Sigma * Sigma * Sigma);
                else if (Dimension_1 == 2 || Dimension_2 == 2)
                    GaussianZ *= -z / (Sigma * Sigma);

                for (int y = -HalfSize; y <= HalfSize; y++)
                {
                    GaussianY = GaussianFunction(Convert.ToDouble(y), Sigma);
                    if (Dimension_1 == 1 && Dimension_2 == 1)
                        GaussianY *= ((y - Sigma) * (y + Sigma)) / (Sigma * Sigma * Sigma * Sigma);
                    else if (Dimension_1 == 1 || Dimension_2 == 1)
                        GaussianY *= -y / (Sigma * Sigma);

                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        GaussianX = GaussianFunction(Convert.ToDouble(x), Sigma);
                        if (Dimension_1 == 0 && Dimension_2 == 0)
                            GaussianX *= ((x - Sigma) * (x + Sigma)) / (Sigma * Sigma * Sigma * Sigma);
                        else if (Dimension_1 == 0 || Dimension_2 == 0)
                            GaussianX *= -x / (Sigma * Sigma);

                        Filter[(z + HalfSize) * Size * Size + (y + HalfSize) * Size + (x + HalfSize)] = GaussianX * GaussianY * GaussianZ;
                    }
                }
            }
        }

        public double Run3D(int XNum, int YNum, int ZNum, ushort[] VolumeDensity, int CurrentVoxelIndex)
        {
            if (VolumeDensity == null || Filter == null)
                return 0.0;

            int CurrentVoxelX = CurrentVoxelIndex % XNum;
            int CurrentVoxelY = (CurrentVoxelIndex % (XNum * YNum)) / XNum;
            int CurrentVoxelZ = CurrentVoxelIndex / (XNum * YNum);
            double NewVoxelDensity = 0.0;
            int FilterIndex, VoxelIndex;

            for (int z = -FilterSize.z / 2; z <= FilterSize.z / 2; z++)
            {
                if (CurrentVoxelZ + z < 0 || CurrentVoxelZ + z > ZNum - 1)
                    continue;
                for (int y = -FilterSize.y / 2; y <= FilterSize.y / 2; y++)
                {
                    if (CurrentVoxelY + y < 0 || CurrentVoxelY + y > YNum - 1)
                        continue;
                    for (int x = -FilterSize.x / 2; x <= FilterSize.x / 2; x++)
                    {
                        if (CurrentVoxelX + x < 0 || CurrentVoxelX + x > XNum - 1)
                            continue;
                        FilterIndex = (z + FilterSize.z / 2) * FilterSize.x * FilterSize.y + (y + FilterSize.y / 2) * FilterSize.x + (x + FilterSize.x / 2);
                        VoxelIndex = (CurrentVoxelZ + z) * XNum * YNum + (CurrentVoxelY + y) * XNum + (CurrentVoxelX + x);

                        NewVoxelDensity += Convert.ToDouble(VolumeDensity[VoxelIndex]) * Filter[FilterIndex];
                    }
                }
            }

            return NewVoxelDensity;
        }

        private double GaussianFunction(double x, double sigma)
        {
            return Math.Exp(-(x * x) / (2.0 * sigma * sigma)) / (Math.Sqrt(2.0 * Math.PI) * sigma);
        }

        private void Normalize()
        {
            if (Filter == null)
                return;

            double Sum = 0.0;
            for (int i = 0; i < FilterSize.x * FilterSize.y * FilterSize.z; i++)
                Sum += Filter[i];
            for (int i = 0; i < FilterSize.x * FilterSize.y * FilterSize.z; i++)
                Filter[i] /= Sum;
        }
    }
}
