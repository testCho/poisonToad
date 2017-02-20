using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;

namespace SmallHousing.CorridorPatterns
{
    partial class CorridorP1
    {
        class CorridorP1S3 : ICorridorP1Sub
        {
            //field
            private string name = "즁복도 양방향 가로형";
            private List<double> lengthFactors = new List<double> { 1, 0.3, 0.3 };
            private List<string> factorName = new List<string> { "기준점 세로 위치", "복도 길이1", "복도 길이2" };

            //property
            public string Name { get { return name; } private set { } }
            public List<double> Param
            {
                get { return lengthFactors; }
                set { lengthFactors = value as List<double>; }
            }
            public Floor ParentFloor { get; set; }
            public List<List<Line>> AxisSet { get; set; }
            public P1Core Core { get; set; }
            private Polyline Outline { get { return ParentFloor.Outline; } }


            //main
            public List<Polyline> Build()
            {
                List<double> tempParam = new List<double>(Param);

                List<Point3d> anchor1s = DrawAnchor1(Core.BaseLine, AxisSet[0], tempParam[0]);

                if (anchor1s.Count == 0)
                    return new List<Polyline>();

                tempParam.RemoveAt(0);
                List<Point3d> anchor2s = DrawAnchor2(anchor1s, Outline, AxisSet[0], tempParam);

                return DrawCorridor(anchor1s, anchor2s);
            }


            // drawing method
            private static List<Point3d> DrawAnchor1(Line baseLine, List<Line> baseAxis, double vFactor)
            {
                //output
                List<Point3d> anchors = new List<Point3d>();

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
                    return new List<Point3d>();

                double vLimit = limitUpper - limitLower;

                //draw anchors, 일단 둘 다..
                Vector3d hAxis = baseAxis[0].UnitTangent;
                Vector3d vAxis = baseAxis[1].UnitTangent;
                Point3d Anchor1first = baseAxis[0].PointAt(0) + hAxis * (baseHalfHSize + corridorwidth / 2) - vAxis * (limitLower + vLimit * vFactor); //횡장축부터, from horizontal-longerAxis
                Point3d Anchor1second = baseAxis[0].PointAt(0) - hAxis * (baseHalfHSize + corridorwidth / 2) - vAxis * (limitLower + vLimit * vFactor);

                anchors.Add(Anchor1first);
                anchors.Add(Anchor1second);

                return anchors;
            }

            private static List<Point3d> DrawAnchor2(List<Point3d> anchor1, Polyline outline, List<Line> mainAxis, List<double> hFactors)
            {
                List<Point3d> anchor2 = new List<Point3d>();

                for (int i = 0; i < anchor1.Count; i++)
                {
                    Vector3d tempAxis = mainAxis[0].UnitTangent * Math.Pow(-1, i);
                    double hLimit = PCXTools.PCXByEquation(anchor1[i], outline, tempAxis).Length - CorridorDimension.TwoWayWidth / 2;
                    anchor2.Add(anchor1[i] + tempAxis * hLimit * hFactors[i]);
                }

                return anchor2;
            }

            private static List<Polyline> DrawCorridor(List<Point3d> anchor1, List<Point3d> anchor2)
            {
                List<Polyline> corridors = new List<Polyline>();

                for (int i = 0; i < anchor1.Count; i++)
                {
                    Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchor1[i], anchor2[i], CorridorDimension.TwoWayWidth);
                    corridors.Add(tempRect.ToPolyline());
                }

                return corridors;
            }
        }
    }
}
