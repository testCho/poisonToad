using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace SmallHousing.Utility
{
    class CCXTools
    {
        public static Polyline RegionIntersect(Polyline polyline1, Polyline polyline2)
        {
            Polyline resultPolyine = new Polyline();

            List<double> tempParamA = new List<double>(); //Polyline1 위의 교차점
            List<double> tempParamB = new List<double>(); //Polyline1 위의 교차점
            Curve polyCurve1 = polyline1.ToNurbsCurve();
            Curve polyCurve2 = polyline2.ToNurbsCurve();

            List<Curve> tempLocalResult = new List<Curve>();

            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(polyCurve1, polyCurve2, 0, 0);

            if (tempIntersection.Count == 0) //없으면 null..
                return resultPolyine;

            foreach (var i in tempIntersection)
            {
                tempParamA.Add(i.ParameterA);
                tempParamB.Add(i.ParameterB);
            }

            List<Curve> tempSplittedA = polyCurve1.Split(tempParamA).ToList();
            List<Curve> tempSplittedB = polyCurve2.Split(tempParamB).ToList();

            //case of Polyline1
            foreach (Curve i in tempSplittedA)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = new Point3d((j.PointAtEnd + j.PointAtStart) / 2);
                    int decider = (int)polyCurve2.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }

            //case of Polyline2
            foreach (Curve i in tempSplittedB)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = new Point3d((j.PointAtEnd + j.PointAtStart) / 2);
                    int decider = (int)polyCurve1.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }
            List<Curve> resultList = Curve.JoinCurves(tempLocalResult).ToList();
            resultList.OrderByDescending(i => CurveTools.GetArea(i));

            if (resultList.Count != 0)
                resultPolyine = CurveTools.ToPolyline(resultList[0]);

            return resultPolyine;
        }

        public static List<Curve> RegionIntersect(List<Curve> curveSet1, List<Curve> curveSet2)
        {
            List<Curve> IntersectCrvs = new List<Curve>();
            foreach (Curve i in curveSet1)
            {
                foreach (Curve j in curveSet2)
                {
                    List<double> tempParamA = new List<double>();
                    List<double> tempParamB = new List<double>();
                    List<Curve> tempLocalResult = new List<Curve>();

                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i, j, 0, 0);

                    if (tempIntersection.Count == 0) // 없으면 다음커브로..
                        continue;

                    foreach (var k in tempIntersection)
                    {
                        tempParamA.Add(k.ParameterA);
                        tempParamB.Add(k.ParameterB);
                    }

                    List<Curve> tempSplittedA = i.Split(tempParamA).ToList();
                    List<Curve> tempSplittedB = j.Split(tempParamB).ToList();

                    //case of Curve1
                    foreach (Curve k in tempSplittedA)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)j.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }

                    //case of Curve2
                    foreach (Curve k in tempSplittedB)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)i.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }
                    IntersectCrvs.AddRange(Curve.JoinCurves(tempLocalResult).ToList());
                }
            }
            return IntersectCrvs;
        }

        public static Point3d GetCrossPt(Line line1, Line line2)
        {
            //dSide: divider 쪽, oSide: origin 쪽
            Point3d origin1 = line1.PointAt(0);
            Vector3d direction1 = line1.UnitTangent;

            Point3d origin2 = line2.PointAt(0);
            Vector3d direction2 = line2.UnitTangent;

            //ABC is coefficient of linear Equation, Ax+By=C 
            double A1 = direction1.Y;
            double B1 = -direction1.X;
            double C1 = A1 * origin1.X + B1 * origin1.Y;

            double A2 = direction2.Y;
            double B2 = -direction2.X;
            double C2 = A2 * origin2.X + B2 * origin2.Y;

            //det=0: isParallel, 평행한 경우
            double detTolerance = 0.005;
            double det = A1 * B2 - B1 * A2;

            if (Math.Abs(det) < detTolerance)
                return Point3d.Unset;

            double perpX = (B2 * C1 - B1 * C2) / det;
            double perpY = (A1 * C2 - A2 * C1) / det;

            return new Point3d(perpX, perpY, origin1.Z);
        }
    }
}
