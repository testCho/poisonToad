using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PolylineTools
    {
        public static double GetArea(Polyline input)
        {
            List<Point3d> y = new List<Point3d>(input);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        /// <summary>
        /// 닫힌 폴리라인 방향을 반시계 방향으로 만들어줍니다. (꼭지점의 index 순서 기준)
        /// </summary>
        public static Polyline AlignPolyline(Polyline polyline)
        {
            Polyline output = new Polyline(polyline);
            if (output.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
            {
                output.Reverse();
            }
            return output;
        }

        public static List<Point3d> GetVertex(Polyline polyline)
        {
            List<Point3d> tempVertex = new List<Point3d>(polyline);
            tempVertex.RemoveAt(tempVertex.Count - 1);

            return tempVertex;
        }

        public static void SetPolylineAlignCCW(Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }

        public static void SetPolylineAlignCW(Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }

        /// <summary>
        /// 폴리라인의 각 세그먼트들에 대해 평행한 또는 안쪽으로 수직인 벡터 리스트를 구해줍니다.(반시계방향 기준)
        /// </summary>
        public class SegmentVector
        {
            public static List<Vector3d> GetAlign(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> alignVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    alignVector.Add(tempVector);
                }
                return alignVector;
            }

            public static List<Vector3d> GetPerpendicular(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> perpVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length
                    tempVector.Transform(Transform.Rotation(Math.PI / 2, tempStart));

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    perpVector.Add(tempVector);
                }
                return perpVector;
            }
        }

        /// <summary>
        /// Offset 대상인 폴리라인의 벡터를 이용해 Offset된 폴리라인의 내부 고리를 없애줍니다.
        /// </summary>
        public static List<Curve> RemoveLoop(Polyline loopedPoly, Polyline baseBound, double tolerance)
        {
            Curve loopedCrv = loopedPoly.ToNurbsCurve();
            Curve boundCrv = baseBound.ToNurbsCurve();
            var tempX = Rhino.Geometry.Intersect.Intersection.CurveSelf(loopedCrv, 0);

            List<double> splitParam = new List<double>();
            foreach (var i in tempX)
            {
                splitParam.Add(i.ParameterA);
                splitParam.Add(i.ParameterB);
            }

            List<Curve> splittedSegment = loopedCrv.Split(splitParam).ToList();
            List<Curve> loopOut = new List<Curve>();

            foreach (Curve i in splittedSegment)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Vector3d testVec = new Vector3d(j.PointAtEnd - j.PointAtStart);
                    Point3d testBase = new Point3d((j.PointAtStart + j.PointAtEnd) / 2);
                    testVec.Transform(Transform.Scale(j.PointAtStart, 1 / testVec.Length));
                    testVec.Transform(Transform.Rotation(Math.PI / 2, j.PointAtStart));
                    Point3d testPt = new Point3d(testBase + testVec * tolerance);

                    int decider = (int)loopedCrv.Contains(testPt, Plane.WorldXY, tolerance / 2);
                    int decider2 = (int)boundCrv.Contains(testPt, Plane.WorldXY, tolerance / 2);

                    if ((decider != 2) && (decider2 != 2))
                        loopOut.Add(j);
                }
            }

            List<Curve> joined = Curve.JoinCurves(loopOut).ToList();
            List<Curve> final = new List<Curve>();
            foreach (Curve k in joined)
            {
                Boolean decider3 = k.IsClosed;
                if (decider3 == true)
                    final.Add(k);
            }

            return final;
        }

        /// <summary>
        /// 폴리라인의 각 변마다 거리를 지정해 Offset 커브를 만들어줍니다.
        /// </summary>
        public static List<Curve> ImprovedOffset(Polyline bound, List<double> offsetDist)
        {
            Polyline ccwBound = AlignPolyline(bound);
            List<Point3d> trimmedOffsetPt = new List<Point3d>();

            //set vectors
            List<Vector3d> alignVector = SegmentVector.GetAlign(ccwBound, true);
            List<Vector3d> perpVector = SegmentVector.GetPerpendicular(ccwBound, true);
            List<Point3d> boundVertex = GetVertex(ccwBound);

            int numSegment = alignVector.Count;
            int numVertex = boundVertex.Count;


            //offset and trim segments
            for (int i = 0; i < numSegment; i++)
            {
                double a = offsetDist[i];
                double b = offsetDist[(i + 1) % offsetDist.Count];
                double dotProduct = Vector3d.Multiply(alignVector[i], alignVector[(i + 1) % numSegment]);
                Vector3d crossProduct = Vector3d.CrossProduct(alignVector[i], alignVector[(i + 1) % numSegment]);
                double cos = Math.Abs(dotProduct / (alignVector[i].Length * alignVector[(i + 1) % numSegment].Length));
                double sin = Math.Sqrt(1 - Math.Pow(cos, 2));

                double decider1 = Vector3d.Multiply(Plane.WorldXY.ZAxis, crossProduct);
                double decider2 = Vector3d.Multiply(-alignVector[i], alignVector[(i + 1) % numSegment]);

                Point3d tempPt = new Point3d();

                if (decider1 > 0.005) // concave
                {
                    if (decider2 < 0) // blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos - b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos - b) / sin);
                }

                else if (decider1 < -0.005) // convex
                {
                    if (decider2 < 0) //blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos + b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos + b) / sin);
                }

                else //straight & near straight
                    tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * Math.Max(a, b);

                trimmedOffsetPt.Add(tempPt);
            }

            trimmedOffsetPt.Add(trimmedOffsetPt[0]);
            Polyline offBound = new Polyline(trimmedOffsetPt);

            //remove loop
            //List<Curve> loopOut = RemoveLoop(offBound, ccwBound, 0.001);
            //return loopOut;

            List<Curve> debug = new List<Curve>();
            debug.Add(offBound.ToNurbsCurve());
            return debug;
        }

        public static Polyline ChangeCoordinate(Polyline basePoly, Plane fromPln, Plane toPln)
        {
            Polyline changedPoly = new Polyline(basePoly);
            changedPoly.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedPoly;
        }

        public static bool IsOverlap(Polyline poly1, Polyline poly2)
        {
            Curve polyCrv1 = poly1.ToNurbsCurve();
            Curve polyCrv2 = poly2.ToNurbsCurve();

            var polyIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(polyCrv1, polyCrv2, 0, 0);
            foreach (var i in polyIntersection)
            {
                if (i.IsOverlap)
                    return true;
            }

            return false;
        }

    }
}
