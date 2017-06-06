using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NestNET
{
    class NestFigure
    {
        public NestPoint[][] points;
        private int initPrims = 16, initPoints = 1024;
        private int nmbPrims, nmbPoints;
        private bool isFigure;
        private double tstep = 0.01;
        public NestFigure(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode root = doc.GetElementsByTagName("svg")[0];

            nmbPoints = nmbPrims = 0;
            points = new NestPoint[initPrims][];

            for (int i = 0; i < root.ChildNodes.Count; i++)
                ParseNode(root.ChildNodes[i], "1_0_0_0_1_0_0_0_1");

            Array.Resize(ref points, nmbPrims);
        }

        private string[] GetTransformInfo(string transform)
        {
            int start = 0;
            int len = transform.IndexOf('(');
            string name = transform.Substring(start, len);
            start = len + 1;
            len = transform.IndexOf(')') - start;
            string param = transform.Substring(start, len);
            return new string[] { name, param };
        }

        private string MatrixToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return String.Format("{0}_{1}_{2}_{3}_{4}_{5}_0_0_1", vals[0], vals[2], vals[4], vals[1], vals[3], vals[5]);
        }

        private string TranslateToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length > 1)
                return String.Format("1_0_{0}_0_1_{1}_0_0_1", vals[0], vals[1]);
            else
                return String.Format("1_0_{0}_0_1_0_0_0_1", vals[0]);
        }

        private String ScaleToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length > 1)
                return String.Format("{0}_0_0_0_{1}_0_0_0_1", vals[0], vals[1]);
            else
                return String.Format("{0}_0_0_0_{1}_0_0_0_1", vals[0], vals[0]);
        }

        private String RotateToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double a = Convert.ToDouble(vals[0]);
            a = Math.PI * a / 180.0;
            double sin = Math.Sin(a);
            double cos = Math.Cos(a);
            string mtx2 = String.Format("{0}_{1}_0_{2}_{3}_0_0_0_1", cos, -sin, sin, cos);
            if (vals.Length < 3)
                return mtx2;
            else
            {
                double m = Convert.ToDouble(vals[1]);
                double n = Convert.ToDouble(vals[2]);
                string mtx1 = String.Format("1_0_0_0_1_0_{0}_{1}_1", -1 * m, -1 * n);
                string mtx3 = String.Format("1_0_0_0_1_0_{0}_{1}_1", m, n);
                return mtx1 + " " + mtx2 + " " + mtx3;
            }
        }

        private String SkewXToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double a = Convert.ToDouble(vals[0]);
            a = Math.PI * a / 180.0;
            double tan = Math.Tan(a);
            return String.Format("1_{0}_0_0_1_0_0_0_1", tan);
        }

        private String SkewYToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double a = Convert.ToDouble(vals[0]);
            a = Math.PI * a / 180.0;
            double tan = Math.Tan(a);
            return String.Format("1_0_0_{0}_1_0_0_0_1", tan);
        }

        private string[] SplitTransforms(string transformAttr)
        {
            string[] transforms = transformAttr.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < transforms.Length; i++)
                transforms[i] = (transforms[i] + ")").Trim();
            return transforms;
        }

        private string ParseTransformAttr(string transformAttr)
        {
            // string[] transforms = transformAttr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] transforms = SplitTransforms(transformAttr);
            string result = "";
            
            for (int i = 0; i < transforms.Length; i++)
            {
                string[] info = GetTransformInfo(transforms[i]);
                switch (info[0])
                {
                    case "matrix":
                        result += " " + MatrixToString(info[1]);
                        break;
                    case "translate":
                        result += " " + TranslateToString(info[1]);
                        break;
                    case "scale":
                        result += " " + ScaleToString(info[1]);
                        break;
                    case "rotate":
                        result += " " + RotateToString(info[1]);
                        break;
                    case "skewX":
                        result += " " + SkewXToString(info[1]);
                        break;
                    case "skewY":
                        result += " " + SkewYToString(info[1]);
                        break;
                }
            }

            return result.Trim();
        }

        private double[,] MatrixFromString(string transform)
        {
            double[,] matrix = new double[3, 3];

            string[] vals = transform.Split(new char[] { '_' });

            for (int i = 0; i < vals.Length; i++)
                matrix[i / 3, i % 3] = Convert.ToDouble(vals[i].Replace(".", ","));

            return matrix;
        }

        private double[,] MultiplyMatrixes(double[,] matrix1, double[,] matrix2)
        {
            double[,] result = new double[3, 3];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int r = 0; r < 3; r++)
                        result[i, j] += matrix1[i, r] * matrix2[r, j];

            return result;
        }

        private double[,] MultiplyTransforms(string transform)
        {
            double[,] matrix1 = new double[3, 3];

            for (int i = 0; i < 3; i++)
                matrix1[i, i] = 1;

            string[] transforms = transform.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < transforms.Length; i++)
            {
                double[,] matrix2 = MatrixFromString(transforms[i]);
                matrix1 = MultiplyMatrixes(matrix1, matrix2);
            }

            return matrix1;
        }

        private void CircleToPoints(XmlNode node, string transform)
        {
            double cx, cy, r;
            double step, len;
            double[,] matrix;

            matrix = MultiplyTransforms(transform);
            cx = double.Parse(node.Attributes["cx"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            cy = double.Parse(node.Attributes["cy"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            r = double.Parse(node.Attributes["r"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            len = 2 * Math.PI * r;
            step = 1.0 / len;

            for (double t = 0.0; t <= 2 * Math.PI; t += step)
            {
                double x = r * Math.Cos(t) + cx;
                double y = r * Math.Sin(t) + cy;
                points[nmbPrims][nmbPoints] = new NestPoint(x, y);
                points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                if (nmbPoints == points[nmbPrims].Length)
                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            }
            isFigure = true;
        }

        private void EllipseToPoints(XmlNode node, string transform)
        {
            double cx, cy, rx, ry;
            double step, len;
            double[,] matrix;
            
            matrix = MultiplyTransforms(transform);
            cx = double.Parse(node.Attributes["cx"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            cy = double.Parse(node.Attributes["cy"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            rx = double.Parse(node.Attributes["rx"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            ry = double.Parse(node.Attributes["ry"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            len = (4 * Math.PI * rx * ry + Math.Pow(rx - ry, 2)) / (rx + ry);
            step = 1.0 / (2 * Math.PI * len);

            for (double t = 0.0; t <= 2 * Math.PI; t += step)
            {
                double x = rx * Math.Cos(t) + cx;
                double y = ry * Math.Sin(t) + cy;
                points[nmbPrims][nmbPoints] = new NestPoint(x, y);
                points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                if (nmbPoints == points[nmbPrims].Length)
                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            }
            isFigure = true;
        }

        private void LineToPoints(XmlNode node, string transform)
        {
            double x1, y1, x2, y2;
            double[,] matrix;

            matrix = MultiplyTransforms(transform);
            x1 = double.Parse(node.Attributes["x1"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            y1 = double.Parse(node.Attributes["y1"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            x2 = double.Parse(node.Attributes["x2"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            y2 = double.Parse(node.Attributes["y2"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);

            points[nmbPrims][nmbPoints] = new NestPoint(x1, y1);
            points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
            if (nmbPoints == points[nmbPrims].Length)
                Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            points[nmbPrims][nmbPoints] = new NestPoint(x2, y2);
            points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
            if (nmbPoints == points[nmbPrims].Length)
                Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            isFigure = true;
        }

        private void PolylineToPoints(XmlNode node, string transform)
        {
            double[,] matrix;
            string[] coordinates = node.Attributes["points"].Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            matrix = MultiplyTransforms(transform);

            for (int i = 0; i < coordinates.Length; i += 2)
            {
                double x = double.Parse(coordinates[i], NumberStyles.Any, CultureInfo.InvariantCulture);
                double y = double.Parse(coordinates[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                points[nmbPrims][nmbPoints] = new NestPoint(x, y);
                points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                if (nmbPoints == points[nmbPrims].Length)
                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            }
            isFigure = true;
        }

        private void PolygonToPoints(XmlNode node, string transform)
        {
            double[,] matrix;
            string[] coordinates = node.Attributes["points"].Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            matrix = MultiplyTransforms(transform);

            for (int i = 0; i < coordinates.Length; i += 2)
            {
                double x = double.Parse(coordinates[i], NumberStyles.Any, CultureInfo.InvariantCulture);
                double y = double.Parse(coordinates[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                points[nmbPrims][nmbPoints] = new NestPoint(x, y);
                points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                if (nmbPoints == points[nmbPrims].Length)
                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            }

            points[nmbPrims][nmbPoints++] = points[nmbPrims][0].Clone();
            isFigure = true;
        }


        private void RelToAbsLine(NestPoint[] points, NestPoint curr) 
        {
            points[0] = points[0] + curr;
            for (int i = 1; i < points.Length; i++)
                points[i] = points[i] + points[i - 1];
        }

        private void RelToAbsBezier(NestPoint[] points, NestPoint curr, int degree)
        {
            NestPoint c = curr.Clone();
            for (int i = 0; i < points.Length; i += degree) 
            {
                for (int j = 0; j < degree; j++)
                    points[i + j] = points[i + j] + c;
                c = points[i + degree - 1].Clone();
            }
        }

        private void RelToAbsArc(NestPoint[] points, NestPoint curr)
        {
            points[0] = points[0] + curr;
            for (int i = 1; i < points.Length; i++)
                points[i] = points[i] + points[i - 1];
        }

        private List<List<Tuple<string, double[]>>> GetSubpaths(string cmdStr) 
        {
            List<List<Tuple<string, double[]>>> result = new List<List<Tuple<string, double[]>>>();
            List<Tuple<string, double[]>> subPath = new List<Tuple<string, double[]>>();            
            string commandSet = "mMlLzZhHvVcCsSqQtTaA";
            cmdStr = cmdStr.Trim().Replace("-", ",-");;
            if (cmdStr.Substring(0, 1) == ",") {
                cmdStr = cmdStr.Substring(1);
            }
            cmdStr = cmdStr.Replace(" ", ",");
            cmdStr = cmdStr.Replace(",,", ",");
            cmdStr = cmdStr.Replace("e,", "e");
            
            for (int i = 0; i < cmdStr.Length;) {
                string cmd = cmdStr.Substring(i, 1);
                string argsStr = "", curr = "";
                if (commandSet.IndexOf(cmd) >= 0) {
                    i++;
                    for (; i < cmdStr.Length; i++) {
                        curr = cmdStr.Substring(i, 1);
                        if (commandSet.IndexOf(curr) >= 0)
                            break;
                        argsStr += cmdStr.Substring(i, 1);
                    }

                }
                argsStr = argsStr.Trim();
                string[] argsStrArr = argsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                double[] args = new double[argsStrArr.Length];
                for (int j = 0; j < args.Length; j++)
                    args[j] = Convert.ToDouble(argsStrArr[j].Replace(".", ","));
                subPath.Add(new Tuple<string, double[]>(cmd, args));
                if ("mM".IndexOf(curr) >= 0) {
                    result.Add(subPath);
                    subPath = new List<Tuple<string, double[]>>();
                } 
            }

            result.Add(subPath);

            return result;
        }

        private NestPoint calcBezier3Point(NestPoint[] p, double t) 
        {
            return (1 - t)*(1 - t)*(1 - t) * p[0] + 3*t * (1 - t)*(1 - t) * p[1] + 3*t*t * (1 - t) * p[2] + t*t*t * p[3];
        }

        private NestPoint calcBezier2Point(NestPoint[] p, double t) 
        {
            return (1 - t)*(1 - t) * p[0] + 2*t * (1 - t) * p[1] + t*t * p[2];
        }

        private double svgSignum(double n) {
            if (n < 0) 
                return -1;
            else 
                return 1;
        }

        private void PathToPoints(string cmdStr, string transform)
        {
            double[,] matrix = MultiplyTransforms(transform);
            
            List<List<Tuple<string, double[]>>> subPaths = GetSubpaths(cmdStr);
            NestPoint first = new NestPoint(0, 0);
            NestPoint curr = new NestPoint(0, 0);
            NestPoint prev = new NestPoint(0, 0);
            bool prevCubic, prevQuadr;
            prevCubic = prevQuadr = false;
            
            for (int sp = 0; sp < subPaths.Count; sp++) {
                List<Tuple<string, double[]>> commands = subPaths[sp];
                points[nmbPrims] = new NestPoint[initPoints];
                for (int i = 0; i < commands.Count; i++) {
                    NestPoint[] pathPoints;
                    string cmd = commands[i].Item1;
                    double[] args = commands[i].Item2;            
                    int degree;
                        
                    switch (cmd) {
                        case "m":
                        case "M":
                        case "l":
                        case "L":
                            pathPoints = new NestPoint[args.Length / 2];
                            for (int j = 0; j < args.Length; j += 2) 
                                pathPoints[j/2] = new NestPoint(args[j], args[j + 1]);

                            if ("ml".IndexOf(cmd) >= 0)
                                RelToAbsLine(pathPoints, curr);

                            if (pathPoints.Length > 1 || "lL".IndexOf(cmd) >= 0) {
                                if ("mM".IndexOf(cmd) < 0) 
                                {
                                    points[nmbPrims][nmbPoints] = curr.Clone();
                                    points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                       
                                    if (nmbPoints == points[nmbPrims].Length)
                                        Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                                }
                                for (int j = 0; j < pathPoints.Length; j++) {
                                    points[nmbPrims][nmbPoints] = pathPoints[j].Clone();
                                    points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                    if (nmbPoints == points[nmbPrims].Length)
                                        Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                                }
                            }
                            first = ("mM".IndexOf(cmd) >= 0) ? pathPoints[0].Clone() : first;
                            curr = pathPoints[pathPoints.Length - 1].Clone();

                            prevCubic = prevQuadr = false;
                            
                        break;

                        case "z":
                        case "Z":
                            points[nmbPrims][nmbPoints] = first.Clone();
                            points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                            if (nmbPoints == points[nmbPrims].Length)
                                Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                            curr = first.Clone();
                            prevCubic = prevQuadr = false;
                        break;

                        case "v":
                        case "V":
                        case "h":
                        case "H":
                            pathPoints = new NestPoint[args.Length];
                            for (int j = 0; j < args.Length; j++) {
                                if (cmd == "v")
                                    pathPoints[j] = new NestPoint(0, args[j]);
                                else if (cmd == "V")
                                    pathPoints[j] = new NestPoint(curr.X, args[j]);
                                else if (cmd == "h")
                                    pathPoints[j] = new NestPoint(args[j], 0);
                                else if (cmd == "H")
                                    pathPoints[j] = new NestPoint(args[j], curr.Y);                  
                            }

                            if ("vh".IndexOf(cmd) >= 0)
                                RelToAbsLine(pathPoints, curr);

                            points[nmbPrims][nmbPoints] = curr.Clone();
                            points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                
                            if (nmbPoints == points[nmbPrims].Length)
                                Array.Resize(ref points[nmbPrims], nmbPoints * 2);

                            for (int j = 0; j < pathPoints.Length; j++) {
                                points[nmbPrims][nmbPoints] = pathPoints[j].Clone();
                                points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                if (nmbPoints == points[nmbPrims].Length)
                                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                            }
                            
                            curr = pathPoints[pathPoints.Length - 1].Clone();

                            prevCubic = prevQuadr = false;                                                
                        break;

                        case "c":
                        case "C":
                        case "q":
                        case "Q":
                            pathPoints = new NestPoint[args.Length / 2];
                            for (int j = 0; j < args.Length; j += 2) 
                                pathPoints[j/2] = new NestPoint(args[j], args[j + 1]);
                            
                            degree = ("cC".IndexOf(cmd) >= 0) ? 3 : 2;
                            if ("cq".IndexOf(cmd) >= 0)
                                RelToAbsBezier(pathPoints, curr, degree);
                            
                            for (int j = 0; j < pathPoints.Length; j+=degree)
                            {
                                NestPoint[] p;
                                if (degree == 3) 
                                    p = new NestPoint[]{ curr.Clone(), pathPoints[j].Clone(), pathPoints[j + 1].Clone(), pathPoints[j + 2].Clone() };
                                else
                                    p = new NestPoint[]{ curr.Clone(), pathPoints[j].Clone(), pathPoints[j + 1].Clone() };
                                for (double t = 0.0; t <= 1.0; t += tstep) 
                                {
                                    if (degree == 3) {
                                        points[nmbPrims][nmbPoints] = calcBezier3Point(p, t);
                                    } else
                                        points[nmbPrims][nmbPoints] = calcBezier2Point(p, t);
                                    points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                        
                                    
                                    if (nmbPoints == points[nmbPrims].Length)
                                        Array.Resize(ref points[nmbPrims], nmbPoints * 2);                                    
                                }

                                curr = p[p.Length - 1].Clone();
                                prev = p[p.Length - 2].Clone();
                            }
                            
                            if ("cC".IndexOf(cmd) >= 0) {
                                prevCubic = true;
                                prevQuadr = false;
                            } else if ("qQ".IndexOf(cmd) >= 0) {
                                prevCubic = false;
                                prevQuadr = true;
                            }
                            
                        break;
                        
                        case "s":
                        case "S":
                        case "t":
                        case "T":
                            pathPoints = new NestPoint[args.Length / 2];
                            for (int j = 0; j < args.Length; j += 2) 
                                pathPoints[j/2] = new NestPoint(args[j], args[j + 1]);

                            degree = ("sS".IndexOf(cmd) >= 0) ? 2 : 1;
                            if ("st".IndexOf(cmd) >= 0)
                                RelToAbsBezier(pathPoints, curr, degree);
                            
                            if (("sScC".IndexOf(cmd) >= 0 && !prevCubic) || ("tTqQ".IndexOf(cmd) >= 0 && !prevQuadr))
                                prev = curr.Clone();

                            for (int j = 0; j < pathPoints.Length; j += degree)
                            {
                                NestPoint[] p;
                                NestPoint cp = curr + curr - prev;
                                if (degree == 2) 
                                    p = new NestPoint[]{ curr.Clone(), cp.Clone(), pathPoints[j].Clone(), pathPoints[j + 1].Clone() };
                                else
                                    p = new NestPoint[]{ curr.Clone(), cp.Clone(), pathPoints[j].Clone() };

                                for (double t = 0.0; t <= 1.0; t += tstep) 
                                {
                                    if (degree == 2) {
                                        points[nmbPrims][nmbPoints] = calcBezier3Point(p, t);
                                    } else
                                        points[nmbPrims][nmbPoints] = calcBezier2Point(p, t);
                                    
                                    if (nmbPoints == points[nmbPrims].Length)
                                        Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                                    
                                    points[nmbPrims][nmbPoints++].ApplyTransform(matrix);

                                    curr = p[p.Length - 1].Clone();
                                    prev = p[p.Length - 2].Clone();
                                }
                            }

                            if ("sS".IndexOf(cmd) >= 0) {
                                prevCubic = true;
                                prevQuadr = false;
                            } else if ("tT".IndexOf(cmd) >= 0) {
                                prevCubic = false;
                                prevQuadr = true;
                            }

                        break;

                        case "a":
                        case "A":
                            pathPoints = new NestPoint[args.Length / 7];
                            double[] rxArr = new double[args.Length / 7];
                            double[] ryArr = new double[args.Length / 7];
                            double[] xAxisRotation = new double[args.Length / 7];
                            double[] largeArcFlag = new double[args.Length / 7];
                            double[] sweepFlag = new double[args.Length / 7];
                            for (int j = 0; j < args.Length; j += 7) 
                            {
                                rxArr[j/7] = Math.Abs(args[j]);
                                ryArr[j/7] = Math.Abs(args[j + 1]);
                                xAxisRotation[j/7] = args[j + 2];
                                largeArcFlag[j/7] = args[j + 3];
                                sweepFlag[j/7] = args[j + 4];
                                pathPoints[j/7] = new NestPoint(args[j + 5], args[j + 6]);
                            }

                            if (cmd == "a")
                                RelToAbsArc(pathPoints, curr);

                            
                            for (int j = 0; j < pathPoints.Length; j++) 
                            {
                                double radphi = Math.PI * xAxisRotation[j] / 180.0;
                                double rx = rxArr[j];
                                double ry = ryArr[j];
                                double x1 = curr.X;
                                double y1 = curr.Y;
                                double x2 = pathPoints[j].X;
                                double y2 = pathPoints[j].Y;

                                double newx = Math.Cos(radphi)*((x1-x2)/2) + Math.Sin(radphi)*((y1-y2)/2);
	                            double newy = (-1) * Math.Sin(radphi)*((x1-x2)/2) + Math.Cos(radphi)*((y1-y2)/2);
                                double lambda = (newx*newx)/(rx*rx) + (newy*newy)/(ry*ry);

                                if (lambda > 1) {
                                    rx = Math.Sqrt(lambda)*rx;
                                    ry = Math.Sqrt(lambda)*ry;
                                }

                                double s = Math.Sqrt(Math.Abs((rx*rx * ry*ry - rx*rx * newy*newy - ry*ry * newx*newx) 
                                    / (rx*rx * newy*newy + ry*ry * newx*newx)));

                                if (largeArcFlag[j] == sweepFlag[j])
                                    s = -s;
                                
                                double tcx = s * (rx*newy)/ry;
	                            double tcy = s * ((-1)*ry*newx)/rx;

                                double cx = Math.Cos(radphi)*tcx - Math.Sin(radphi)*tcy + (x1+x2)/2;
                                double cy = Math.Sin(radphi)*tcx + Math.Cos(radphi)*tcy + (y1+y2)/2;

                                double vx = (newx - tcx) / rx;
                                double vy = (newy - tcy) / ry;
                                double ux = 1;
                                double uy = 0;
                                
                                double theta = (ux*vx + uy*vy) / (Math.Sqrt(ux*ux + uy*uy) * Math.Sqrt(vx*vx + vy*vy));
	                            theta = Math.Acos(theta) * 180 / Math.PI;
	                            theta = Math.Abs(theta) * svgSignum(ux*vy - vx*uy);

                                ux = (newx - tcx) / rx;
                                uy = (newy - tcy) / ry;
                                vx = (-newx - tcx)/rx;
                                vy = (-newy - tcy)/ry;
                                
                                double dtheta = (ux*vx + uy*vy) / (Math.Sqrt(ux*ux + uy*uy) * Math.Sqrt(vx*vx + vy*vy));
                                dtheta = (Math.Acos(dtheta) * 180 / Math.PI) % 360;
                                dtheta = Math.Abs(dtheta) * svgSignum(ux*vy - vx*uy);

                                if (sweepFlag[j] == 0 && dtheta > 0)
                                    dtheta -= 360;
                                else if (sweepFlag[j] == 1 && dtheta < 0)
                                    dtheta += 360;

                                double arcStep = (sweepFlag[j] == 0) ? -tstep : tstep;

                                for (double ang = theta; Math.Abs(ang - (theta + dtheta)) > 0.01; ang += arcStep) 
                                {
                                    newx = Math.Cos(radphi)*Math.Cos(ang*Math.PI/180)*rx - Math.Sin(radphi)*Math.Sin((ang*Math.PI)/180)*ry + cx;
                                    newy = Math.Sin(radphi)*Math.Cos(ang*Math.PI/180)*rx + Math.Cos(radphi)*Math.Sin((ang*Math.PI)/180)*ry + cy;

                                    points[nmbPrims][nmbPoints] = new NestPoint(newx, newy);
                                    points[nmbPrims][nmbPoints++].ApplyTransform(matrix);
                                    if (nmbPoints == points[nmbPrims].Length)
                                        Array.Resize(ref points[nmbPrims], nmbPoints * 2);
                                }

                                curr = pathPoints[j].Clone();
                            }

                            prevCubic = prevQuadr = false;    
                        break;
                    }

                    
                }

                Array.Resize(ref points[nmbPrims], nmbPoints);
                if (nmbPoints > 0)
                    nmbPrims++;
                nmbPoints = 0;
                if (nmbPrims == points.Length)
                    Array.Resize(ref points, nmbPrims * 2);
            }   
        }

        private void RectToPoints(XmlNode node, string transform)
        {
            double rx = 0.0;
            if (node.Attributes["rx"] != null) 
                rx = Convert.ToDouble(node.Attributes["rx"].Value.Replace(".", ","));

            double ry = 0.0;
            if (node.Attributes["ry"] != null) 
                ry = Convert.ToDouble(node.Attributes["ry"].Value.Replace(".", ","));
            
            double x = Convert.ToDouble(node.Attributes["x"].Value.Replace(".", ","));
            double y = Convert.ToDouble(node.Attributes["y"].Value.Replace(".", ","));
            double width = Convert.ToDouble(node.Attributes["width"].Value.Replace(".", ","));
            double height = Convert.ToDouble(node.Attributes["height"].Value.Replace(".", ","));

            string d = "M " + Convert.ToString(x + rx).Replace(",", ".") + "," + Convert.ToString(y).Replace(",", ".");

            d += " L " + Convert.ToString(x + width - rx).Replace(",", ".") + "," + Convert.ToString(y).Replace(",", ".");

            d += " A " + Convert.ToString(rx).Replace(",", ".") + "," + Convert.ToString(ry).Replace(",", ".") + ",0,0,1," +
            Convert.ToString(x+width).Replace(",", ".") + "," + Convert.ToString(y+ry).Replace(",", ".");

            d += " L " + Convert.ToString(x + width).Replace(",", ".") + "," + Convert.ToString(y + height - ry).Replace(",", ".");

            d += " A " + Convert.ToString(rx).Replace(",", ".") + "," + Convert.ToString(ry).Replace(",", ".") + ",0,0,1," +
            Convert.ToString(x+width-rx).Replace(",", ".") + "," + Convert.ToString(y+height).Replace(",", ".");

            d += " L " + Convert.ToString(x + rx).Replace(",", ".") + "," + Convert.ToString(y + height).Replace(",", ".");

            d += " A " + Convert.ToString(rx).Replace(",", ".") + "," + Convert.ToString(ry).Replace(",", ".") + ",0,0,1," +
            Convert.ToString(x).Replace(",", ".") + "," + Convert.ToString(y+height-ry).Replace(",", ".");

            d += " L " + Convert.ToString(x).Replace(",", ".") + "," + Convert.ToString(y + ry).Replace(",", ".");

            d += " A " + Convert.ToString(rx).Replace(",", ".") + "," + Convert.ToString(ry).Replace(",", ".") + ",0,0,1," +
            Convert.ToString(x+rx).Replace(",", ".") + "," + Convert.ToString(y).Replace(",", ".");
            
            PathToPoints(d, transform);
        }


        private void ApproxIfFigure(XmlNode node, string transform)
        {
            nmbPoints = 0;
            points[nmbPrims] = new NestPoint[initPoints];
            isFigure = false;
            switch (node.Name)
            {
                case "rect":
                    RectToPoints(node, transform);
                    break;
                case "circle":
                    CircleToPoints(node, transform);
                    break;
                case "ellipse":
                    EllipseToPoints(node, transform);
                    break;
                case "line":
                    LineToPoints(node, transform);
                    break;
                case "polyline":
                    PolylineToPoints(node, transform);
                    break;
                case "polygon":
                    PolygonToPoints(node, transform);
                    break;
                case "path":
                    PathToPoints(node.Attributes["d"].Value, transform);
                    break;
            }

            if (isFigure)
            {
                Array.Resize(ref points[nmbPrims], nmbPoints);
                nmbPrims++;
                if (nmbPrims == points.Length)
                    Array.Resize(ref points, nmbPrims * 2);
            }

        }

        private void ParseNode(XmlNode node, string transform)
        {
            if (node.Attributes != null)
                for (int i = 0; i < node.Attributes.Count; i++)
                    if (node.Attributes[i].Name == "transform")
                        transform += " " + ParseTransformAttr(node.Attributes[i].Value);

            ApproxIfFigure(node, transform);

            for (int i = 0; i < node.ChildNodes.Count; i++)
                ParseNode(node.ChildNodes[i], transform);
        }
    }
}
