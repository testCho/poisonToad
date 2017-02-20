using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;

namespace SmallHousing.CorridorPatterns
{
    partial class CorridorP1
    {
        private interface ICorridorP1Sub
        {
            string Name { get; }
            List<double> Param { get; set; }

            Floor ParentFloor { set; }
            List<List<Line>> AxisSet { get; set; }
            P1Core Core { get; set; }

            List<Polyline> Build();
        }

        private class P1Core
        {
            //property
            public Polyline CoreLine { get; private set; }
            public Polyline Landing { get; private set; }
            public Vector3d UpstairDirec { get; private set; }
            public Line BaseLine { get; private set; }
            public Point3d CenterPt { get; private set; }

            //constructor
            public P1Core(Core core)
            {
                CoreLine = core.Outline;
                Landing = core.Landing;
                UpstairDirec = MakeUpDirec(this.CoreLine, this.Landing);
                BaseLine = SearchBaseLine();
                CenterPt = SearchLandingCenter();
            }

            private Vector3d MakeUpDirec(Polyline coreLine, Polyline landing)
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

            private Line SearchBaseLine()
            {
                //output
                Line baseSeg = new Line();

                //process
                List<Line> landingSeg = Landing.GetSegments().ToList();
                List<Line> perpToStair = new List<Line>();

                double perpTolerance = 0.005;

                foreach (Line i in landingSeg)
                {
                    double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, UpstairDirec));
                    if (axisDecider < perpTolerance)
                        perpToStair.Add(i);
                }

                perpToStair.Sort(delegate (Line x, Line y)
                {
                    Point3d perp1Center = x.PointAt(0.5);
                    Point3d perp2Center = y.PointAt(0.5);

                    Vector3d gapBetween = perp1Center - perp2Center;
                    double decider = Vector3d.Multiply(gapBetween, UpstairDirec);

                    if (decider > 0)
                        return -1;
                    else if (decider == 0)
                        return 0;
                    else
                        return 1;
                });

                baseSeg = perpToStair[0];

                return baseSeg;
            }

            private Point3d SearchLandingCenter()
            {
                double vTolerance = 1;
                double vLimit = 0;

                Point3d decidingPt1 = BaseLine.PointAt(0.01) - UpstairDirec / UpstairDirec.Length * vTolerance;
                Point3d decidingPt2 = BaseLine.PointAt(0.09) - UpstairDirec / UpstairDirec.Length * vTolerance;

                double candidate1 = PCXTools.PCXByEquationStrict(decidingPt1, Landing, -UpstairDirec).Length + vTolerance;
                double candidate2 = PCXTools.PCXByEquationStrict(decidingPt2, Landing, -UpstairDirec).Length + vTolerance;

                if (candidate1 > candidate2)
                    vLimit = candidate2;
                else
                    vLimit = candidate1;

                Point3d basePt = BaseLine.PointAt(0.5) - (UpstairDirec /UpstairDirec.Length) * vLimit / 2.0;
                return basePt;
            }
        }

        public class CorridorDimension
        {
            //property
            public static double MinRoomWidth { get { return Dimensions.MinRoomWidth; } private set { } }
            public static double OneWayWidth { get { return Dimensions.OneWayCorridorWidth; } private set { } }
            public static double TwoWayWidth { get { return Dimensions.TwoWayCorridorWidth; } private set { } }
        }
    }
}
