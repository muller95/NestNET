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
            "Transform2.svg", "rotate.svg"};

        static string prefixSet1 = "/home/vadim/SvgFiles/SvgSamples/set1/";
        static string prefixSet2 = "/home/vadim/SvgFiles/SvgSamples/set2/";
        static string[] set1 = { "фигурки1.svg",  "фигурки2.svg",  "фигурки3.svg",  "фигурки4.svg",  "фигурки5.svg"};
        static string[] set2 = { "snezhinka.svg",  "podkova.svg" };
        
        static void Main(string[] args)
        {
            for (int f = 0; f < figs.Length; f++) 
            {
                NestFigure fig = new NestFigure(prefix + figs[f]);
                Bitmap bmp = new Bitmap(746, 1056);
                Graphics graphContext = Graphics.FromImage(bmp);

                graphContext.Clear(Color.White);
                for (int i = 0; i < fig.points.Length; i++) 
                {
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

            for (int f = 0; f < set1.Length; f++) 
            {
                NestFigure fig = new NestFigure(prefixSet1 + set1[f]);
                Console.WriteLine("5 45");
                for (int i = 0; i < fig.points.Length; i++) 
                {
                    for (int j = 0; j < fig.points[i].Length; j++)
                        Console.WriteLine(fig.points[i][j]);

                    if (i != fig.points.Length - 1)
                        Console.WriteLine("");
                }
                Console.WriteLine(":");
            }
        }
    }
}
