using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestNET
{
    class NestPoint
    {
        private double x, y;
        public NestPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public void ApplyTransform(double[,] matrix)
        {
            double newx = matrix[0, 0] * x + matrix[0, 1] * y + matrix[0, 2];
            double newy = matrix[1, 0] * x + matrix[1, 1] * y + matrix[1, 2];
            x = newx;
            y = newy;
        }

        public NestPoint Clone()
        {
            return new NestPoint(x, y);
        }

        public double X
        {
            get
            {
                return x;
            }
        }

        public double Y
        {
            get
            {
                return y;
            }
        }
    }
}
