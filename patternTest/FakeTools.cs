using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Test
    {
        public static Vector3d MakeUpDirec(Polyline coreLine, Polyline landing)
        {
            Point3d basePt = landing.CenterPoint();
            Curve coreCrv = coreLine.ToNurbsCurve();
            List<Curve> coreSeg = coreCrv.DuplicateSegments().ToList();
            coreSeg.Sort(delegate (Curve x, Curve y)
            {
                double xParam = new double();
                x.ClosestPoint(basePt, out xParam);
                double xDist = basePt.DistanceTo(x.PointAt(xParam));

                double yParam = new double();
                y.ClosestPoint(basePt, out yParam);
                double yDist = basePt.DistanceTo(y.PointAt(yParam));

                if (xDist == yDist)
                    return 0;
                else if (xDist > yDist)
                    return -1;
                else

                    return 1;
            });

            double finalParam = new double();
            coreSeg[0].ClosestPoint(basePt, out finalParam);
            Point3d closestPt = coreSeg[0].PointAt(finalParam);

            return -new Line(basePt, closestPt).UnitTangent;
        }
    }


}
