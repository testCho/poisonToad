using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class VectorTools
    {
        public static Vector3d RotateVectorXY(Vector3d baseVector, double angle)
        {
            Vector3d rotatedVector = new Vector3d(baseVector.X * Math.Cos(angle) - baseVector.Y * Math.Sin(angle), baseVector.X * Math.Sin(angle) + baseVector.Y * Math.Cos(angle), 0);
            return rotatedVector;
        }

        public static Vector3d ChangeCoordinate(Vector3d baseVector, Plane fromPln, Plane toPln)
        {
            Vector3d changedVector = baseVector;
            changedVector.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedVector;
        }

        /// <summary>
        /// 주어진 폴리라인 위의 한 세그먼트에 대해 폴리라인 내부로 향하는 단위벡터를 구해줍니다.
        /// </summary>
        public static Vector3d GetInnerPerpUnit(Line segment, Polyline boundary, double tolerance)
        {
            Vector3d perpVector = RotateVectorXY(segment.UnitTangent, Math.PI / 2);
            Vector3d perpVector2 = RotateVectorXY(segment.UnitTangent, -Math.PI / 2);
            Point3d basePt = segment.PointAt(0.5) + perpVector * tolerance;
            int decider = (int)boundary.ToNurbsCurve().Contains(basePt);

            if (decider == 1)
                return perpVector;

            return perpVector2;
        }
    }
}
