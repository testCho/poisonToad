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
        /// <summary>
        /// 직선일때만 쓰삼
        /// xy평면으로 -90도 회전한 유닛벡터.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>

        public static Vector3d PV(this Curve c)
        {
            var tempv = c.TangentAtStart;
            tempv.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            return tempv;
        }

        public static bool IsOverlap(this Curve curve, Curve otherCurve)
        {
            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, curve, 0, 0);
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
                foreach (var j in tempIntersection)
                {
                    if (j.IsOverlap)
                        return true;
                }
            }
            return false;
        }

        public static void AlignCC(this Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }
    }



}
