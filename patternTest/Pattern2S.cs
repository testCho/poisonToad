using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern2S
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

        public double SetBasePtVLimit(Polyline landing, Vector3d upstairDirec, Line baseLine)
        {
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - upstairDirec / upstairDirec.Length * 0.01;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - upstairDirec / upstairDirec.Length * 0.01;

            double candidate1 = PCXTools.ExtendFromPt(decidingPt1, landing, -upstairDirec).Length + 0.01;
            double candidate2 = PCXTools.ExtendFromPt(decidingPt2, landing, -upstairDirec).Length + 0.01;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            return vLimit;
        }

        public List<Line> SetBaseAxis(Polyline landing, Polyline outline, Vector3d upstairDirec, Line baseLine)
        {
            //output
            List<Line> mainAxis = new List<Line>();

            //process
            double basePtH = SetBasePtVLimit(landing, upstairDirec, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (upstairDirec / upstairDirec.Length) * SetBasePtVLimit(landing, upstairDirec, baseLine) / 2;

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

        public Point3d DrawAnchor1(Vector3d upstairDirec, Line baseLine, List<Line> baseAxis, double corridorWidth, double scale)
        {
            Point3d anchor = new Point3d();

            Point3d basePt = baseAxis[0].PointAt(0);
            Point3d anchorCenter = basePt + baseAxis[0].UnitTangent * (baseLine.Length / 2.0 + corridorWidth /(2* scale));
            anchor = anchorCenter; //임시

            return anchor;
        }

        public Point3d DrawAnchor2(Point3d anchor1, Polyline coreLine, List<Line> baseAxis, double corridorWidth, double scale)
        {
            Point3d anchor2 = new Point3d();

            double coreSideLimit = PCXTools.ExtendFromPt(baseAxis[0].PointAt(0), coreLine, -baseAxis[1].UnitTangent).Length;
            double scaledCorridorWidth = corridorWidth / scale;
            double vLimit = coreSideLimit + scaledCorridorWidth / 2;

            anchor2 = anchor1 - baseAxis[1].UnitTangent * vLimit;

            return anchor2;
        }

        public Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line>baseAxis, Line baseLine,double hFactor, double corridorWidth, double scale)
        {
            Point3d anchor3 = new Point3d();

            double outlineSideLimit = PCXTools.ExtendFromPt(anchor2, outline, -baseAxis[0].UnitTangent).Length - corridorWidth/(scale*2);
            double hLimit = baseLine.Length;

            if (outlineSideLimit < hLimit)
                hLimit = outlineSideLimit;

            anchor3 = anchor2 - baseAxis[0].UnitTangent * hLimit*hFactor;

            return anchor3;
        }

        public Point3d DrawAnchor4(Point3d anchor3,Polyline outline, List<Line>baseAxis, double vFactor, double corridorWidth, double scale)
        {
            Point3d anchor4 = new Point3d();

            double vLimit = PCXTools.ExtendFromPt(anchor3, outline, -baseAxis[1].UnitTangent).Length - corridorWidth / (scale * 2);
            anchor4 = anchor3 - baseAxis[1].UnitTangent *vLimit * vFactor;

            return anchor4;
        }

        public Polyline DrawCorridor(List<Point3d> anchors, List<Line> mainAxis, double corridorWidth, double scale)
        {
            Polyline corridor = new Polyline();

            if (anchors.Count < 2)
                return null;

            List<Rectangle3d> rectList = new List<Rectangle3d>();

            for (int i = 0; i < anchors.Count - 1; i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchors[i], anchors[i + 1], corridorWidth / scale);
                rectList.Add(tempRect);
            }

            if (rectList.Count > 1)
            {
                Curve intersected = rectList[0].ToNurbsCurve();

                for (int i = 0; i < rectList.Count - 1; i++)
                {
                    List<Curve> unionCurves = new List<Curve>();
                    unionCurves.Add(intersected);
                    unionCurves.Add(rectList[i + 1].ToNurbsCurve());
                    intersected = Curve.CreateBooleanUnion(unionCurves)[0];
                }

                corridor = CurveTools.ToPolyline(intersected);
            }

            return corridor;
        }
    }
}
