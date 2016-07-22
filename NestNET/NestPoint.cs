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
            double newx = matrix[0, 0] * this.x + matrix[0, 1] * y + matrix[0, 2];
            double newy = matrix[1, 0] * this.x + matrix[1, 1] * y + matrix[1, 2];
            this.x = newx;
            this.y = newy;
        }

        public NestPoint Clone()
        {
            return new NestPoint(this.x, this.y);
        }
    }
}
