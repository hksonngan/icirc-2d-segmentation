using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace iCiRC
{
    public class BallFilter
    {
        private List<Point> CircleInsidePoints;
        private List<Point> CircleBoundaryPoints;
        private double Radius;

        public BallFilter(double CircleRadius)
        {
            Radius = CircleRadius;
            CircleInsidePoints = new List<Point>();
            CircleBoundaryPoints = new List<Point>();
            int SearchRange = Convert.ToInt32(CircleRadius + 1.5);
            for (int y = -SearchRange; y <= SearchRange; y++)
            {
                for (int x = -SearchRange; x <= SearchRange; x++)
                {
                    if (Math.Abs(Math.Sqrt(x * x + y * y) - Radius) <= 0.5)
                        CircleBoundaryPoints.Add(new Point(x, y));
                    else if (Math.Sqrt(x * x + y * y) < Radius - 0.5)
                        CircleInsidePoints.Add(new Point(x, y));
                }
            }
        }

        public int GetInsideObject(int CurrentPixelIndex, int XNum, int YNum, byte[] LabelMask)
        {
            int InsideObject = 0;
            int CurrentPixelIndexX = CurrentPixelIndex % XNum;
            int CurrentPixelIndexY = CurrentPixelIndex / XNum;
            for (int i = 0; i < CircleInsidePoints.Count; i++)
            {
                int CurrentFilterPointX = CurrentPixelIndexX + CircleInsidePoints[i].X;
                int CurrentFilterPointY = CurrentPixelIndexY + CircleInsidePoints[i].Y;
                if (CurrentFilterPointX < 0 || CurrentFilterPointY < 0 || CurrentFilterPointX > XNum - 1 || CurrentFilterPointY > YNum - 1)
                    continue;

                if (LabelMask[CurrentFilterPointY * XNum + CurrentFilterPointX] == Constants.LABEL_FOREGROUND)
                    InsideObject++;
            }
            return InsideObject;
        }

        public int GetBoundaryObject(int CurrentPixelIndex, int XNum, int YNum, byte[] LabelMask)
        {
            int BoundaryObject = 0;
            int CurrentPixelIndexX = CurrentPixelIndex % XNum;
            int CurrentPixelIndexY = CurrentPixelIndex / XNum;
            for (int i = 0; i < CircleBoundaryPoints.Count; i++)
            {
                int CurrentFilterPointX = CurrentPixelIndexX + CircleBoundaryPoints[i].X;
                int CurrentFilterPointY = CurrentPixelIndexY + CircleBoundaryPoints[i].Y;
                if (CurrentFilterPointX < 0 || CurrentFilterPointY < 0 || CurrentFilterPointX > XNum - 1 || CurrentFilterPointY > YNum - 1)
                    continue;

                if (LabelMask[CurrentFilterPointY * XNum + CurrentFilterPointX] == Constants.LABEL_FOREGROUND)
                    BoundaryObject++;
            }
            return BoundaryObject;
        }

        public int GetInsidePoint()
        {
            return CircleInsidePoints.Count;
        }

        public int GetBoundaryPoint()
        {
            return CircleBoundaryPoints.Count;
        }
    }
}
