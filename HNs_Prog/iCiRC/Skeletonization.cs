using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class Skeletonization : FrameProcessing
    {
        public enum AlgorithmType { PalagyiThinning, ZhangAndSuenThinning, RosenfeldThinning };
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
                case AlgorithmType.RosenfeldThinning:
                    RunRosenfeldThinning();
                    break;
            }

            return OutputFrameMask;
        }

        private void RunRosenfeldThinning()
        {
            int FramePixelNum = XNum * YNum;
            const byte TEMP_LABEL_BACKGROUND = 0x00;
            const byte TEMP_LABEL_FOREGROUND = 0x01;

            int[] NeiborOffset = new int[8];
            NeiborOffset[0] = -XNum + 1;
            NeiborOffset[1] = -XNum;
            NeiborOffset[2] = -XNum - 1;
            NeiborOffset[3] = -1;
            NeiborOffset[4] = XNum - 1;
            NeiborOffset[5] = XNum;
            NeiborOffset[6] = XNum + 1;
            NeiborOffset[7] = 1;

            byte[] TempMaskSrc = new byte[FramePixelNum];
            TempMaskSrc.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (InputFrameMask[i] == Constants.LABEL_FOREGROUND)
                    TempMaskSrc[i] = 0x01;
            }
            byte[] TempMaskDes = new byte[FramePixelNum];
            TempMaskDes = (byte[])TempMaskSrc.Clone();

            bool CheckWhile = true;
            while (CheckWhile)
            {
                CheckWhile = false;
                for (int i = 1; i < 8; i += 2)
                {
                    for (int y = 1; y < YNum - 1; y++)
                    {
                        for (int x = 1; x < XNum - 1; x++)
                        {
                            int CurrentPixelIndex = y * XNum + x;
                            int NeiborPixelIndex = CurrentPixelIndex + NeiborOffset[i];
                            if (TempMaskSrc[CurrentPixelIndex] == TEMP_LABEL_BACKGROUND || TempMaskSrc[NeiborPixelIndex] == TEMP_LABEL_FOREGROUND)
                                continue;

                            byte NeiborMaskSum = 0;
                            byte[] NeiborMask = new byte[8];
                            for (int n = 0; n < 8; n++)
                            {
                                NeiborMask[n] = TempMaskSrc[CurrentPixelIndex + NeiborOffset[n]];
                                NeiborMaskSum += NeiborMask[n];
                            }
                            if (NeiborMaskSum <= 1)
                                continue;

                            bool ContinueCheck = false;
                            int n48 = NeiborMask[3] + NeiborMask[7];
                            int n26 = NeiborMask[1] + NeiborMask[5];
                            int n24 = NeiborMask[1] + NeiborMask[3];
                            int n46 = NeiborMask[3] + NeiborMask[5];
                            int n68 = NeiborMask[5] + NeiborMask[7];
                            int n82 = NeiborMask[7] + NeiborMask[1];
                            int n123 = NeiborMask[0] + NeiborMask[1] + NeiborMask[2];
                            int n345 = NeiborMask[2] + NeiborMask[3] + NeiborMask[4];
                            int n567 = NeiborMask[4] + NeiborMask[5] + NeiborMask[6];
                            int n781 = NeiborMask[6] + NeiborMask[7] + NeiborMask[0];

                            if ((NeiborMask[1] == TEMP_LABEL_FOREGROUND && n48 == 0 && n567 > 0)
                                || (NeiborMask[5] == TEMP_LABEL_FOREGROUND && n48 == 0 && n123 > 0)
                                || (NeiborMask[7] == TEMP_LABEL_FOREGROUND && n26 == 0 && n345 > 0)
                                || (NeiborMask[3] == TEMP_LABEL_FOREGROUND && n26 == 0 && n781 > 0))
                            {
                                if (!ContinueCheck)
                                    continue;
                                TempMaskDes[CurrentPixelIndex] = TEMP_LABEL_BACKGROUND;
                                CheckWhile = true;
                                continue;
                            }

                            if ((NeiborMask[4] == TEMP_LABEL_FOREGROUND && n46 == 0)
                                || (NeiborMask[6] == TEMP_LABEL_FOREGROUND && n68 == 0)
                                || (NeiborMask[0] == TEMP_LABEL_FOREGROUND && n82 == 0)
                                || (NeiborMask[2] == TEMP_LABEL_FOREGROUND && n24 == 0))
                            {
                                if (!ContinueCheck)
                                    continue;
                                TempMaskDes[CurrentPixelIndex] = TEMP_LABEL_BACKGROUND;
                                CheckWhile = true;
                                continue;
                            }

                            ContinueCheck = true;
                            if (!ContinueCheck)
                                continue;
                            TempMaskDes[CurrentPixelIndex] = TEMP_LABEL_BACKGROUND;
                            CheckWhile = true;
                        }
                    }

                    TempMaskSrc = (byte[])TempMaskDes.Clone();
                }
            }

            OutputFrameMask = new byte[FramePixelNum];
            OutputFrameMask.Initialize();
            for (int i = 0; i < FramePixelNum; i++)
            {
                if (TempMaskSrc[i] == TEMP_LABEL_FOREGROUND)
                    OutputFrameMask[i] = Constants.LABEL_FOREGROUND;
            }
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
