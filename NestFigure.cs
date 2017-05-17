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
            Console.WriteLine("@TRANSFORM " + transform);
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
            double sin = Math.Sin(a);
            double cos = Math.Cos(a);
            string mtx2 = String.Format("{0}_{1}_0_{2}_{3}_0_0_0_1", cos, -1 * sin, sin, cos);
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
            double tan = Math.Tan(a);
            return String.Format("1_{0}_0_0_1_0_0_0_1", tan);
        }

        private String SkewYToString(string param)
        {
            string[] vals = param.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double a = Convert.ToDouble(vals[0]);
            double tan = Math.Tan(a);
            return String.Format("1_0_0_{0}_1_0_0_0_1", tan);
        }

        private string[] SplitTransforms(string transformAttr)
        {
            string[] transforms = transformAttr.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < transforms.Length; i++)
                transforms[i] += ")";
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
                matrix[i / 3, i % 3] = double.Parse(vals[i], NumberStyles.Any, CultureInfo.InvariantCulture);

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


        private void ApproxIfFigure(XmlNode node, string transform)
        {
            nmbPoints = 0;
            points[nmbPrims] = new NestPoint[initPoints];
            isFigure = false;
            switch (node.Name)
            {
                case "rect":
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
                    Console.WriteLine("@GOT PATH");
                    break;
            }

            if (isFigure)
            {
                Array.Resize(ref points[nmbPrims++], nmbPoints);
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
