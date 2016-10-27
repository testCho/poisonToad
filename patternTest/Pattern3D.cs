using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern3D
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
            double basePtY = SetBasePtVLimit(landing, upstairDirec, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (upstairDirec / upstairDirec.Length) * SetBasePtVLimit(landing, upstairDirec, baseLine)/2;

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

        public List<Point3d> DrawAnchor1(Vector3d upstairDirec, Line baseLine, List<Line> baseAxis, double vFactor, double corridorWidth, double scale)
        {
            //output
            List<Point3d> anchors = new List<Point3d>();

            //base setting
            double minChamberWidth = 3000/scale; //최소 방 너비
            double scaledCorridorwidth = corridorWidth / scale; 
            double baseHalfVSize = baseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
            double baseHalfHSize = baseLine.Length / 2;

            //set vertical limit
            List<double> limitCandidates = new List<double>(); 

            limitCandidates.Add(-baseHalfVSize+ scaledCorridorwidth / 2);
            limitCandidates.Add(baseHalfVSize - scaledCorridorwidth / 2);
            double minChamberLimit = (minChamberWidth + scaledCorridorwidth / 2) - baseAxis[1].Length;
            limitCandidates.Add(minChamberLimit);

            limitCandidates.Sort((x,y)=>-x.CompareTo(y));

            double limitUpper = limitCandidates[0];
            double limitLower = limitCandidates[1];

            if (limitLower < minChamberLimit)
                return null;

            double vLimit = limitUpper - limitLower;

            //draw anchors, 일단 둘 다..
            Vector3d hAxis = baseAxis[0].UnitTangent;
            Vector3d vAxis = baseAxis[1].UnitTangent;
            Point3d Anchor1first = baseAxis[0].PointAt(0) + hAxis * (baseHalfHSize+ scaledCorridorwidth / 2) - vAxis * (limitLower+vLimit * vFactor); //횡장축부터, from horizontal-longerAxis
            Point3d Anchor1second = baseAxis[0].PointAt(0) - hAxis * (baseHalfHSize + scaledCorridorwidth / 2) - vAxis * (limitLower + vLimit * vFactor);

            anchors.Add(Anchor1first);
            anchors.Add(Anchor1second);

            return anchors;
        }

        public List<Point3d> DrawAnchor2(List<Point3d> anchor1, Polyline outline, List<Line> mainAxis, List<double> hFactors, double corridorWidth, double scale)
        {
            List<Point3d> anchor2 = new List<Point3d>();

            for(int i=0; i<anchor1.Count;i++)
            {
                Vector3d tempAxis = mainAxis[0].UnitTangent*Math.Pow(-1,i);
                double hLimit = PCXTools.ExtendFromPt(anchor1[i], outline, tempAxis).Length-corridorWidth/(2*scale);
                anchor2.Add(anchor1[i] +tempAxis*hLimit*hFactors[i]);
            }

            return anchor2;
        }

        public List<Polyline> DrawCorridor(List<Point3d> anchor1, List<Point3d> anchor2, double corridorWidth, double scale)
        {
            List<Polyline> corridors = new List<Polyline>();

            for(int i=0; i<anchor1.Count;i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchor1[i], anchor2[i], corridorWidth/scale);
                corridors.Add(tempRect.ToPolyline());
            }

            return corridors;
        }
    }
}
