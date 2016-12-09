using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PCXTools
    {
        public static Line ExtendFromPt(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            Line output = new Line();

            double coverAllLength = new BoundingBox(boundary).Diagonal.Length * 2;
            LineCurve lay = new LineCurve(basePt, basePt + direction / direction.Length * coverAllLength);

            var layIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(lay, boundary.ToNurbsCurve(), 0, 0);

            List<Point3d> intersectedPts = new List<Point3d>();
            foreach (var i in layIntersection)
            {
                if (i.IsOverlap)
                    intersectedPts.Add(i.PointA2);
                else if (i.PointA != basePt)
                    intersectedPts.Add(i.PointA);
            }

            intersectedPts.Sort((x, y) => basePt.DistanceTo(x).CompareTo(basePt.DistanceTo(y)));

            output = new Line(basePt, intersectedPts[0]);

            return output;
        }

        public static Line PCXByEquation(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            double onCurveTolerance = 0.005;

            double coverAllLength = new BoundingBox(boundary).Diagonal.Length * 2;
            Line testLine = new Line(basePt, basePt + direction / direction.Length * coverAllLength);
            List<Line> boundarySeg = boundary.GetSegments().ToList();
            List<Point3d> crossPtCandidate = new List<Point3d>();

            foreach (Line i in boundarySeg)
            {
                Point3d tempCrossPt = CCXTools.GetCrossPt(testLine, i);
                if (IsPtOnLine(tempCrossPt, testLine, onCurveTolerance) && IsPtOnLine(tempCrossPt, i, onCurveTolerance))
                    crossPtCandidate.Add(tempCrossPt);
            }

            crossPtCandidate.Sort((a, b) => (basePt.DistanceTo(a).CompareTo(basePt.DistanceTo(b))));

            if (crossPtCandidate.Count != 0)
                return new Line(basePt, crossPtCandidate[0]);
            else
                return new Line();
        }

        public static Boolean IsPtOnLine(Point3d testPt, Line testLine, double tolerance)
        {
            if (testPt == Point3d.Unset)
                return false;

            List<Point3d> linePtList = new List<Point3d>();
            linePtList.Add(testLine.PointAt(0));
            linePtList.Add(testLine.PointAt(1));


            linePtList.Sort((a, b) => (a.X.CompareTo(b.X)));
            double minX = linePtList.First().X;
            double maxX = linePtList.Last().X;

            linePtList.Sort((a, b) => (a.Y.CompareTo(b.Y)));
            double minY = linePtList.First().Y;
            double maxY = linePtList.Last().Y;

            linePtList.Sort((a, b) => (a.Z.CompareTo(b.Z)));
            double minZ = linePtList.First().Z;
            double maxZ = linePtList.Last().Z;



            //isOnOriginTest
            bool isSatisfyingX = (testPt.X - (minX - tolerance)) * ((maxX + tolerance) - testPt.X) >= 0;
            bool isSatisfyingY = (testPt.Y - (minY - tolerance)) * ((maxY + tolerance) - testPt.Y) >= 0;
            bool isSatisfyingZ = (testPt.Z - (minZ - tolerance)) * ((maxZ + tolerance) - testPt.Z) >= 0;

            if (isSatisfyingX && isSatisfyingY && isSatisfyingZ)
                return true;

            return false;
        }
    }
}
