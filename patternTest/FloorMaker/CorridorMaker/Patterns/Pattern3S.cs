using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Corr_TwoWayHorizontal1: ICorridorPattern
    {
        public List<Polyline> GetCorridor(Line baseLine, List<Line> mainAxis, Core core, Polyline outline, List<double> lengthFactors)
        {
            List<double> tempFactors = new List<double>();
            if (lengthFactors.Count == 0)
                tempFactors = GetInitialLengthFactors();
            else
                tempFactors = lengthFactors;

            Point3d anchor1 = DrawAnchor1(baseLine, mainAxis, tempFactors[0]);
            Point3d anchor2 = DrawAnchor2(anchor1, outline, mainAxis, tempFactors[1]);

            List<Point3d> anchors = new List<Point3d>();
            anchors.Add(anchor1);
            anchors.Add(anchor2);

            return DrawCorridor(anchor1, anchor2);
        }

        public List<double> GetInitialLengthFactors()
        {
            List<double> lengthFactors = new List<double>();
            lengthFactors.Add(0.5);
            lengthFactors.Add(0.7);

            return lengthFactors;
        }


        //
        private static Point3d DrawAnchor1(Line baseLine, List<Line> baseAxis, double vFactor)
        {
            //output
            Point3d anchor1 = new Point3d();

            //base setting
            double minRoomWidth = Corridor.MinRoomWidth; //최소 방 너비
            double corridorwidth = Corridor.TwoWayWidth;
            double baseHalfVSize = baseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
            double baseHalfHSize = baseLine.Length / 2;

            //set vertical limit
            List<double> limitCandidates = new List<double>();

            limitCandidates.Add(-baseHalfVSize + corridorwidth / 2);
            limitCandidates.Add(baseHalfVSize - corridorwidth / 2);
            double minChamberLimit = (minRoomWidth + corridorwidth / 2) - baseAxis[1].Length;
            limitCandidates.Add(minChamberLimit);

            limitCandidates.Sort((x, y) => -x.CompareTo(y));

            double limitUpper = limitCandidates[0];
            double limitLower = limitCandidates[1];

            if (limitLower < minChamberLimit)
                return Point3d.Unset;

            double vLimit = limitUpper - limitLower;

            //draw anchors
            Vector3d hAxis = baseAxis[0].UnitTangent;
            Vector3d vAxis = baseAxis[1].UnitTangent;
            anchor1 = baseAxis[0].PointAt(0) + hAxis * (baseHalfHSize + corridorwidth / 2) - vAxis * (limitLower + vLimit * vFactor); //횡장축부터, from horizontal-longerAxis

            return anchor1;
        }

        private static Point3d DrawAnchor2(Point3d anchor1, Polyline outline, List<Line> mainAxis, double hFactor)
        {
            Point3d anchor2 = new Point3d();


            Vector3d hAxis = mainAxis[0].UnitTangent;
            double hLimit = PCXTools.PCXByEquation(anchor1, outline, hAxis).Length - Corridor.TwoWayWidth / 2;
            anchor2 = anchor1 + hAxis * hLimit * hFactor;
            
            return anchor2;
        }

        private static List<Polyline> DrawCorridor(Point3d anchor1, Point3d anchor2)
        {
            List<Polyline> corridors = new List<Polyline>();
        
            Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchor1, anchor2, Corridor.TwoWayWidth);
            corridors.Add(tempRect.ToPolyline());

            return corridors;
        }
    }
}
