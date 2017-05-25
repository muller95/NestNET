using System;
using System.IO;

namespace OutputParser
{
    class Program
    {
        static string prefixSet1 = "/home/vadim/SvgFiles/SvgSamples/set1/";        
        static string prefixSet2 = "/home/vadim/SvgFiles/SvgSamples/set2/";
        static string[] set1 = { "фигурки1.svg",  "фигурки2.svg",  "фигурки3.svg",  "фигурки4.svg",  "фигурки5.svg"};
        
        static void Main(String[] args)
        {
            StreamReader sr = new StreamReader("set1");
            StreamWriter result = new StreamWriter("set1out.svg");

            result.WriteLine("<svg>");
            while (!sr.EndOfStream) {
                string str1 = sr.ReadLine();
                if (str1 == ":")
                    continue;
                
                string str2 = sr.ReadLine();

                int id = Convert.ToInt32(str1);

                Console.WriteLine("str1 " +  str1);
                Console.WriteLine("str2 " + str2);

                StreamReader figReader = new StreamReader(prefixSet1 + set1[id]);
                result.Write(figReader.ReadToEnd().Replace("<svg>", String.Format("<g transform=\"{0}\"", str2).Replace("</svg>", "</g>")));
            }
            result.WriteLine("</svg>");            
            result.Close();
        }
    }
}