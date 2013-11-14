using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class HomomorphicFilter : FrameProcessing
    {
        public HomomorphicFilter()
        {
        }

        public HomomorphicFilter(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
        }

        public byte[] RunFiltering(byte[] Intensity)
        {
            InputFrameIntensity = Intensity;
            int FramePixelNum = XNum * YNum;
            OutputFrameIntensity = new byte[FramePixelNum];
            OutputFrameIntensity.Initialize();

            const float rL = 0.6f;
            const float rH = 1.2f;
            const float Sigma = 64.0f;
            const float Slope = 0.5f;

            // Take log image
            int[,] ImageData = new int[XNum, YNum];
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                    ImageData[x, y] = Convert.ToInt32(InputFrameIntensity[y * XNum + x]);
            }

            // High-pass filtering
            FFT FFTObject = new FFT(ImageData);
            FFTObject.ForwardFFT();                             //FFT of the LOG Image
            FFTObject.FFTShift();                               //Shifting FFT for Filtering
            COMPLEX[,] FFTData = ApplyFilterHMMFreqDomain(FFTObject.FFTShifted, rH, rL, Sigma, Slope);  // Applying Filter on the FFT of the Log Image    

            FFT FFTInvObject = new FFT(FFTData);                //Inverse FFT of the COMPLEX Data
            FFTInvObject.FFTShifted = FFTData;
            FFTInvObject.RemoveFFTShift();                      //Removing FFT SHIFT
            FFTInvObject.InverseFFT(FFTInvObject.FFTNormal);    //Inverse FFT

            // Take exp image
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                    OutputFrameIntensity[y * XNum + x] = Convert.ToByte(FFTInvObject.GreyImage[x, y]);
            }

            return OutputFrameIntensity;
        }

        /// <summary>
        /// Applies Gaussian Filter on the Image Data
        /// </summary>
        /// <param name="FFTData">FFT of the Image</param>
        /// <param name="rL">Lower Homomrphic Threshold</param>
        /// <param name="rH">Upper Homomrphic Threshold</param>
        /// <param name="Sigma"> Spread of the Gaussian</param>
        /// <param name="Slope">Slope of the Sharpness of the Gaussian Filter</param>
        /// <returns></returns>

        private COMPLEX[,] ApplyFilterHMMFreqDomain(COMPLEX[,] FFTData, float rH, float rL, float Sigma, float Slope)
        {
            COMPLEX[,] Output = new COMPLEX[FFTData.GetLength(0), FFTData.GetLength(1)];
            int i, j, W, H;

            W = FFTData.GetLength(0);
            H = FFTData.GetLength(1);

            double Weight;
            //Taking FFT of Gaussian HPF
            double[,] GaussianHPF = GenerateGaussianKernelHPF(FFTData.GetLength(0), Sigma, Slope, out Weight);

            //Variables for FFT of Gaussian Filter
            COMPLEX[,] GaussianHPFFFT;
            // FFT GaussianFFTObject;

            for (i = 0; i <= GaussianHPF.GetLength(0) - 1; i++)
                for (j = 0; j <= GaussianHPF.GetLength(1) - 1; j++)
                {
                    GaussianHPF[i, j] = GaussianHPF[i, j];// / Weight;
                }

            FFT GaussianFFTObject = new FFT(GaussianHPF);
            GaussianFFTObject.ForwardFFT(GaussianHPF);
            //Shifting FFT for Filtering
            GaussianFFTObject.FFTShift();


            GaussianHPFFFT = GaussianFFTObject.FFTShifted;
            for (i = 0; i <= GaussianHPF.GetLength(0) - 1; i++)
                for (j = 0; j <= GaussianHPF.GetLength(1) - 1; j++)
                {
                    GaussianHPFFFT[i, j].real = (rH - rL) * GaussianHPFFFT[i, j].real + rL;
                    GaussianHPFFFT[i, j].imag = (rH - rL) * GaussianHPFFFT[i, j].imag + rL;


                }

            // Applying Filter on the FFT of the Log Image by Multiplying in Frequency Domain
            Output = MultiplyFFTMatrices(GaussianHPFFFT, FFTData);


            return Output;
        }

        /// <summary>
        /// Generates Gaussian Filter Kernel
        /// </summary>
        /// <param name="N">Size of the Filter</param>
        /// <param name="Sigma">Spread of the Gaussian</param>
        /// <param name="Slope">Harpness of the Slope of the Gaussian</param>
        /// <param name="Weight">Weight of the Filter Kernel (Out Variable)</param>
        /// <returns>GAussian Kernel</returns>
        private double[,] GenerateGaussianKernelHPF(int N, float Sigma, float Slope, out double Weight)
        {
            float pi;
            pi = (float)Math.PI;
            int i, j;
            int SizeofKernel = N;
            double[,] GaussianKernel = new double[N, N]; ;
            float[,] Kernel = new float[N, N];

            float[,] OP = new float[N, N];
            float D1, D2;


            D1 = 1 / (2 * pi * Sigma * Sigma);
            D2 = 2 * Sigma * Sigma;

            float min = 1000, max = 0;

            for (i = -SizeofKernel / 2; i <= SizeofKernel / 2 - 1; i++)
            {
                for (j = -SizeofKernel / 2; j <= SizeofKernel / 2 - 1; j++)
                {
                    Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = ((1 / D1) * (float)Math.Exp(-Slope * (i * i + j * j) / D2));

                    if (Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] < min)
                        min = Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                    if (Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] > max)
                        max = Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];

                }
            }
            //Converting to the scale of 0-1
            double sum = 0;
            for (i = -SizeofKernel / 2; i <= SizeofKernel / 2 - 1; i++)
            {
                for (j = -SizeofKernel / 2; j <= SizeofKernel / 2 - 1; j++)
                {
                    GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = (Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] - min) / (max - min);

                    GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = 1 - GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];

                    sum = sum + GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                }

            }
            //Normalizing kernel Weight
            Weight = sum;

            return GaussianKernel;
        }

        public COMPLEX[,] MultiplyFFTMatrices(COMPLEX[,] A, COMPLEX[,] B)
        {
            int Width = A.GetLength(0);
            int Height = A.GetLength(1);
            COMPLEX[,] Output = new COMPLEX[Width, Height];
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    double a = A[i, j].real;
                    double b = A[i, j].imag;
                    double c = B[i, j].real;
                    double d = B[i, j].imag;

                    Output[i, j].real = (a * c - b * d);
                    Output[i, j].imag = (a * d + b * c);
                }
            }
            return Output;
        }     
    }
}
