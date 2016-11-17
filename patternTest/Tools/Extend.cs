using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    public static class Extended
    {
        //curve extended
        public static bool IsOverlap(this Curve curve, Curve otherCurve)
        {
            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, curve, 0, 0);
            if (tempIntersection.Count == 0)
                return false;

            foreach (var i in tempIntersection)
            {
                if (i.IsOverlap)
                    return true;
            }

            return false;
        }

        public static bool IsOverlap(this Curve curve, List<Curve> otherCurves)
        {
            foreach (Curve i in otherCurves)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, i, 0, 0);
                if (tempIntersection.Count == 0)
                    return false;

                foreach (var j in tempIntersection)
                {
                    if (j.IsOverlap)
                        return true;
                }
            }
            return false;
        }

        //polyline extended
        public static void AlignCC(this Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }
    }

}
