using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestNET
{
    class Program
    {
        static string prefix = "/home/vadim/SvgFiles/SvgSamples/Svg Reader Test/";
        static string[] figs = { "bez.svg", "bez1.svg",  "Circle.svg",  "Transform.svg",  "ёж.svg",  "фигурки1.svg", "move.svg", 
            "Transform2.svg"};
        
        static void Main(string[] args)
        {
            for (int f = 3; f < 4; f++) {
                Console.WriteLine("@DO " + figs[f]);
                NestFigure fig = new NestFigure(prefix + figs[f]);
                Bitmap bmp = new Bitmap(746, 1056);
                Graphics graphContext = Graphics.FromImage(bmp);

                graphContext.Clear(Color.White);
                for (int i = 0; i < fig.points.Length; i++) {
                    for (int j = 0; j < fig.points[i].Length - 1; j++)
                    {
                        float x1, y1, x2, y2;
                        x1 = (float)fig.points[i][j].X;
                        y1 = (float)fig.points[i][j].Y;
                        x2 = (float)fig.points[i][j + 1].X;
                        y2 = (float)fig.points[i][j + 1].Y;
                        graphContext.DrawLine(new Pen(Color.Black, 1.0f), x1, y1, x2, y2); 
                    }
                }

                bmp.Save("out/" + figs[f].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[0] + ".png");
            }
        }
    }
}
