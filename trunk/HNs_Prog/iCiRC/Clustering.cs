using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    public class Clustering
    {
        public enum DistanceCriterion { Intensity, Position };
        public DistanceCriterion CriterionType;
        public int[] ClusterLabel;
        protected int XNum, YNum, LabelNum;
        protected byte[] ImageMask;
        protected byte ObjectLabel;

        public Clustering()
        {
            CriterionType = DistanceCriterion.Intensity;
        }
    }
}
