using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern4S
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

        public Point3d DrawAnchor1(Polyline outline, Polyline coreLine, Vector3d upstairDirec, Line baseLine, List<Line> baseAxis, double corridorWidth, double scale)
        {
            Point3d anchor1 = new Point3d();

            Point3d anchorBase = baseAxis[0].PointAt(0);

            double coreSideLength = PCXTools.ExtendFromPt(anchorBase, coreLine, -baseAxis[1].UnitTangent).Length;
            double landingSideLength = baseLine.PointAt(0.5).DistanceTo(anchorBase);
            double subCorridorWidth = coreSideLength + landingSideLength;
            double scaledCorridorWidth = corridorWidth / scale;
            double halfHLength = baseLine.Length / 2;

            Vector3d hAxis = baseAxis[0].UnitTangent;
            Vector3d vAxis = -baseAxis[1].UnitTangent;


            anchor1 = anchorBase + vAxis * (subCorridorWidth / 2 - landingSideLength) + hAxis * (halfHLength + scaledCorridorWidth / 2);

            return anchor1;
        }

        public Point3d DrawAnchor2(Point3d anchor1, Polyline outline, Vector3d upstairDirec, Line baseLine, List<Line> baseAxis, double hFactor, double corridorWidth, double scale)
        {
            //output
            Point3d anchor = new Point3d();

            //base setting
            double minChamberWidth = 3000 / scale; //최소 방 너비 //scale하고 같이 따로 static으로 만들어 둘것 --;
            double scaledCorridorwidth = corridorWidth / scale;
            double baseHalfVSize = baseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
            double baseHalfHSize = baseLine.Length / 2;

            //set horizontal limit
            List<double> limitCandidates = new List<double>();

            limitCandidates.Add(baseHalfHSize+ scaledCorridorwidth / 2);
            limitCandidates.Add(baseAxis[0].Length- scaledCorridorwidth / 2);

            double shortHAxisLength = PCXTools.ExtendFromPt(baseLine.PointAt(0.5),outline,-baseAxis[0].UnitTangent).Length;
            double chamberLimit = (minChamberWidth-(shortHAxisLength+ baseHalfHSize))+baseHalfHSize+ scaledCorridorwidth / 2;
            limitCandidates.Add(chamberLimit);
   
            limitCandidates.Sort((x, y) => -x.CompareTo(y));

            double limitUpper = limitCandidates[0];
            double limitLower = limitCandidates[1];

            double hLimit = limitUpper - limitLower;
            
            //set vertical limit

            //draw anchors
            Vector3d hAxis = baseAxis[0].UnitTangent;

            anchor = baseAxis[0].PointAt(0) + hAxis*(hLimit * hFactor+limitLower);
            return anchor;
        }

        public Point3d DrawAnchor3(Polyline outline, List<Line> baseAxis, Point3d anchor1, double vFactor, double corridorWidth, double scale)
        {
            Point3d anchor2 = new Point3d();

            double vLimit = PCXTools.ExtendFromPt(anchor1, outline, -baseAxis[1].UnitTangent).Length-corridorWidth/(2*scale);
            anchor2 = anchor1 - baseAxis[1].UnitTangent * (vLimit * vFactor);
            return anchor2;
        }

        public Polyline DrawCorridor(List<Point3d> anchors, List<Line> baseAxis, double corridorWidth, double scale)
        {
            Polyline corridor = new Polyline();

            Rectangle3d mainCorridor = RectangleTools.DrawP2PRect(anchors[0], anchors[1], corridorWidth / scale);
            corridor = mainCorridor.ToPolyline();

            return corridor;
        }
    }
}
