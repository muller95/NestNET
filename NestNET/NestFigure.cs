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
        private NestPoint[][] points;
        private int initPrims = 16, initPoints = 1024;
        private int nmbPrims, nmbPoints;
        public NestFigure(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode root = doc.GetElementsByTagName("svg")[0];

            nmbPoints = nmbPrims = 0;
            points = new NestPoint[initPrims][];

            for (int i = 0; i < root.ChildNodes.Count; i++)
                ParseNode(root.ChildNodes[i], "1_0_0_0_1_0_0_0_1");
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

        private string ParseTransformAttr(string transformAttr)
        {
            string[] transforms = transformAttr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

        private void CircleToPoints(XmlNode node)
        {
            double cx, cy, r;
            double step;

            cx = Double.Parse(node.Attributes["cx"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            cy = Double.Parse(node.Attributes["cy"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            r = Double.Parse(node.Attributes["r"].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            step = 1.0 / (2 * Math.PI * r);
            for (double t = 0.0; t <= 2 * Math.PI; t += step)
            {
                double x = r * Math.Cos(t) + cx;
                double y = r * Math.Sin(t) + cy;
                points[nmbPrims][nmbPoints++] = new NestPoint(x, y);
                if (nmbPoints == points[nmbPrims].Length)
                    Array.Resize(ref points[nmbPrims], nmbPoints * 2);
            }

        }

        private void ApproxIfFigure(XmlNode node)
        {
            nmbPoints = 0;
            points[nmbPrims] = new NestPoint[initPoints];
            switch (node.Name)
            {
                case "rect":
                    break;
                case "circle":
                    CircleToPoints(node);
                    break;
                case "ellipse":
                    break;
                case "line":
                    break;
                case "polyline":
                    break;
                case "polygon":
                    break;
            }
            Array.Resize(ref points[nmbPrims++], nmbPoints);
            if (nmbPrims == points.Length)
                Array.Resize(ref points, nmbPrims * 2);
            
        }

        private void ParseNode(XmlNode node, string transform)
        {
            if (node.Attributes != null)
                for (int i = 0; i < node.Attributes.Count; i++)
                    if (node.Attributes[i].Name == "transform")
                        transform += ParseTransformAttr(node.Attributes[i].Value);

            ApproxIfFigure(node);

            for (int i = 0; i < node.ChildNodes.Count; i++)
                ParseNode(node.ChildNodes[i], transform);
        }
    }
}
