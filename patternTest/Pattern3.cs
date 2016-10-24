using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern3
    {
        public Line SearchBaseLine(Polyline landing, Vector3d upstairDirec)
        {
            //output
            Line baseSeg = new Line();

            //process
            List<Line> landingSeg = landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, upstairDirec));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort(delegate (Line x, Line y)
            {
                Point3d perp1Center = x.PointAt(0.5);
                Point3d perp2Center = y.PointAt(0.5);

                Vector3d gapBetween = perp1Center - perp2Center;
                double decider = Vector3d.Multiply(gapBetween, upstairDirec);

                if (decider > 0)
                    return -1;
                else if (decider == 0)
                    return 0;
                else
                    return 1;
            });

            baseSeg = perpToStair[0];

            return baseSeg;
        }

        public List<Line> SetMainAxis(Polyline landing, Polyline outline, Vector3d upstairDirec, Line baseLine, double scale)
        {
            //output
            List<Line> mainAxis = new List<Line>();

            //process
            Point3d basePt = baseLine.PointAt(0.5) - (upstairDirec / upstairDirec.Length) * (900 / scale);

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.ExtendFromPt(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.ExtendFromPt(basePt, outline, -baseLine.UnitTangent);

            if (horizonReached1.Length > horizonReached2.Length)
                mainAxis.Add(horizonReached1);
            else
                mainAxis.Add(horizonReached2);

            //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택
            Line verticalReached1 = PCXTools.ExtendFromPt(basePt, outline, upstairDirec);
            Line verticalReached2 = PCXTools.ExtendFromPt(basePt, outline, -upstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
                mainAxis.Add(verticalReached1);
            else
                mainAxis.Add(verticalReached2);

            return mainAxis;

        }

        public List<Point3d> DrawAnchor1(Vector3d upstairDirec, Line baseLine, List<Line> mainAxis, double vFactor, double scale)
        {
            List<Point3d> anchor = new List<Point3d>();

            double vLimitLower = 3900/scale - mainAxis[1].Length;
            double vLimitUpper = 900/scale;
            double vLimit = vLimitUpper - vLimitLower;

            if (vLimit<0)
                return null;

   
            Point3d basePt = mainAxis[0].PointAt(0);

            return anchor;
        }

        public Point3d DrawAnchor2(Point3d anchor1, Polyline coreLine, Polyline outline, List<Line> mainAxis, double scale, double lengthFactor)
        {
            Point3d anchor2 = new Point3d();

            Point3d anchor2Center = new Point3d();
            double anchorLineLength = PCXTools.ExtendFromPt(mainAxis[1].PointAt(0), coreLine, mainAxis[1].UnitTangent).Length;

            if (anchorLineLength < mainAxis[1].Length)
                anchor2Center = anchor1 + mainAxis[1].UnitTangent * (anchorLineLength - 600 / scale) * lengthFactor;
            else
                anchor2Center = anchor1 + mainAxis[1].UnitTangent * (mainAxis[1].Length - 600 / scale) * lengthFactor;

            anchor2 = anchor2Center; //임시

            return anchor2;
        }
    }
}
