using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Corr_OneWayHorizontal1: ICorridorPattern
    {
        //field
        private string name = "편복도 일방향 가로형";
        private List<double> lengthFactors = new List<double> { 0.7 };
        private List<string> factorName = new List<string> { "복도 가로길이" };


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
            Point3d anchor1 = DrawAnchor1(baseLine, mainAxis);
            Point3d anchor2 = DrawAnchor2(anchor1, core, outline, mainAxis);
            Point3d anchor3 = DrawAnchor3(anchor2, outline, mainAxis, lengthFactors[0]);

            List<Point3d> anchors = new List<Point3d>();
            anchors.Add(anchor1);
            anchors.Add(anchor2);
            anchors.Add(anchor3);

            return DrawCorridor(anchors, mainAxis);
        }


        //drawing method
        private static Point3d DrawAnchor1(Line baseLine, List<Line>mainAxis) // baseBox 중심에 맞춰져 있음.. 끝으로 바꿔야함
        {
            Point3d anchor = new Point3d();

            Point3d basePt = mainAxis[0].PointAt(0);
            Point3d anchorCenter = basePt + mainAxis[0].UnitTangent * (baseLine.Length/2.0 + CorridorDimension.OneWayWidth/2);
            anchor = anchorCenter; //임시

            return anchor;
        }

        private static Point3d DrawAnchor2(Point3d anchor1, Core core, Polyline outline, List<Line>mainAxis)
        {
            Point3d anchor2 = new Point3d();

            Point3d anchor2Center = new Point3d();
            double anchorLineLength = PCXTools.PCXByEquation(mainAxis[1].PointAt(0), core.CoreLine, mainAxis[1].UnitTangent).Length;

            if (anchorLineLength < mainAxis[1].Length)
                anchor2Center = anchor1 + mainAxis[1].UnitTangent * (anchorLineLength - CorridorDimension.OneWayWidth / 2);
            else
                anchor2Center = anchor1 + mainAxis[1].UnitTangent * (mainAxis[1].Length- CorridorDimension.OneWayWidth / 2);

            anchor2 = anchor2Center; //임시

            return anchor2;
        }

        private static Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line>mainAxis, double lengthFactor)
        {
            Point3d anchor3 = new Point3d();

            Point3d anchor3Center = new Point3d();
            double anchorLineLength = (PCXTools.PCXByEquation(anchor2, outline, mainAxis[0].UnitTangent).Length- CorridorDimension.OneWayWidth / 2) *lengthFactor;
            anchor3Center = anchor2 + mainAxis[0].UnitTangent * anchorLineLength;

            anchor3 = anchor3Center; //임시
            return anchor3;
        }

        private static List<Polyline> DrawCorridor(List<Point3d> anchors, List<Line> mainAxis)
        {
            List<Polyline> corridor = new List<Polyline>();

            if (anchors.Count < 2)
                return null;

            List<Rectangle3d> rectList = new List<Rectangle3d>();

            for(int i =0; i<anchors.Count-1;i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchors[i], anchors[i + 1], CorridorDimension.OneWayWidth);
                rectList.Add(tempRect);
            }

            if(rectList.Count>1)
            {
                Curve intersected = rectList[0].ToNurbsCurve();

                for(int i =0; i<rectList.Count-1;i++)
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
