using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iCiRC
{
    class Point2D
    {
        public int x, y;

        public Point2D()
        {
            x = y = 0;
        }

        public Point2D(int inX, int inY)
        {
            x = inX;
            y = inY;
        }
    }

    class Point3D
    {
        public int x, y, z;

        public Point3D()
        {
            x = y = z = 0;
        }

        public Point3D(int inX, int inY, int inZ)
        {
            x = inX;
            y = inY;
            z = inZ;
        }
    }
}
