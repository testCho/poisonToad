using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Pattern1HRefac
    {
        public List<Line> SetMainAxis(Polyline landing, Vector3d upstairDirection)
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
            verticalAxis = new Line(horizontalAxis.PointAt(1), -upstairDirection / upstairDirection.Length, 1200 / scale);

            corridorAxis.Add(horizontalAxis);
            corridorAxis.Add(verticalAxis);

            return corridorAxis;
        }
    }
}
