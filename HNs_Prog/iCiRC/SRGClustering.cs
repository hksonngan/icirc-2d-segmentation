using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class SRGClustering : Clustering
    {
        int ClusterMinSizeThreshold;
        int ClusterMaxSizeThreshold;

        public SRGClustering(int MinSize, int MaxSize)
        {
            ClusterMinSizeThreshold = MinSize;
            ClusterMaxSizeThreshold = MaxSize;
        }

        public int RunClustering(int paraXNum, int paraYNum, byte[] paraImageMask, byte paraObjectLabel)
        {
            if (paraXNum <= 0 || paraYNum <= 0)
                return -1;

            XNum = paraXNum; YNum = paraYNum;
            ImageMask = paraImageMask;
            ObjectLabel = paraObjectLabel;

            LabelNum = 0;
            ClusterLabel = new int[XNum * YNum];
            ClusterLabel.Initialize();

            bool[] NeighborCheck = new bool[4];
            int[] NeighborOffset = new int[4];
            NeighborOffset[0] = -1;
            NeighborOffset[1] = 1;
            NeighborOffset[2] = -XNum;
            NeighborOffset[3] = XNum;

            // SRG
            for (int i = 0; i < XNum * YNum; i++)
            {
                // Seed
                if (ImageMask[i] == ObjectLabel && ClusterLabel[i] == Constants.LABEL_BACKGROUND)
                {
                    LabelNum++;
                    List<int> PointQueue = new List<int>();
                    PointQueue.Add(i);
                    ClusterLabel[i] = LabelNum;
                    int CurrentLabelSize = 1;

                    while (PointQueue.Count() > 0 && CurrentLabelSize < ClusterMaxSizeThreshold)
                    {
                        int CurrentVoxelIndex = PointQueue[0];
                        PointQueue.RemoveAt(0);
                        int CurrentVoxelX = CurrentVoxelIndex % XNum;
                        int CurrentVoxelY = CurrentVoxelIndex / XNum;

                        for (int j = 0; j < 4; j++)
                            NeighborCheck[j] = true;
                        if (CurrentVoxelX <= 0)
                            NeighborCheck[0] = false;   // Left
                        if (CurrentVoxelX >= XNum - 1)
                            NeighborCheck[1] = false;   // Right
                        if (CurrentVoxelY <= 0)
                            NeighborCheck[2] = false;   // Top
                        if (CurrentVoxelY >= YNum - 1)
                            NeighborCheck[3] = false;   // Bottom

                        for (int j = 0; j < 4; j++)
                        {
                            if (NeighborCheck[j])
                            {
                                int NeighborVoxelIndex = CurrentVoxelIndex + NeighborOffset[j];
                                if (ImageMask[NeighborVoxelIndex] == ObjectLabel && ClusterLabel[NeighborVoxelIndex] == Constants.LABEL_BACKGROUND)
                                {
                                    ClusterLabel[NeighborVoxelIndex] = LabelNum;
                                    CurrentLabelSize++;
                                    PointQueue.Add(NeighborVoxelIndex);
                                }
                            }
                        }
                    }

                    // Small cluster remove
                    if (PointQueue.Count() == 0 && CurrentLabelSize < ClusterMinSizeThreshold)
                    {
                        for (int j = 0; j < XNum * YNum; j++)
                        {
                            if (ClusterLabel[j] == LabelNum)
                                ClusterLabel[j] = ImageMask[j] = Constants.LABEL_BACKGROUND;
                        }
                        LabelNum--;
                    }
                    PointQueue.Clear();
                }
            }

            return LabelNum;
        }
    }
}
