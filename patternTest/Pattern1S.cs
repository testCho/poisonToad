using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern1S
    {
        public Line SearchBaseLine(Core core)
        {
            //output
            Line baseSeg = new Line();

            //process
            List<Line> landingSeg = core.Landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, core.UpstairDirec));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort(delegate (Line x, Line y)
            {
                Point3d perp1Center = x.PointAt(0.5);
                Point3d perp2Center = y.PointAt(0.5);

                Vector3d gapBetween = perp1Center - perp2Center;
                double decider = Vector3d.Multiply(gapBetween, core.UpstairDirec);

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

        public double SetBasePtVLimit(Core core, Line baseLine)
        {
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;

            double candidate1 = PCXTools.ExtendFromPt(decidingPt1, core.Landing, -core.UpstairDirec).Length + 0.01;
            double candidate2 = PCXTools.ExtendFromPt(decidingPt2, core.Landing, -core.UpstairDirec).Length + 0.01;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            return vLimit;
        }

        public List<Line> SetBaseAxis(Polyline outline, Core core, Line baseLine)
        {
            //output
            List<Line> mainAxis = new List<Line>();

            //process
            double basePtY = SetBasePtVLimit(core, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * SetBasePtVLimit(core, baseLine) / 2;

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.ExtendFromPt(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.ExtendFromPt(basePt, outline, -baseLine.UnitTangent);

            if (horizonReached1.Length > horizonReached2.Length)
                mainAxis.Add(horizonReached1);
            else
                mainAxis.Add(horizonReached2);

            //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택
            Line verticalReached1 = PCXTools.ExtendFromPt(basePt, outline, core.UpstairDirec);
            Line verticalReached2 = PCXTools.ExtendFromPt(basePt, outline, -core.UpstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
                mainAxis.Add(verticalReached1);
            else
                mainAxis.Add(verticalReached2);

            return mainAxis;

        }

        public Point3d DrawAnchor1(Line baseLine, List<Line>mainAxis) // baseBox 중심에 맞춰져 있음.. 끝으로 바꿔야함
        {
            Point3d anchor = new Point3d();

            Point3d basePt = mainAxis[0].PointAt(0);
            Point3d anchorCenter = basePt + mainAxis[0].UnitTangent * (baseLine.Length/2.0 +Corridor.OneWayWidth/2);
            anchor = anchorCenter; //임시

            return anchor;
        }

        public Point3d DrawAnchor2(Point3d anchor1, Core core, Polyline outline, List<Line>mainAxis, double lengthFactor)
        {
            Point3d anchor2 = new Point3d();

            Point3d anchor2Center = new Point3d();
            double anchorLineLength = PCXTools.ExtendFromPt(mainAxis[1].PointAt(0), core.CoreLine, mainAxis[1].UnitTangent).Length;

            if (anchorLineLength < mainAxis[1].Length)
                anchor2Center = anchor1 + mainAxis[1].UnitTangent * (anchorLineLength - Corridor.OneWayWidth / 2) *lengthFactor;
            else
                anchor2Center = anchor1 + mainAxis[1].UnitTangent*(mainAxis[1].Length- Corridor.OneWayWidth / 2) *lengthFactor;

            anchor2 = anchor2Center; //임시

            return anchor2;
        }

        public Point3d DrawAnchor3(Point3d anchor2, Polyline outline, List<Line>mainAxis, double lengthFactor)
        {
            Point3d anchor3 = new Point3d();

            Point3d anchor3Center = new Point3d();
            double anchorLineLength = (PCXTools.ExtendFromPt(anchor2, outline, mainAxis[0].UnitTangent).Length- Corridor.OneWayWidth / 2) *lengthFactor;
            anchor3Center = anchor2 + mainAxis[0].UnitTangent * anchorLineLength;

            anchor3 = anchor3Center; //임시
            return anchor3;
        }

        public Polyline DrawCorridor(List<Point3d> anchors, List<Line> mainAxis)
        {
            Polyline corridor = new Polyline();

            if (anchors.Count < 2)
                return null;

            List<Rectangle3d> rectList = new List<Rectangle3d>();

            for(int i =0; i<anchors.Count-1;i++)
            {
                Rectangle3d tempRect = RectangleTools.DrawP2PRect(anchors[i], anchors[i + 1], Corridor.OneWayWidth);
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

                corridor = CurveTools.ToPolyline(intersected);
            }
       
            return corridor;
        }

        public Polyline DrawFirstDivider(Point3d anchor1, Point3d hLimitPt, Polyline outline, List<Line> mainAxis, double hFactor, double vFactor)
        {
            Polyline firstDivider = new Polyline();

            Plane basePln = new Plane(anchor1, mainAxis[0].UnitTangent, -mainAxis[1].UnitTangent);
            Point3d anchor1Copy = new Point3d(anchor1) + basePln.XAxis * Corridor.OneWayWidth / 2 + basePln.YAxis * Corridor.OneWayWidth / 2;
            Point3d limitPtCopy = new Point3d(hLimitPt);
            Polyline outlineCopy = new Polyline(outline);

            //transform coordinate
            outlineCopy.Transform(Transform.ChangeBasis(Plane.WorldXY, basePln));
            anchor1Copy.Transform(Transform.ChangeBasis(Plane.WorldXY, basePln));
            limitPtCopy.Transform(Transform.ChangeBasis(Plane.WorldXY, basePln));

            //set horizontal limit
            double hLimit = new double();
            hLimit = limitPtCopy.X - anchor1Copy.X;

            //set negative vertical limit //추가?
            double vLimitBelow = new double();
            vLimitBelow = anchor1Copy.Y - hLimitPt.Y;

            //set positive vertical limit
            double vLimit = new double();

            Point3d newAnchor1 = anchor1Copy + Vector3d.XAxis * hLimit * hFactor;

            Line limitDecider1 = PCXTools.ExtendFromPt(anchor1Copy, outlineCopy, Vector3d.YAxis);
            Line limitDecider2 = PCXTools.ExtendFromPt(newAnchor1, outlineCopy, Vector3d.YAxis);

            List<Point3d> vLimitCandidate = new List<Point3d>();
            List<Point3d> vLimitRunner = PolylineTools.GetVertex(outlineCopy);

            vLimitCandidate.Add(limitDecider1.PointAt(1));
            vLimitCandidate.Add(limitDecider2.PointAt(1));
            vLimitCandidate.Sort((x, y) => x.X.CompareTo(y.X));

            foreach (Point3d i in vLimitRunner)
            {
                if ((i.X > vLimitCandidate[0].X) && (i.X < vLimitCandidate[1].X) && (i.Y > anchor1Copy.Y))
                    vLimitCandidate.Add(i);
            }

            vLimitCandidate.Sort((x, y) => x.Y.CompareTo(y.Y));
            vLimit = vLimitCandidate[0].Y - anchor1Copy.Y;

            //draw new anchor and dividing line
            List<Point3d> dividingVertex = new List<Point3d>();
            anchor1Copy.Transform(Transform.ChangeBasis(basePln, Plane.WorldXY));
            limitDecider2.Transform(Transform.ChangeBasis(basePln, Plane.WorldXY));

            Point3d firstAnchor1 = anchor1Copy + basePln.YAxis * vLimit * vFactor;
            Point3d firstAnchor2 = anchor1Copy + basePln.XAxis * hLimit * hFactor + basePln.YAxis * vLimit * vFactor;

            dividingVertex.Add(anchor1Copy);
            dividingVertex.Add(firstAnchor1);
            dividingVertex.Add(firstAnchor2);
            dividingVertex.Add(limitDecider2.PointAt(1));

            firstDivider = new Polyline(dividingVertex);

            return firstDivider;
        }

        public Polyline DrawLastDivider(Point3d anchor3, Polyline outline, List<Line> mainAxis)
        {
            Polyline lastDivider = new Polyline();

            Plane basePln = new Plane(anchor3, mainAxis[0].UnitTangent, -mainAxis[1].UnitTangent);
            Point3d anchor3Copy = new Point3d(anchor3) + basePln.XAxis * Corridor.OneWayWidth / 2 + basePln.YAxis * Corridor.OneWayWidth / 2;

            //transform coordinate

            //check horizontal limit

            //set vertical limit

            //draw new anchor and dividing line
            Line lastLine = PCXTools.ExtendFromPt(anchor3Copy, outline, basePln.YAxis);

            List<Point3d> dividingVertex = new List<Point3d>();
            dividingVertex.Add(anchor3Copy);
            dividingVertex.Add(lastLine.PointAt(1));

            lastDivider = new Polyline(dividingVertex);

            return lastDivider;
 
        }

        public Polyline DrawMidDivider(List<Point3d> dividerLimit, Polyline outline, List<Line> mainAxis, double hFactor)
        {
            Polyline midDivider = new Polyline();
            return midDivider;
        }       

    }
}
