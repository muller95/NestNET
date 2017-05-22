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

            set
            {
                x = value;
            }
        }

        public double Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public override string ToString() 
        {
            return String.Format("({0}, {1})", x, y);
        }

        public static NestPoint operator +(NestPoint a, NestPoint b) 
        {
            double newx = a.x + b.x;
            double newy = a.y + b.y;
            
            return new NestPoint(newx, newy);
        }

        public static NestPoint operator *(double c, NestPoint p) 
        {
            double newx = c * p.x;
            double newy = c * p.y;

            return new NestPoint(newx, newy);
        }

        public static NestPoint operator -(NestPoint a, NestPoint b) 
        {
            double newx = a.x - b.x;
            double newy = a.y - b.y;
            
            return new NestPoint(newx, newy);
        }
    }
}
