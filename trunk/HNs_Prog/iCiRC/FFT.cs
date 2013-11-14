using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public struct COMPLEX
    {
        public double real, imag;
        public COMPLEX(double x, double y)
        {
            real = x;
            imag = y;
        }
        public float Magnitude()
        {
            return ((float)Math.Sqrt(real * real + imag * imag));
        }
        public float Phase()
        {
            return ((float)Math.Atan(imag / real));
        }
    }

    public class FFT
    {
        public int[,] GreyImage;         //GreyScale Image Array Generated from input Image
        private int Width, Height;
        private int nx, ny;              //Number of Points in Width & height
        private COMPLEX[,] Fourier;      //Fourier Magnitude  Array Used for Inverse FFT
        private COMPLEX[,] Output;        // FFT Normal
        public COMPLEX[,] FFTShifted;    // Shifted FFT 
        public COMPLEX[,] FFTNormal;     // FFT Shift Removed - required for Inverse FFT 

        public FFT(int[,] Input)
        {
            GreyImage = Input;
            Width = nx = Input.GetLength(0);
            Height = ny = Input.GetLength(1);
        }

        public FFT(double[,] Input)
        {
            Width = nx = Input.GetLength(0);
            Height = ny = Input.GetLength(1);
        }

        /// <summary>
        /// Constructor for Inverse FFT
        /// </summary>
        /// <param name="Input"></param>
        public FFT(COMPLEX[,] Input)
        {

            nx = Width = Input.GetLength(0);
            ny = Height = Input.GetLength(1);
            Fourier = Input;

        }

        // Calculate Fast Fourier Transform of Input Image
        public void ForwardFFT()
        {
            //Initializing Fourier Transform Array
            int i, j;
            Fourier = new COMPLEX[Width, Height];
            Output = new COMPLEX[Width, Height];
            //Copy Image Data to the Complex Array
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    Fourier[i, j].real = (double)GreyImage[i, j];
                    Fourier[i, j].imag = 0;
                }
            //Calling Forward Fourier Transform
            Output = FFT2D(Fourier, nx, ny, 1);
            return;
        }

        public void ForwardFFT(double[,] Data)
        {
            //Initializing Fourier Transform Array
            int i, j, W, H;
            W = Data.GetLength(0);
            H = Data.GetLength(1);

            Fourier = new COMPLEX[W, H];
            Output = new COMPLEX[W, H];
            //Copy Image Data to the Complex Array
            for (i = 0; i <= W - 1; i++)
                for (j = 0; j <= H - 1; j++)
                {
                    Fourier[i, j].real = Data[i, j];
                    Fourier[i, j].imag = 0;
                }
            //Calling Forward Fourier Transform
            Output = FFT2D(Fourier, W, H, 1);
            return;
        }

        // Shift The FFT of the Image
        public void FFTShift()
        {
            int i, j;
            FFTShifted = new COMPLEX[nx, ny];

            for (i = 0; i <= (nx / 2) - 1; i++)
                for (j = 0; j <= (ny / 2) - 1; j++)
                {
                    FFTShifted[i + (nx / 2), j + (ny / 2)] = Output[i, j];
                    FFTShifted[i, j] = Output[i + (nx / 2), j + (ny / 2)];
                    FFTShifted[i + (nx / 2), j] = Output[i, j + (ny / 2)];
                    FFTShifted[i, j + (nx / 2)] = Output[i + (nx / 2), j];
                }

            return;
        }

        /// <summary>
        /// Removes FFT Shift for FFTshift Array
        /// </summary>
        public void RemoveFFTShift()
        {
            int i, j;
            FFTNormal = new COMPLEX[nx, ny];

            for (i = 0; i <= (nx / 2) - 1; i++)
                for (j = 0; j <= (ny / 2) - 1; j++)
                {
                    FFTNormal[i + (nx / 2), j + (ny / 2)] = FFTShifted[i, j];
                    FFTNormal[i, j] = FFTShifted[i + (nx / 2), j + (ny / 2)];
                    FFTNormal[i + (nx / 2), j] = FFTShifted[i, j + (ny / 2)];
                    FFTNormal[i, j + (nx / 2)] = FFTShifted[i + (nx / 2), j];
                }
            return;
        }

        /// <summary>
        /// Generates Inverse FFT of Given Input Fourier
        /// </summary>
        /// <param name="Fourier"></param>
        public void InverseFFT(COMPLEX[,] Fourier)
        {
            //Initializing Fourier Transform Array
            int i, j;

            //Calling Forward Fourier Transform
            Output = new COMPLEX[nx, ny];
            Output = FFT2D(Fourier, nx, ny, -1);
            GreyImage = new int[nx, ny];

            //Copying Real Image Back to Greyscale
            //Copy Image Data to the Complex Array
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    GreyImage[i, j] = (int)Output[i, j].Magnitude();

                }
            return;

        }

        /*-------------------------------------------------------------------------
            Perform a 2D FFT inplace given a complex 2D array
            The direction dir, 1 for forward, -1 for reverse
            The size of the array (nx,ny)
            Return false if there are memory problems or
            the dimensions are not powers of 2
        */
        private COMPLEX[,] FFT2D(COMPLEX[,] c, int nx, int ny, int dir)
        {
            int i, j;
            int m;//Power of 2 for current number of points
            double[] real;
            double[] imag;
            COMPLEX[,] output;//=new COMPLEX [nx,ny];
            output = c; // Copying Array
            // Transform the Rows 
            real = new double[nx];
            imag = new double[nx];

            for (j = 0; j < ny; j++)
            {
                for (i = 0; i < nx; i++)
                {
                    real[i] = c[i, j].real;
                    imag[i] = c[i, j].imag;
                }
                // Calling 1D FFT Function for Rows
                m = (int)Math.Log((double)nx, 2);//Finding power of 2 for current number of points e.g. for nx=512 m=9
                FFT1D(dir, m, ref real, ref imag);

                for (i = 0; i < nx; i++)
                {
                    //  c[i,j].real = real[i];
                    //  c[i,j].imag = imag[i];
                    output[i, j].real = real[i];
                    output[i, j].imag = imag[i];
                }
            }
            // Transform the columns  
            real = new double[ny];
            imag = new double[ny];

            for (i = 0; i < nx; i++)
            {
                for (j = 0; j < ny; j++)
                {
                    //real[j] = c[i,j].real;
                    //imag[j] = c[i,j].imag;
                    real[j] = output[i, j].real;
                    imag[j] = output[i, j].imag;
                }
                // Calling 1D FFT Function for Columns
                m = (int)Math.Log((double)ny, 2);//Finding power of 2 for current number of points e.g. for nx=512 m=9
                FFT1D(dir, m, ref real, ref imag);
                for (j = 0; j < ny; j++)
                {
                    //c[i,j].real = real[j];
                    //c[i,j].imag = imag[j];
                    output[i, j].real = real[j];
                    output[i, j].imag = imag[j];
                }
            }

            // return(true);
            return (output);
        }

        /*-------------------------------------------------------------------------
            This computes an in-place complex-to-complex FFT
            x and y are the real and imaginary arrays of 2^m points.
            dir = 1 gives forward transform
            dir = -1 gives reverse transform
            Formula: forward
                     N-1
                      ---
                    1 \         - j k 2 pi n / N
            X(K) = --- > x(n) e                  = Forward transform
                    N /                            n=0..N-1
                      ---
                     n=0
            Formula: reverse
                     N-1
                     ---
                     \          j k 2 pi n / N
            X(n) =    > x(k) e                  = Inverse transform
                     /                             k=0..N-1
                     ---
                     k=0
            */
        private void FFT1D(int dir, int m, ref double[] x, ref double[] y)
        {
            long nn, i, i1, j, k, i2, l, l1, l2;
            double c1, c2, tx, ty, t1, t2, u1, u2, z;
            /* Calculate the number of points */
            nn = 1;
            for (i = 0; i < m; i++)
                nn *= 2;
            /* Do the bit reversal */
            i2 = nn >> 1;
            j = 0;
            for (i = 0; i < nn - 1; i++)
            {
                if (i < j)
                {
                    tx = x[i];
                    ty = y[i];
                    x[i] = x[j];
                    y[i] = y[j];
                    x[j] = tx;
                    y[j] = ty;
                }
                k = i2;
                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }
            /* Compute the FFT */
            c1 = -1.0;
            c2 = 0.0;
            l2 = 1;
            for (l = 0; l < m; l++)
            {
                l1 = l2;
                l2 <<= 1;
                u1 = 1.0;
                u2 = 0.0;
                for (j = 0; j < l1; j++)
                {
                    for (i = j; i < nn; i += l2)
                    {
                        i1 = i + l1;
                        t1 = u1 * x[i1] - u2 * y[i1];
                        t2 = u1 * y[i1] + u2 * x[i1];
                        x[i1] = x[i] - t1;
                        y[i1] = y[i] - t2;
                        x[i] += t1;
                        y[i] += t2;
                    }
                    z = u1 * c1 - u2 * c2;
                    u2 = u1 * c2 + u2 * c1;
                    u1 = z;
                }
                c2 = Math.Sqrt((1.0 - c1) / 2.0);
                if (dir == 1)
                    c2 = -c2;
                c1 = Math.Sqrt((1.0 + c1) / 2.0);
            }
            /* Scaling for forward transform */
            if (dir == 1)
            {
                for (i = 0; i < nn; i++)
                {
                    x[i] /= (double)nn;
                    y[i] /= (double)nn;

                }
            }
        }
    }
}
