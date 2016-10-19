using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern1
    {
        public Pattern1(Polyline coreLine, Polyline landing, Polyline outline)
        { }

        public List<Curve> MakeCorridor(Polyline coreLine, Polyline landing, Polyline outline)
        {
            //output
            List<Polyline> output = new List<Polyline>();

            //process
            List<Point3d> landingVertex = Tools.GetVertex(landing);
            List<Vector3d> landingAlign = Tools.SegmentVector.GetAlign(landing, true);
            List<Vector3d> landingPerp = Tools.SegmentVector.GetPerpendicular(landing, true);

            double coverAllLength = new BoundingBox(outline).Diagonal.Length * 2;

            List<Polyline> landingAlignRects = new List<Polyline>();

            for (int i = 0; i < landingVertex.Count; i++)
            {
                Plane tempPlane = new Plane(landingVertex[i], landingAlign[i], landingPerp[i]);
                Rectangle3d tempAlignRects = new Rectangle3d(tempPlane, tempPlane.Origin - tempPlane.XAxis * coverAllLength, tempPlane.Origin + tempPlane.XAxis * coverAllLength + tempPlane.YAxis * 1200);
                landingAlignRects.Add(tempAlignRects.ToPolyline());
            }

            double smallestSum = 0;
            Polyline baseRect = new Polyline();

            foreach (Polyline i in landingAlignRects)
            {
                double tempSum = 0;
                Curve[] tempCrvList = Curve.CreateBooleanIntersection(coreLine.ToNurbsCurve(), i.ToNurbsCurve());

                foreach (Curve j in tempCrvList)
                    tempSum += Tools.GetArea(j);

                if ((tempSum < smallestSum) || (smallestSum == 0))
                {
                    smallestSum = tempSum;
                    baseRect = i;
                }
            }

            List<Curve> corridors = Curve.CreateBooleanIntersection(baseRect.ToNurbsCurve(), outline.ToNurbsCurve()).ToList();
            

            return corridors;
        }
        public List<Polyline> MakeChamber(Polyline coreLine, Polyline corridor, Polyline outline)
        {
            List<Polyline> chambers = new List<Polyline>();

            
            return chambers;
        }
    }
}
