using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern2S: ICorridorPattern
    {
        public List<Polyline> GetCorridor(Line baseLine, List<Line> mainAxis, Core core, Polyline outline, List<double> lengthFactors)
        {
            List<double> tempFactors = new List<double>();
            if (lengthFactors.Count == 0)
                tempFactors = GetInitialLengthFactors();
            else
                tempFactors = lengthFactors;

            Point3d anchor1 = DrawAnchor1(baseLine, mainAxis);
            Point3d anchor2 = DrawAnchor2(anchor1, core, mainAxis);
            Point3d anchor3 = DrawAnchor3(anchor2, outline, mainAxis, baseLine);
            Point3d anchor4 = DrawAnchor4(anchor3, outline, mainAxis, tempFactors[0]);

            List<Point3d> anchors = new List<Point3d>();
            anchors.Add(anchor1);
            anchors.Add(anchor2);
            anchors.Add(anchor3);
            anchors.Add(anchor4);

            return DrawCorridor(anchors, mainAxis);
        }

        public List<double> GetInitialLengthFactors()
        {
            List<double> lengthFactors = new List<double>();
            lengthFactors.Add(0.5);

            return lengthFactors;
        }

        //
        private static Point3d DrawAnchor1(Line baseLine, List<Line> baseAxis)
        {
            Point3d anchor = new Point3d();

            Point3d basePt = baseAxis[0].PointAt(0);
            Point3d anchorCenter = basePt + baseAxis[0].UnitTangent * (baseLine.Length / 2.0 + Corridor.OneWayWidth / 2);
            anchor = anchorCenter; //임시

            return anchor;
        }

        private static Point3d DrawAnchor2(Point3d anchor1, Core core, List<Line> baseAxis)
        {
            Point3d anchor2 = new Point3d();

            double coreSideLimit = PCXTools.ExtendFromPt(baseAxis[0].PointAt(0), core.CoreLine, -baseAxis[1].UnitTangent).Length;
            double scaledCorridorWidth = Corridor.OneWayWidth / 2;
            double vLimit = coreSideLimit + scaledCorridorWidth;

            anchor2 = anchor1 - baseAxis[1].UnitTangent * vLimit;

            return anchor2;
        }

        private static Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line>baseAxis, Line baseLine)
        {
            Point3d anchor3 = new Point3d();

            double outlineSideLimit = PCXTools.ExtendFromPt(anchor2, outline, -baseAxis[0].UnitTangent).Length - Corridor.OneWayWidth / 2;
            double hLimit = baseLine.Length;

            if (outlineSideLimit < hLimit)
                hLimit = outlineSideLimit;

            anchor3 = anchor2 - baseAxis[0].UnitTangent * hLimit;

            return anchor3;
        }

        private static Point3d DrawAnchor4(Point3d anchor3,Polyline outline, List<Line>baseAxis, double vFactor)
        {
            Point3d anchor4 = new Point3d();

            double vLimit = PCXTools.ExtendFromPt(anchor3, outline, -baseAxis[1].UnitTangent).Length - Corridor.OneWayWidth / 2;
            anchor4 = anchor3 - baseAxis[1].UnitTangent *vLimit * vFactor;

            return anchor4;
        }

        private static List<Polyline> DrawCorridor(List<Point3d> anchors, List<Line> mainAxis)
        {
            List<Polyline> corridor = new List<Polyline>();

            if (anchors.Count < 2)
                return null;

            List<Rectangle3d> rectList = new List<Rectangle3d>();

            for (int i = 0; i < anchors.Count - 1; i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchors[i], anchors[i + 1], Corridor.OneWayWidth);
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

                corridor.Add(CurveTools.ToPolyline(intersected));
            }

            return corridor;
        }
    }
}
