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
        private static List<Point3d> DrawAnchor1(Line baseLine, List<Line> baseAxis, double vFactor)
        {
            //output
            List<Point3d> anchors = new List<Point3d>();

            //base setting
            double minRoomWidth = Corridor.MinRoomWidth; //최소 방 너비
            double corridorwidth = Corridor.TwoWayWidth; 
            double baseHalfVSize = baseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
            double baseHalfHSize = baseLine.Length / 2;

            //set vertical limit
            List<double> limitCandidates = new List<double>(); 

            limitCandidates.Add(-baseHalfVSize+ corridorwidth / 2);
            limitCandidates.Add(baseHalfVSize - corridorwidth / 2);
            double minChamberLimit = (minRoomWidth + corridorwidth / 2) - baseAxis[1].Length;
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
            Point3d Anchor1first = baseAxis[0].PointAt(0) + hAxis * (baseHalfHSize+ corridorwidth / 2) - vAxis * (limitLower+vLimit * vFactor); //횡장축부터, from horizontal-longerAxis
            Point3d Anchor1second = baseAxis[0].PointAt(0) - hAxis * (baseHalfHSize + corridorwidth / 2) - vAxis * (limitLower + vLimit * vFactor);

            anchors.Add(Anchor1first);
            anchors.Add(Anchor1second);

            return anchors;
        }

        private static List<Point3d> DrawAnchor2(List<Point3d> anchor1, Polyline outline, List<Line> mainAxis, List<double> hFactors)
        {
            List<Point3d> anchor2 = new List<Point3d>();

            for(int i=0; i<anchor1.Count;i++)
            {
                Vector3d tempAxis = mainAxis[0].UnitTangent*Math.Pow(-1,i);
                double hLimit = PCXTools.ExtendFromPt(anchor1[i], outline, tempAxis).Length- Corridor.TwoWayWidth/2;
                anchor2.Add(anchor1[i] +tempAxis*hLimit*hFactors[i]);
            }

            return anchor2;
        }

        private static List<Polyline> DrawCorridor(List<Point3d> anchor1, List<Point3d> anchor2)
        {
            List<Polyline> corridors = new List<Polyline>();

            for(int i=0; i<anchor1.Count;i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchor1[i], anchor2[i], Corridor.TwoWayWidth);
                corridors.Add(tempRect.ToPolyline());
            }

            return corridors;
        }
    }
}
