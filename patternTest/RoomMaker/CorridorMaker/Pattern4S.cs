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
        private static Point3d DrawAnchor1(Polyline outline, Core core, Line baseLine, List<Line> baseAxis, out double subLength)
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

        private static Point3d DrawAnchor2(Point3d anchor1, Polyline outline, Core core, Line baseLine, List<Line> baseAxis, double hFactor)
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

        private static Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line> baseAxis, double vFactor, double subLength)
        {
            Point3d anchor3 = new Point3d();
            Point3d anchorBase = baseAxis[0].PointAt(0);

            double vLimit = PCXTools.ExtendFromPt(anchor2, outline, -baseAxis[1].UnitTangent).Length- subLength/2;
            anchor2 = anchor2 - baseAxis[1].UnitTangent * (vLimit * vFactor);
            return anchor3;
        }

        private static Polyline DrawCorridor(List<Point3d> anchors, List<Line> baseAxis, double subLength)
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
