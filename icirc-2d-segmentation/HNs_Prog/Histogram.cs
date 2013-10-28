using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HNs_Prog
{
    class Histogram
    {
        public int MinDensity, MaxDensity;
        public int BinNum, BinSize, MaxCountBinIndex;
        public int[] HistogramData;

        public Histogram(int SrcMinDensity, int SrcMaxDensity, int SrcBinNum)
        {
            MinDensity = SrcMinDensity;
            MaxDensity = SrcMaxDensity;
            BinNum = SrcBinNum;
            BinSize = (MaxDensity - MinDensity) / BinNum + 1;
            HistogramData = new int[SrcBinNum];
            HistogramData.Initialize();
        }

        public void SetVolume(Volume SrcVolume, HistogramMode SrcMode, int SrcCurrentIndex)
        {
            HistogramData.Initialize();

            switch (SrcMode)
            {
                case HistogramMode.Volume:
                    int TotalCount = SrcVolume.XNum * SrcVolume.YNum * SrcVolume.ZNum;

                    int CurrentBinIndex;
                    for (int i = 0; i < TotalCount; i++)
                    {
                        CurrentBinIndex = (SrcVolume.VolumeDensity[i] - MinDensity) / BinSize;
                        HistogramData[CurrentBinIndex]++;
                    }
            	    break;
                case HistogramMode.Axial:
                    TotalCount = SrcVolume.XNum * SrcVolume.YNum;

                    int idxTempY, idxTempZ;
                    idxTempZ = SrcCurrentIndex * SrcVolume.XNum * SrcVolume.YNum;
                    for (int y = 0; y < SrcVolume.YNum; y++)
                    {
                        idxTempY = y * SrcVolume.XNum;
                        for (int x = 0; x < SrcVolume.XNum; x++)
                        {
                            CurrentBinIndex = (SrcVolume.VolumeDensity[idxTempZ + idxTempY + x] - MinDensity) / BinSize;
                            HistogramData[CurrentBinIndex]++;
                        }
                    }
                    break;
                case HistogramMode.Coronal:
                    TotalCount = SrcVolume.XNum * SrcVolume.ZNum;

                    idxTempY = SrcCurrentIndex * SrcVolume.XNum;
                    for (int z = 0; z < SrcVolume.ZNum; z++)
                    {
                        idxTempZ = z * SrcVolume.XNum * SrcVolume.YNum;
                        for (int x = 0; x < SrcVolume.XNum; x++)
                        {
                            CurrentBinIndex = (SrcVolume.VolumeDensity[idxTempZ + idxTempY + x] - MinDensity) / BinSize;
                            HistogramData[CurrentBinIndex]++;
                        }
                    }
                    break;
                case HistogramMode.Sagittal:
                    TotalCount = SrcVolume.YNum * SrcVolume.ZNum;

                    for (int z = 0; z < SrcVolume.ZNum; z++)
                    {
                        idxTempZ = z * SrcVolume.XNum * SrcVolume.YNum;
                        for (int y = 0; y < SrcVolume.YNum; y++)
                        {
                            idxTempY = y * SrcVolume.XNum;
                            CurrentBinIndex = (SrcVolume.VolumeDensity[idxTempZ + idxTempY + SrcCurrentIndex] - MinDensity) / BinSize;
                            HistogramData[CurrentBinIndex]++;
                        }
                    }
                    break;
            }

            int MaxCount = int.MinValue;
            for (int i = 0; i < BinNum; i++)
            {
                if (HistogramData[i] > MaxCount)
                {
                    MaxCount = HistogramData[i];
                    MaxCountBinIndex = i;
                }
            }
        }
    }
}
