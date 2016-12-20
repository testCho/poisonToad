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
        //field
        private string name = "중복도 일방향 가로형";
        private List<double> lengthFactors = new List<double> { 0.5, 0.7 };
        private List<string> factorName = new List<string> { "기준점 세로 위치", "복도 길이"};


        //property
        public string Name { get { return name; } private set { } }
        public List<string> ParamName { get { return factorName; } private set { } }
        public List<double> Param
        {
            get { return lengthFactors; }
            set { lengthFactors = value as List<double>; }
        }


        //main method
        public List<Polyline> Draw(Line baseLine, List<Line> mainAxis, Core core, Polyline outline)
        {
            Point3d anchor1 = DrawAnchor1(baseLine, mainAxis, Param[0]);
            Point3d anchor2 = DrawAnchor2(anchor1, outline, mainAxis, Param[1]);

            List<Point3d> anchors = new List<Point3d>();
            anchors.Add(anchor1);
            anchors.Add(anchor2);

            return DrawCorridor(anchor1, anchor2);
        }


        //drawing method
        private static Point3d DrawAnchor1(Line baseLine, List<Line> baseAxis, double vFactor)
        {
            //output
            Point3d anchor1 = new Point3d();

            //base setting
            double minRoomWidth = CorridorDimension.MinRoomWidth; //최소 방 너비
            double corridorwidth = CorridorDimension.TwoWayWidth;
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
            double hLimit = PCXTools.PCXByEquation(anchor1, outline, hAxis).Length - CorridorDimension.TwoWayWidth / 2;
            anchor2 = anchor1 + hAxis * hLimit * hFactor;
            
            return anchor2;
        }

        private static List<Polyline> DrawCorridor(Point3d anchor1, Point3d anchor2)
        {
            List<Polyline> corridors = new List<Polyline>();
        
            Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchor1, anchor2, CorridorDimension.TwoWayWidth);
            corridors.Add(tempRect.ToPolyline());

            return corridors;
        }
    }
}
