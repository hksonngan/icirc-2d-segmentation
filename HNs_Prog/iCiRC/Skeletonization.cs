using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class Skeletonization : FrameProcessing
    {
        public enum AlgorithmType { PalagyiThinning, ZhangAndSuenThinning };
        public AlgorithmType AlgType;

        private enum Direction { Left, Right, Up, Down };

        public Skeletonization()
        {
            AlgType = AlgorithmType.PalagyiThinning;
        }

        public Skeletonization(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
            AlgType = AlgorithmType.PalagyiThinning;
        }

        public byte[] RunSkeletonization(byte[] Mask)
        {
            InputFrameMask = Mask;

            switch (AlgType)
            {
                case AlgorithmType.PalagyiThinning:
                    RunPalagyiThinning();
                    break;
                case AlgorithmType.ZhangAndSuenThinning:
                    break;
            }

            return OutputFrameMask;
        }

        private void RunPalagyiThinning()
        {
            int FramePixelNum = XNum * YNum;
            OutputFrameMask = new byte[FramePixelNum];
            OutputFrameMask = (byte[])InputFrameMask.Clone();

            int DeletedPixelCnt = 1;
            while (DeletedPixelCnt > 0)
            {
                DeletedPixelCnt = 0;
                DeletedPixelCnt += PalagyiThinningSubIter(Direction.Up);
                DeletedPixelCnt += PalagyiThinningSubIter(Direction.Down);
                DeletedPixelCnt += PalagyiThinningSubIter(Direction.Right);
                DeletedPixelCnt += PalagyiThinningSubIter(Direction.Left);
            }
        }

        private int PalagyiThinningSubIter(Direction BorderDirection)
        {
            int FramePixelNum = XNum * YNum;
            int DeletedPixelCnt = 0;

            int[] NieghborPixelOffset= new int[8];
            NieghborPixelOffset[0] = -1 - XNum;
            NieghborPixelOffset[1] =    - XNum;
            NieghborPixelOffset[2] =  1 - XNum;
            NieghborPixelOffset[3] = -1;
            NieghborPixelOffset[4] =  1;
            NieghborPixelOffset[5] = -1 + XNum;
            NieghborPixelOffset[6] =      XNum;
            NieghborPixelOffset[7] =  1 + XNum;

            int DirectionPixelOffset = 0;
            switch (BorderDirection)
            {
                case Direction.Left:
                    DirectionPixelOffset = -1;
                    break;
                case Direction.Right:
                    DirectionPixelOffset = 1;
                    break;
                case Direction.Up:
                    DirectionPixelOffset = -XNum;
                    break;
                case Direction.Down:
                    DirectionPixelOffset = XNum;
                    break;
            }

            List<int> SimplePointList = new List<int>();
            for (int i = 0; i < FramePixelNum; i++)
            {
                
                int CurrentPixelX = i % XNum;
                int CurrentPixelY = i / XNum;
                if (CurrentPixelX == 0 || CurrentPixelX == XNum - 1 || CurrentPixelY == 0 || CurrentPixelY == YNum - 1)
                    continue;

                // Is border point
                if (OutputFrameMask[i] == Constants.LABEL_FOREGROUND
                    && OutputFrameMask[i + DirectionPixelOffset] == Constants.LABEL_BACKGROUND)
                {
                    byte[] NeighborArray = new byte[8];
                    for (int j = 0; j < 8; j++)
                        NeighborArray[j] = OutputFrameMask[i + NieghborPixelOffset[j]];

                    // Is end point
                    int SumNieghbor = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        if (NeighborArray[j] == Constants.LABEL_FOREGROUND)
                            SumNieghbor++;
                    }
                    if (SumNieghbor > 1)    
                    {
                        // Is simlple
                        if (PalagyiCheckingConditionTwo(NeighborArray) && PalagyiCheckingConditionFour(NeighborArray))
                            SimplePointList.Add(i);
                    }
                }
            }

            while (SimplePointList.Count > 0)
            {
                int CurrentPixelIndex = SimplePointList[0];
                SimplePointList.RemoveAt(0);

                byte[] NeighborArray = new byte[8];
                for (int j = 0; j < 8; j++)
                    NeighborArray[j] = OutputFrameMask[CurrentPixelIndex + NieghborPixelOffset[j]];

                // Is end point
                int SumNieghbor = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (NeighborArray[j] == Constants.LABEL_FOREGROUND)
                        SumNieghbor++;
                }
                if (SumNieghbor > 1)
                {
                    // Is simlple
                    if (PalagyiCheckingConditionTwo(NeighborArray) && PalagyiCheckingConditionFour(NeighborArray))
                    {
                        OutputFrameMask[CurrentPixelIndex] = Constants.LABEL_BACKGROUND;
                        DeletedPixelCnt++;
                    }
                }

            }
            return DeletedPixelCnt;
        }

        private bool PalagyiCheckingConditionTwo(byte[] NeighborArray)
        {
            List<int>[] SetArray = new List<int>[8];
            for (int i = 0; i < 8; i++)
                SetArray[i] = new List<int>();
            SetArray[1].Add(0);
            SetArray[2].Add(1);
            SetArray[3].Add(0); SetArray[3].Add(1);
            SetArray[4].Add(1); SetArray[4].Add(2);
            SetArray[5].Add(3);
            SetArray[6].Add(3); SetArray[6].Add(4); SetArray[6].Add(5);
            SetArray[7].Add(4); SetArray[7].Add(6);

            int label = 0;
            int[] LabelArray = new int[8];
            LabelArray.Initialize();
            for (int i = 0; i < 8; i++)
            {
                if (NeighborArray[i] == Constants.LABEL_FOREGROUND)
                {
                    label++;
                    LabelArray[i] = label;
                }
                for (int j = 0; j < SetArray[i].Count; j++)
                {
                    if (LabelArray[SetArray[i][j]] > 0)
                    {
                        for (int k = 0; k < i; k++)
                        {
                            if (LabelArray[k] == LabelArray[SetArray[i][j]])
                                LabelArray[k] = label;
                        }
                    }
                }
            }

            for (int i = 0; i < 8; i++)
            {
                if (NeighborArray[i] == Constants.LABEL_FOREGROUND && LabelArray[i] != label)
                    return false;
            }
            return true;
        }

        private bool PalagyiCheckingConditionFour(byte[] NeighborArray)
        {
            int label = 0;
            int[] LabelArray = new int[8];
            List<int>[] SetArray = new List<int>[8];
            for (int i = 0; i < 8; i++)
                SetArray[i] = new List<int>();
            SetArray[1].Add(0);
            SetArray[2].Add(1);
            SetArray[3].Add(0); SetArray[3].Add(1);
            SetArray[4].Add(1); SetArray[4].Add(2);
            SetArray[5].Add(3);
            SetArray[6].Add(3); SetArray[6].Add(4); SetArray[6].Add(5);
            SetArray[7].Add(4); SetArray[7].Add(6);
            /*
            SetArray[1].Add(0);
            SetArray[2].Add(1);
            SetArray[3].Add(0);
            SetArray[4].Add(2);
            SetArray[5].Add(3);
            SetArray[6].Add(5);
            SetArray[7].Add(4); SetArray[7].Add(6);
             */

            LabelArray.Initialize();
            for (int i = 0; i < 8; i++)
            {
                if (NeighborArray[i] == Constants.LABEL_BACKGROUND && (i == 1 || i == 3 || i == 4 || i == 6))
                {
                    label++;
                    LabelArray[i] = label;
                }
                for (int j = 0; j < SetArray[i].Count; j++)
                {
                    if (LabelArray[SetArray[i][j]] > 0)
                    {
                        for (int k = 0; k < i; k++)
                        {
                            if (LabelArray[k] == LabelArray[SetArray[i][j]])
                                LabelArray[k] = label;
                        }
                    }
                }
            }

            for (int i = 0; i < 8; i++)
            {
                if (NeighborArray[i] == Constants.LABEL_BACKGROUND && (i == 1 || i == 3 || i == 4 || i == 6))
                {
                    if (LabelArray[i] != label)
                        return false;
                }
            }
            return true;
        }
    }
}
