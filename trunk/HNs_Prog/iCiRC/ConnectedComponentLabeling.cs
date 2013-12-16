using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace iCiRC
{
    public class ConnectedComponentLabeling : FrameProcessing
    {
        private int LabelNum;
        private int[,] LabelingBoard;

        public ConnectedComponentLabeling(int Width, int Height)
        {
            XNum = Width;
            YNum = Height;
            LabelNum = 0;
        }

        public int RunCCL(byte[] InputMask, int MinSize, int MaxSize)
        {
            InputFrameMask = InputMask;
            LabelingBoard = new int[XNum, YNum];
            LabelingBoard.Initialize();

            LabelNum = 1;
            Dictionary<int, Label> AllLabels = new Dictionary<int, Label>();
            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int CurrentPixelIndex = y * XNum + x;
                    if (InputFrameMask[CurrentPixelIndex] == Constants.LABEL_BACKGROUND)
                        continue;

                    IEnumerable<int> NeighboringLabels = GetNeighboringLabels(x, y);

                    int CurrentLabel;
                    if (!NeighboringLabels.Any())
                    {
                        CurrentLabel = LabelNum;
                        AllLabels.Add(CurrentLabel, new Label(CurrentLabel));
                        LabelNum++;
                    }
                    else
                    {
                        CurrentLabel = NeighboringLabels.Min(n => AllLabels[n].GetRoot().Name);
                        Label Root = AllLabels[CurrentLabel].GetRoot();

                        foreach (var Neighbor in NeighboringLabels)
                        {
                            if (Root.Name != AllLabels[Neighbor].GetRoot().Name)
                                AllLabels[Neighbor].Join(AllLabels[CurrentLabel]);
                        }
                    }

                    LabelingBoard[x, y] = CurrentLabel;
                }
            }

            Dictionary<int, List<int>> Patterns = AggregatePatterns(AllLabels);

            OutputFrameMask = new byte[XNum * YNum];
            OutputFrameMask.Initialize();
            LabelNum = 0;
            for (int i = 0; i < Patterns.Count; i++)
            {
                int CurrentKey = Patterns.Keys.ElementAt(i);
                if (Patterns[CurrentKey].Count > MinSize && Patterns[CurrentKey].Count < MaxSize)
                {
                    if (LabelNum < 254)
                        LabelNum++;
                    for (int j = 0; j < Patterns[CurrentKey].Count; j++)
                        OutputFrameMask[Patterns[CurrentKey][j]] = Convert.ToByte(LabelNum);
                }
            }

            return LabelNum;
        }

        private IEnumerable<int> GetNeighboringLabels(int CurrentPixelX, int CurrentPixelY)
        {
            var neighboringLabels = new List<int>();

            if (CurrentPixelX > 0 && LabelingBoard[CurrentPixelX - 1, CurrentPixelY] != 0)
                neighboringLabels.Add(LabelingBoard[CurrentPixelX - 1, CurrentPixelY]);
            if (CurrentPixelX < XNum - 1 && LabelingBoard[CurrentPixelX + 1, CurrentPixelY] != 0)
                neighboringLabels.Add(LabelingBoard[CurrentPixelX + 1, CurrentPixelY]);
            if (CurrentPixelY > 0 && LabelingBoard[CurrentPixelX, CurrentPixelY - 1] != 0)
                neighboringLabels.Add(LabelingBoard[CurrentPixelX, CurrentPixelY - 1]);
            if (CurrentPixelY < YNum - 1 && LabelingBoard[CurrentPixelX, CurrentPixelY + 1] != 0)
                neighboringLabels.Add(LabelingBoard[CurrentPixelX, CurrentPixelY + 1]);

            /*
            for (int y = CurrentPixelY - 1; y <= CurrentPixelY + 1 && y < YNum; y++)
            {
                for (int x = CurrentPixelX - 1; x <= CurrentPixelX + 2 && x < XNum; x++)
                {
                    if (x > -1 && y > -1 && LabelingBoard[x, y] != 0)
                        neighboringLabels.Add(LabelingBoard[x, y]);
                }
            }
             */

            return neighboringLabels;
        }

        private Dictionary<int, List<int>> AggregatePatterns(Dictionary<int, Label> AllLabels)
        {
            Dictionary<int, List<int>> Patterns = new Dictionary<int, List<int>>();

            for (int y = 0; y < YNum; y++)
            {
                for (int x = 0; x < XNum; x++)
                {
                    int PatternNumber = LabelingBoard[x, y];
                    if (PatternNumber != 0)
                    {
                        PatternNumber = AllLabels[PatternNumber].GetRoot().Name;
                        if (!Patterns.ContainsKey(PatternNumber))
                            Patterns[PatternNumber] = new List<int>();
                        Patterns[PatternNumber].Add(y * XNum + x);
                    }
                }
            }

            return Patterns;
        }

        private class Label
        {
            #region Public Properties

            public int Name { get; set; }

            public Label Root { get; set; }

            public int Rank { get; set; }
            #endregion

            #region Constructor

            public Label(int Name)
            {
                this.Name = Name;
                this.Root = this;
                this.Rank = 0;
            }

            #endregion

            #region Public Methods

            internal Label GetRoot()
            {
                if (this.Root != this)
                {
                    this.Root = this.Root.GetRoot();
                }

                return this.Root;
            }

            internal void Join(Label root2)
            {
                if (root2.Rank < this.Rank)//is the rank of Root2 less than that of Root1 ?
                {
                    root2.Root = this;//yes! then Root1 is the parent of Root2 (since it has the higher rank)
                }
                else //rank of Root2 is greater than or equal to that of Root1
                {
                    this.Root = root2;//make Root2 the parent
                    if (this.Rank == root2.Rank)//both ranks are equal ?
                    {
                        root2.Rank++;//increment Root2, we need to reach a single root for the whole tree
                    }
                }
            }

            #endregion
        }
    }


}
