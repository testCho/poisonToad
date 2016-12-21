using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Corr_OneWayVertical1: ICorridorPattern
    {
        //field
        private string name = "편복도 일방향 세로형";
        private List<double> lengthFactors = new List<double> { 0.5 };
        private List<string> factorName = new List<string> { "복도 세로길이" };


        //property        
        public string Name { get { return name; } private set { } }
        public List<string> ParamName { get { return factorName; } private set { } }
        public List<double> Param
        {
            get { return lengthFactors; }
            set { lengthFactors = value as List<double>; }
        }


        //main method
        public List<Polyline> Draw(List<Line> mainAxis, Core core, Polyline outline)
        {
            Point3d anchor1 = DrawAnchor1(core.BaseLine, mainAxis);
            Point3d anchor2 = DrawAnchor2(anchor1, core, mainAxis);
            Point3d anchor3 = DrawAnchor3(anchor2, outline, mainAxis, core.BaseLine);
            Point3d anchor4 = DrawAnchor4(anchor3, outline, mainAxis, Param[0]);

            List<Point3d> anchors = new List<Point3d>();
            anchors.Add(anchor1);
            anchors.Add(anchor2);
            anchors.Add(anchor3);
            anchors.Add(anchor4);

            return DrawCorridor(anchors, mainAxis);
        }


        // drawing method
        private static Point3d DrawAnchor1(Line baseLine, List<Line> baseAxis)
        {
            Point3d anchor = new Point3d();

            Point3d basePt = baseAxis[0].PointAt(0);
            Point3d anchorCenter = basePt + baseAxis[0].UnitTangent * (baseLine.Length / 2.0 + CorridorDimension.OneWayWidth / 2);
            anchor = anchorCenter; //임시

            return anchor;
        }

        private static Point3d DrawAnchor2(Point3d anchor1, Core core, List<Line> baseAxis)
        {
            Point3d anchor2 = new Point3d();

            double coreSideLimit = PCXTools.PCXByEquation(baseAxis[0].PointAt(0), core.CoreLine, -baseAxis[1].UnitTangent).Length;
            double scaledCorridorWidth = CorridorDimension.OneWayWidth / 2;
            double vLimit = coreSideLimit + scaledCorridorWidth;

            anchor2 = anchor1 - baseAxis[1].UnitTangent * vLimit;

            return anchor2;
        }

        private static Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line>baseAxis, Line baseLine)
        {
            Point3d anchor3 = new Point3d();

            double outlineSideLimit = PCXTools.PCXByEquation(anchor2, outline, -baseAxis[0].UnitTangent).Length - CorridorDimension.OneWayWidth / 2;
            double hLimit = baseLine.Length;

            if (outlineSideLimit < hLimit)
                hLimit = outlineSideLimit;

            anchor3 = anchor2 - baseAxis[0].UnitTangent * hLimit;

            return anchor3;
        }

        private static Point3d DrawAnchor4(Point3d anchor3,Polyline outline, List<Line>baseAxis, double vFactor)
        {
            Point3d anchor4 = new Point3d();

            double vLimit = PCXTools.PCXByEquation(anchor3, outline, -baseAxis[1].UnitTangent).Length - CorridorDimension.OneWayWidth / 2;
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
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchors[i], anchors[i + 1], CorridorDimension.OneWayWidth);
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
