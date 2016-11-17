using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern4S //많이 고쳐야함 --;
    {
        public Line SearchBaseLine(Core core)
        {
            //output
            Line baseSeg = new Line();

            //process
            List<Line> landingSeg = core.Landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, core.UpstairDirec));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort(delegate (Line x, Line y)
            {
                Point3d perp1Center = x.PointAt(0.5);
                Point3d perp2Center = y.PointAt(0.5);

                Vector3d gapBetween = perp1Center - perp2Center;
                double decider = Vector3d.Multiply(gapBetween, core.UpstairDirec);

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

        public double SetBasePtVLimit(Core core, Line baseLine)
        {
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;

            double candidate1 = PCXTools.ExtendFromPt(decidingPt1, core.Landing, -core.UpstairDirec).Length + 0.01;
            double candidate2 = PCXTools.ExtendFromPt(decidingPt2, core.Landing, -core.UpstairDirec).Length + 0.01;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            return vLimit;
        }

        public List<Line> SetBaseAxis(Polyline outline, Core core, Line baseLine)
        {
            //output
            List<Line> mainAxis = new List<Line>();

            //process
            double basePtY = SetBasePtVLimit(core, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * SetBasePtVLimit(core, baseLine) / 2;

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.ExtendFromPt(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.ExtendFromPt(basePt, outline, -baseLine.UnitTangent);

            if (horizonReached1.Length > horizonReached2.Length)
                mainAxis.Add(horizonReached1);
            else
                mainAxis.Add(horizonReached2);

            //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택
            Line verticalReached1 = PCXTools.ExtendFromPt(basePt, outline, core.UpstairDirec);
            Line verticalReached2 = PCXTools.ExtendFromPt(basePt, outline, -core.UpstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
                mainAxis.Add(verticalReached1);
            else
                mainAxis.Add(verticalReached2);

            return mainAxis;

        }

        public Point3d DrawAnchor1(Polyline outline, Core core, Line baseLine, List<Line> baseAxis, out double subLength)
        {
            Point3d anchor1 = new Point3d();

            Point3d anchorBase = baseAxis[0].PointAt(0);

            double coreSideLength = PCXTools.ExtendFromPt(anchorBase, core.CoreLine, -baseAxis[1].UnitTangent).Length;
            double landingSideLength = baseLine.PointAt(0.5).DistanceTo(anchorBase);
            double subCorridorWidth = coreSideLength + landingSideLength;
            double halfHLength = baseLine.Length / 2;

            Vector3d hAxis = baseAxis[0].UnitTangent;
            Vector3d vAxis = -baseAxis[1].UnitTangent;


            anchor1 = anchorBase + vAxis * (subCorridorWidth / 2 - landingSideLength) + hAxis * (halfHLength + Corridor.TwoWayWidth/2);

            subLength = subCorridorWidth;
            return anchor1;
        }

        public Point3d DrawAnchor2(Point3d anchor1, Polyline outline, Core core, Line baseLine, List<Line> baseAxis, double hFactor)
        {
            //output
            Point3d anchor2 = new Point3d();

            //base setting
            double baseHalfVSize = baseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
            double baseHalfHSize = baseLine.Length / 2;

            //set horizontal limit
            List<double> limitCandidates = new List<double>();

            limitCandidates.Add(0);
            limitCandidates.Add(baseAxis[0].Length- Corridor.TwoWayWidth-baseHalfHSize);

            double shortHAxisLength = PCXTools.ExtendFromPt(baseLine.PointAt(0.5),outline,-baseAxis[0].UnitTangent).Length;
            limitCandidates.Add(Corridor.MinRoomWidth - shortHAxisLength);
   
            limitCandidates.Sort((x, y) => -x.CompareTo(y));

            double limitUpper = limitCandidates[0];
            double limitLower = limitCandidates[1];

            double hLimit = limitUpper - limitLower;
            
            //set vertical limit

            //draw anchors
            Vector3d hAxis = baseAxis[0].UnitTangent;

            anchor2 = anchor1 + hAxis*(hLimit * hFactor+limitLower);
            return anchor2;
        }

        public Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line> baseAxis, double vFactor, double subLength)
        {
            Point3d anchor3 = new Point3d();
            Point3d anchorBase = baseAxis[0].PointAt(0);

            double vLimit = PCXTools.ExtendFromPt(anchor2, outline, -baseAxis[1].UnitTangent).Length- subLength/2;
            anchor2 = anchor2 - baseAxis[1].UnitTangent * (vLimit * vFactor);
            return anchor3;
        }

        public Polyline DrawCorridor(List<Point3d> anchors, List<Line> baseAxis, double subLength)
        {
            Polyline corridor = new Polyline();

            Rectangle3d subCorridor = RectangleTools.DrawP2PRect(anchors[0], anchors[1], subLength, Corridor.TwoWayWidth);
            Rectangle3d mainCorridor = RectangleTools.DrawP2PRect(anchors[1], anchors[2], Corridor.TwoWayWidth, subLength);

            List<Curve> forUnion = new List<Curve>();
            forUnion.Add(subCorridor.ToNurbsCurve());
            forUnion.Add(mainCorridor.ToNurbsCurve());

            corridor = CurveTools.ToPolyline(Curve.CreateBooleanUnion(forUnion)[0]);

            return corridor;
        }
    }
}
