using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC.Tracking
{
    public class PerPixelIntensityGMMTracking : VesselTracking
    {
        int PerPixelModelNum;
        IntensityGaussianModel[,] GMMComponent;

        public PerPixelIntensityGMMTracking(int ModelNum)
        {
            PerPixelModelNum = ModelNum;
        }

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

            const int StartFrameIndex = 0;

            ProgressWindow winProgress = new ProgressWindow("Vessel tracking...", 0, FrameNum);
            winProgress.Show();

            InitializeGMMModelParameters(StartFrameIndex);

            // For each frame 
            for (int f = StartFrameIndex; f < FrameNum; f++)
            {
                UpdateGMMModelParameters(f);
                int[] FgModel = SortingGMMComponents();
                SegmentationUsingDataCost(f, FgModel);
                winProgress.Increment(1);
            }
            winProgress.Close();

            return FrameMask;
        }

        void InitializeGMMModelParameters(int CurrentFrameIndex)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            // Build the initial models            
            double ComponentWeight = 1.0 / Convert.ToDouble(PerPixelModelNum);
            GMMComponent = new IntensityGaussianModel[FramePixelNum, PerPixelModelNum];
            for (int i = 0; i < FramePixelNum; i++)
            {
                double Mean = 0.0;
                double Variance = 0.0;
                for (int f = 0; f < 5; f++)
                {
                    double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i + f * FramePixelNum]);
                    Mean += CurrentPixelIntensity;
                    Variance += CurrentPixelIntensity * CurrentPixelIntensity;
                }
                Mean /= 5.0;
                Variance = (Variance / 5.0) - (Mean * Mean);

                double ComponentOffset = Math.Sqrt(Variance) / 2.0;
                for (int c = 0; c < PerPixelModelNum; c++)
                {
                    GMMComponent[i, c] = new IntensityGaussianModel();
                    if (c == 0)
                        GMMComponent[i, c].IntensityMean = Mean;
                    else
                        GMMComponent[i, c].IntensityMean = Mean + Convert.ToDouble(c + 1) * Math.Pow(-1.0, Convert.ToDouble(c % 2)) * ComponentOffset;
                    GMMComponent[i, c].IntensityVar = Variance;
                    GMMComponent[i, c].Weight = ComponentWeight;
                }
            }

            /*
            // For the first frame, compute mean and variance of intensity
            double Mean = 0.0;
            double Variance = 0.0;
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                Mean += CurrentPixelIntensity;
                Variance += CurrentPixelIntensity * CurrentPixelIntensity;
            }
            Mean /= Convert.ToDouble(FramePixelNum);
            Variance = (Variance / Convert.ToDouble(FramePixelNum)) - (Mean * Mean);

            // Build the initial models
            GMMComponent = new IntensityGaussianModel[FramePixelNum, PerPixelModelNum];
            double ComponentWeight = 1.0 / Convert.ToDouble(PerPixelModelNum);
            double ComponentOffset = Math.Sqrt(Variance) / 2.0;
            for (int i = 0; i < FramePixelNum; i++)
            {
                for (int c = 0; c < PerPixelModelNum; c++)
                {
                    GMMComponent[i,c] = new IntensityGaussianModel();
                    if (c == 0)
                        GMMComponent[i,c].IntensityMean = Mean;
                    else
                        GMMComponent[i,c].IntensityMean = Mean + Convert.ToDouble(c + 1) * Math.Pow(-1.0, Convert.ToDouble(c % 2)) * ComponentOffset;
                    GMMComponent[i,c].IntensityVar = Variance;
                    GMMComponent[i,c].Weight = ComponentWeight;
                }
            }
             * */
        }

        void UpdateGMMModelParameters(int CurrentFrameIndex)
        {
            const double MatchingSDTHreshold = 2.5;
            const double LearningRate = 0.2;
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;

            // For each pixel
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                // Check the matching Gaussian distribution
                bool IsMatched = false;
                double WeightSum = 0.0;
                for (int c = 0; c < PerPixelModelNum; c++)
                {
                    double Difference = Math.Abs(CurrentPixelIntensity - GMMComponent[i, c].IntensityMean);
                    if (Difference < Math.Sqrt(GMMComponent[i, c].IntensityVar) * MatchingSDTHreshold)
                    {
                        // Update the matching Gaussian distribution
                        double SecondLearninRate = LearningRate * GMMComponent[i, c].GetGaussianProbability(CurrentPixelIntensity);
                        GMMComponent[i, c].IntensityMean = GMMComponent[i, c].IntensityMean * (1.0 - SecondLearninRate) + CurrentPixelIntensity * SecondLearninRate;
                        GMMComponent[i, c].IntensityVar = GMMComponent[i, c].IntensityVar * (1.0 - SecondLearninRate) + Difference * Difference * SecondLearninRate;
                        GMMComponent[i, c].Weight = GMMComponent[i, c].Weight * LearningRate + LearningRate;
                        IsMatched = true;
                    }
                    else
                        GMMComponent[i, c].Weight = GMMComponent[i, c].Weight * LearningRate;
                    WeightSum += GMMComponent[i, c].Weight;
                }
                // Weight normalization
                for (int c = 0; c < PerPixelModelNum; c++)
                    GMMComponent[i, c].Weight /= WeightSum;

                if (!IsMatched) // if  none of the K distributions match the current pixel
                {
                    int LeastProbableComponentIndex = 0;
                    double MinProbability = 1.0;
                    for (int c = 0; c < PerPixelModelNum; c++)
                    {
                        double CurrentComponentProbability = GMMComponent[i, c].Weight * GMMComponent[i, c].GetGaussianProbability(CurrentPixelIntensity);
                        if (CurrentComponentProbability < MinProbability)
                        {
                            MinProbability = CurrentComponentProbability;
                            LeastProbableComponentIndex = c;
                        }
                    }
                    GMMComponent[i, LeastProbableComponentIndex].IntensityMean = CurrentPixelIntensity;
                    //GMMComponent[i, LeastProbableComponentIndex].IntensityVar /= 2.0;
                }
            }
        }

        int[] SortingGMMComponents()
        {
            int FramePixelNum = XNum * YNum;
            const double MinimumDataPortion = 0.5;

            // Ordering the Gaussians by the value of (weight/sigma)
            for (int i = 0; i < FramePixelNum; i++) // For each pixel
            {
                for (int c = 1; c < PerPixelModelNum; c++)
                {
                    IntensityGaussianModel InsertComponent = new IntensityGaussianModel();
                    InsertComponent.IntensityMean = GMMComponent[i,c].IntensityMean;
                    InsertComponent.IntensityVar = GMMComponent[i,c].IntensityVar;
                    InsertComponent.Weight = GMMComponent[i,c].Weight;
                    double ValueToInsert = GMMComponent[i,c].Weight / Math.Sqrt(GMMComponent[i,c].IntensityVar);
                    int p = c;
                    while (p > 0 && ValueToInsert < (GMMComponent[i,p - 1].Weight / Math.Sqrt(GMMComponent[i,p - 1].IntensityVar)))
                    {
                        GMMComponent[i,p].IntensityMean = GMMComponent[i,p - 1].IntensityMean;
                        GMMComponent[i,p].IntensityVar = GMMComponent[i, p - 1].IntensityVar;
                        GMMComponent[i,p].Weight = GMMComponent[i,p - 1].Weight;
                        p--;
                    }
                    GMMComponent[i,p].IntensityMean = InsertComponent.IntensityMean;
                    GMMComponent[i,p].IntensityVar = InsertComponent.IntensityVar;
                    GMMComponent[i,p].Weight = InsertComponent.Weight;
                }
            }

            // Last B distribution are chosen as the foreground
            int[] ForeGMMComponentNum = new int[FramePixelNum];
            ForeGMMComponentNum.Initialize();
            for (int i = 0; i < FramePixelNum; i++) // For each pixel
            {
                double WeightSum = 0.0;
                for (int c = PerPixelModelNum - 1; c >= 0 && WeightSum < MinimumDataPortion; c--)
                {
                    WeightSum += GMMComponent[i,c].Weight;
                    ForeGMMComponentNum[i] = c;
                }
            }

            return ForeGMMComponentNum;
        }

        void SegmentationUsingDataCost(int CurrentFrameIndex, int[] ForeGMMComponentNum)
        {
            int FramePixelNum = XNum * YNum;
            int CurrentFrameOffset = CurrentFrameIndex * FramePixelNum;
            for (int i = 0; i < FramePixelNum; i++)
            {
                double CurrentPixelIntensity = Convert.ToDouble(FrameIntensity[CurrentFrameOffset + i]);
                // Likelihood
                double BackLikelihood = 0.0;
                double ForeLikelihood = 0.0;
                for (int c = 0; c < ForeGMMComponentNum[i]; c++)
                {
                    double GMMLikelihood = GMMComponent[i, c].Weight * GMMComponent[i, c].GetGaussianProbability(CurrentPixelIntensity);
                    ForeLikelihood += Math.Log10(GMMLikelihood);

                }
                for (int c = ForeGMMComponentNum[i]; c < PerPixelModelNum; c++)
                {
                    double GMMLikelihood = GMMComponent[i, c].Weight * GMMComponent[i, c].GetGaussianProbability(CurrentPixelIntensity);
                    BackLikelihood += Math.Log10(GMMLikelihood);
                }
                if (ForeLikelihood > BackLikelihood)
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_FOREGROUND;
                else
                    FrameMask[CurrentFrameOffset + i] = Constants.LABEL_BACKGROUND;
            }
        }
    }
}
