using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class Clustering
    {
        public int[] ClusterLabel;
        protected int XNum, YNum, LabelNum;
        protected byte[] ImageMask;
        protected byte ObjectLabel;
        protected int DataDimension;

        public Clustering()
        {
            DataDimension = 1;
        }
    }
}
