using System;
using System.IO;
using System.Diagnostics;

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

            ProcessStartInfo startInfo;        
            int count = 0;
            startInfo = new ProcessStartInfo ("gawk", string.Format (
                "-f backend.awk -v width=\"1000\" -v height=\"1000\" -v outfile=\"{0}\"", String.Format("out/{0}.svg", count)));
			startInfo.RedirectStandardOutput = false;	
			startInfo.RedirectStandardInput = true;
			startInfo.UseShellExecute = false;
			Process gawkout = new Process ();
			gawkout.StartInfo = startInfo;
			gawkout.Start ();

            while (!sr.EndOfStream) {
                string str1 = sr.ReadLine();
                if (str1 == ":")
                {
                    count++;
                    gawkout.StandardInput.Close();
                    startInfo = new ProcessStartInfo ("gawk", string.Format (
                        "-f backend.awk -v width=\"1000\" -v height=\"1000\" -v outfile=\"{0}\"", String.Format("out/{0}.svg", count)));
                    startInfo.RedirectStandardOutput = false;	
                    startInfo.RedirectStandardInput = true;
                    startInfo.UseShellExecute = false;
                    gawkout = new Process ();
                    gawkout.StartInfo = startInfo;
                    gawkout.Start ();
                    continue;
                }
                
                string str2 = sr.ReadLine();

                int id = Convert.ToInt32(str1);
                Console.WriteLine(prefixSet1 + set1[id]);
                gawkout.StandardInput.WriteLine(prefixSet1 + set1[id]);
                gawkout.StandardInput.WriteLine(str2);
                gawkout.StandardInput.WriteLine(":");
            }
        }
    }
}