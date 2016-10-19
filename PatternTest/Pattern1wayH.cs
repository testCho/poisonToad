using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern1wayH
    {
        public List<Line> SetCorridorAxis(Polyline landing, Vector3d upstairDirection)
        {
            List<Line> corridorAxis = new List<Line>();
            Line horizontalAxis = new Line();
            Line verticalAxis = new Line();
            double scale = 1;

            List<Line> landingSeg = landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, upstairDirection));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort((Line x, Line y) => -(x.Length.CompareTo(y.Length)));

            horizontalAxis = perpToStair[0];
            verticalAxis = new Line(horizontalAxis.PointAt(1), -upstairDirection / upstairDirection.Length, 1200/scale);

            corridorAxis.Add(horizontalAxis);
            corridorAxis.Add(verticalAxis);

            return corridorAxis;
        }

        public List<Polyline> DrawBaseAppendix(Polyline landing, List<Line> corridorAxis, Polyline outline)
        {
            List<Polyline> baseAppendix = new List<Polyline>();
            double scale = 1;

            Line horizonAxis = corridorAxis[0];
            Line verticalAxis = corridorAxis[1];

            Point3d horizonCenter = horizonAxis.PointAt(0.5);

            Vector3d corridorDir = new Vector3d();

            double coverAllLength = new BoundingBox(outline).Diagonal.Length*2;

            List<Line> candidateAxisSet = new List<Line>();
            candidateAxisSet.Add(new Line(horizonCenter, horizonAxis.UnitTangent * coverAllLength));
            candidateAxisSet.Add(new Line(horizonCenter, -horizonAxis.UnitTangent * coverAllLength));

            List<Line> reachedCandidate = new List<Line>();

            foreach (Line axisLay in candidateAxisSet)
            {
                List<Point3d> intersectedPt = new List<Point3d>();
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(axisLay.ToNurbsCurve(), outline.ToNurbsCurve(), 0, 0);

                foreach (var i in tempIntersection)
                    intersectedPt.Add(i.PointA);

                intersectedPt.Sort((x,y) => x.DistanceTo(horizonCenter).CompareTo(y.DistanceTo(horizonCenter)));
                Line reachedLay = new Line(horizonCenter, intersectedPt[0]);
                reachedCandidate.Add(reachedLay);
            }

            reachedCandidate.Sort((x,y)=>-(x.Length.CompareTo(y.Length)));
            corridorDir = reachedCandidate[0].UnitTangent;

            Point3d deciderPt = horizonCenter + corridorDir * 1;
            int decider = horizonAxis.PointAt(0).DistanceTo(deciderPt).CompareTo(horizonAxis.PointAt(1).DistanceTo(deciderPt));
            Point3d basePt = new Point3d();

            if (decider > 0)
                basePt = horizonAxis.PointAt(0);
            else
                basePt = horizonAxis.PointAt(1);

            Plane appendixPln = new Plane(basePt, corridorDir, -verticalAxis.UnitTangent);
            Rectangle3d appendixRect = new Rectangle3d(appendixPln, basePt, basePt + appendixPln.XAxis * 1200 / scale - appendixPln.YAxis * 1200 / scale);

            baseAppendix.Add(appendixRect.ToPolyline());

            return baseAppendix;
        }

        public Polyline DrawSurrounding(Polyline landing, Polyline coreLine, List<Line> corridorAxis, Polyline outline, Polyline appendix, double lengthRatio)
        {
            Polyline surrounding = new Polyline();
            double scale = 1;

            //set axis
            Line horizonAxis = corridorAxis[0];
            Line verticalAxis = corridorAxis[1];
            double coverAllLength = new BoundingBox(outline).Diagonal.Length * 2;

            Point3d landingCenter = landing.CenterPoint(); //랜딩이 직사각형이 아닐 수 있음.. 
            
            Point3d appendixCenter = appendix.CenterPoint();
            Vector3d appendixDir = new Line(landingCenter, appendixCenter).UnitTangent;
            

            List<Line> verticalLaySet = new List<Line>();
            verticalLaySet.Add(new Line(appendixCenter, verticalAxis.UnitTangent, coverAllLength));
            verticalLaySet.Add(new Line(appendixCenter, verticalAxis.UnitTangent, -coverAllLength));

            List<Line> reachedLays = new List<Line>();
            
            foreach (Line i in verticalLaySet)
            {
                var vIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i.ToNurbsCurve(),outline.ToNurbsCurve(),0,0);
                List<Point3d> intersectedPt = new List<Point3d>();

                foreach (var j in vIntersection)
                    intersectedPt.Add(j.PointA);

                intersectedPt.Sort((x, y) => -(x.DistanceTo(appendixCenter).CompareTo(y.DistanceTo(appendixCenter))));

                reachedLays.Add(new Line(appendixCenter, intersectedPt[0]));
            }

            reachedLays.Sort((x, y) => x.Length.CompareTo(y.Length));
            Vector3d surroundingDir = reachedLays[0].UnitTangent;


            //set length
            Line lengthLay = new Line(landingCenter, surroundingDir, coverAllLength);

            List<Point3d> lengthReachedPt = new List<Point3d>();

            var lengthDecider = Rhino.Geometry.Intersect.Intersection.CurveCurve(lengthLay.ToNurbsCurve(), coreLine.ToNurbsCurve(),0,0);
            foreach (var i in lengthDecider)
                lengthReachedPt.Add(i.PointA);

            lengthReachedPt.Sort((x, y) => x.DistanceTo(landingCenter).CompareTo(y.DistanceTo(landingCenter)));
            double surroundingLength = new Line(landingCenter, lengthReachedPt[0]).Length - 600 / scale;


            //draw surrounding

            Plane surrPlane = new Plane(appendixCenter, appendixDir, surroundingDir);
            Point3d surrCorner1 = appendixCenter - surrPlane.XAxis * 600 / scale - surrPlane.YAxis * 600 / scale;
            Point3d surrCorner2 = appendixCenter + surrPlane.XAxis * 600 / scale + surrPlane.YAxis * (surroundingLength + 600 / scale);

            surrounding = new Rectangle3d(surrPlane, surrCorner1, surrCorner2).ToPolyline();
            
            return surrounding;
        }

        public Polyline DrawCorridor(Plane surrPlane, Polyline surrounding, Polyline outline, double lengthRatio)
        {
            Polyline corridor = new Polyline();



            return corridor;
        }
    }
}
