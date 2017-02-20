using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;
using Rhino;


namespace SmallHousing.Utility
{
    public static class PolylineExtended
    {
        //polyline extended
        public static void AlignCC(this Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }

        public static double GetArea(this Polyline poly)
        {
            if (!poly.IsClosed)
                return 0;

            List<Point3d> y = new List<Point3d>(poly);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        public static double GetCoverageArea(this List<Polyline> polys)
        {
            List<PolylineCurve> polylinecurves = polys.Select(n => new PolylineCurve(n.Select(m => new Point3d(m.X,m.Y,0)))).ToList();
            var cunion = Curve.CreateBooleanUnion(polylinecurves);
            if (cunion.Length != 0)
            {
                return AreaMassProperties.Compute(cunion).Area;
            }

            List<Brep> tomerge = new List<Brep>();
            for (int i = 1; i < polys.Count; i++)
            {

                Polyline temp = new Polyline(polys[i].Select(n => new Point3d(n.X, n.Y, 0)));
                Brep tempb = Brep.CreateEdgeSurface(new PolylineCurve(temp).DuplicateSegments());
                tomerge.Add(tempb);
            }

            var union = Brep.CreateBooleanUnion(tomerge, 0.1);
            Rhino.RhinoDoc.ActiveDoc.Objects.Add(union[0]);
            return union[0].GetArea();
        }

    }
}
