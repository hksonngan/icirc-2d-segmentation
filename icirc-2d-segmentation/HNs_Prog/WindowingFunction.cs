using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace HNs_Prog
{
    class WindowingFunction
    {
        public List<int> ControlPointDensity;
        public List<int> ControlPointIntensity;
        public int ThresholdMin, ThresholdMax;
        public bool ThresholdOn;

        public WindowingFunction(int MinDensity, int MaxDensity)
        {
            ControlPointDensity = new List<int>();
            ControlPointIntensity = new List<int>();
            int DensityStepSize = (MaxDensity - MinDensity) / 3;
            ControlPointDensity.Add(0);
            ControlPointIntensity.Add(0);
            ControlPointDensity.Add(MinDensity);
            ControlPointIntensity.Add(0);
            ControlPointDensity.Add(255);
            ControlPointIntensity.Add(255);
            ControlPointDensity.Add(MaxDensity);
            ControlPointIntensity.Add(255);

            ThresholdOn = false;
            ThresholdMin = MinDensity + 2 * DensityStepSize;
            ThresholdMax = MaxDensity - 2 * DensityStepSize;
        }

        public int ConvertDensityToIntensity(int Density)
        {
            int Intensity = 255;
            if (ThresholdOn && Density >= ThresholdMin && Density <= ThresholdMax)
                Intensity = 256;
            else
            {
                for (int i = 0; i < ControlPointDensity.Count - 1; i++)
                {
                    if (Density == ControlPointDensity[i])
                        Intensity = ControlPointIntensity[i];
                    else if (Density > ControlPointDensity[i] && Density < ControlPointDensity[i + 1])
                        Intensity = ControlPointIntensity[i] + (ControlPointIntensity[i + 1] - ControlPointIntensity[i]) * (Density - ControlPointDensity[i]) / (ControlPointDensity[i + 1] - ControlPointDensity[i]);
                }
            }
            return Intensity;
        }
        
        public void SetThresholingRange(int RangeMin, int RangeMax)
        {
            ThresholdMin = RangeMin;
            ThresholdMax = RangeMax;
        }
    }
}
