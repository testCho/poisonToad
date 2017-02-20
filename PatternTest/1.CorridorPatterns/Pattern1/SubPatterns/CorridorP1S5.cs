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
        class CorridorP1S5 : ICorridorP1Sub //그리는 부분 많이 고쳐야함 --;
        {
            //field
            private string name = "중복도 일방향 세로형";
            private List<double> lengthFactors = new List<double> { 0.5, 0.5 };
            private List<string> factorName = new List<string> { "복도 가로 길이", "복도 세로 길이" };

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
                double subLength = new double();
                Point3d anchor1 = DrawAnchor1(Outline, Core, AxisSet[0], out subLength);
                Point3d anchor2 = DrawAnchor2(anchor1, Outline, Core, AxisSet[0], lengthFactors[0]);
                Point3d anchor3 = DrawAnchor3(anchor2, Outline, AxisSet[0], Param[1], subLength);

                List<Point3d> anchors = new List<Point3d>();
                anchors.Add(anchor1);
                anchors.Add(anchor2);
                anchors.Add(anchor3);

                return DrawCorridor(anchors, AxisSet[0], subLength);
            }
           

         
            //drawing method
            private static Point3d DrawAnchor1(Polyline outline, P1Core core, List<Line> baseAxis, out double subLength)
            {
                Point3d anchor1 = new Point3d();

                Point3d anchorBase = baseAxis[0].PointAt(0);

                double coreSideLength = PCXTools.PCXByEquation(anchorBase, core.CoreLine, -baseAxis[1].UnitTangent).Length;
                double landingSideLength = core.BaseLine.PointAt(0.5).DistanceTo(anchorBase);
                double subCorridorWidth = coreSideLength + landingSideLength;
                double halfHLength = core.BaseLine.Length / 2;

                Vector3d hAxis = baseAxis[0].UnitTangent;
                Vector3d vAxis = -baseAxis[1].UnitTangent;


                anchor1 = anchorBase + vAxis * (subCorridorWidth / 2 - landingSideLength) + hAxis * (halfHLength + CorridorDimension.TwoWayWidth / 2);

                subLength = subCorridorWidth;
                return anchor1;
            }

            private static Point3d DrawAnchor2(Point3d anchor1, Polyline outline, P1Core core, List<Line> baseAxis, double hFactor)
            {
                //output
                Point3d anchor2 = new Point3d();

                //base setting
                double baseHalfVSize = core.BaseLine.PointAt(0.5).DistanceTo(baseAxis[0].PointAt(0));
                double baseHalfHSize = core.BaseLine.Length / 2;

                //set horizontal limit
                List<double> limitCandidates = new List<double>();

                limitCandidates.Add(0);
                limitCandidates.Add(baseAxis[0].Length - CorridorDimension.TwoWayWidth - baseHalfHSize);

                double shortHAxisLength = PCXTools.PCXByEquation(core.BaseLine.PointAt(0.5), outline, -baseAxis[0].UnitTangent).Length;
                limitCandidates.Add(CorridorDimension.MinRoomWidth - shortHAxisLength);

                limitCandidates.Sort((x, y) => -x.CompareTo(y));

                double limitUpper = limitCandidates[0];
                double limitLower = limitCandidates[1];

                double hLimit = limitUpper - limitLower;

                //set vertical limit

                //draw anchors
                Vector3d hAxis = baseAxis[0].UnitTangent;

                anchor2 = anchor1 + hAxis * (hLimit * hFactor + limitLower);
                return anchor2;
            }

            private static Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line> baseAxis, double vFactor, double subLength)
            {
                Point3d anchor3 = new Point3d();
                Point3d anchorBase = baseAxis[0].PointAt(0);

                double vLimit = PCXTools.PCXByEquation(anchor2, outline, -baseAxis[1].UnitTangent).Length - subLength / 2;
                anchor2 = anchor2 - baseAxis[1].UnitTangent * (vLimit * vFactor);
                return anchor3;
            }

            private static List<Polyline> DrawCorridor(List<Point3d> anchors, List<Line> baseAxis, double subLength)
            {
                List<Polyline> corridor = new List<Polyline>();

                Rectangle3d subCorridor = RectangleTools.DrawP2PRect(anchors[0], anchors[1], subLength, CorridorDimension.TwoWayWidth);
                Rectangle3d mainCorridor = RectangleTools.DrawP2PRect(anchors[1], anchors[2], CorridorDimension.TwoWayWidth, subLength);

                List<Curve> forUnion = new List<Curve>();
                forUnion.Add(subCorridor.ToNurbsCurve());
                forUnion.Add(mainCorridor.ToNurbsCurve());

                corridor.Add(CurveTools.ToPolyline(Curve.CreateBooleanUnion(forUnion)[0]));

                return corridor;
            }
        }
    }
}
