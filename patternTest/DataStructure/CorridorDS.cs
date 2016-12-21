using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    public class Core
    {
        //property
        public Polyline CoreLine { get; private set; }
        public Polyline Landing { get; private set; }
        public Vector3d UpstairDirec { get; private set; }
        public Line BaseLine { get; private set; }
        public Point3d CenterPt { get; private set; }

        //constructor
        public Core(Polyline coreLine, Polyline landing)
        {
            CoreLine = coreLine;
            Landing = landing;
            UpstairDirec = MakeUpDirec(coreLine, landing);
            BaseLine = SearchBaseLine(this);
            CenterPt = SearchLandingCenter(this);
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

        private Line SearchBaseLine(Core core)
        {
            //output
            Line baseSeg = new Line();

            //process
            List<Line> landingSeg = core.Landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, core.UpstairDirec));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort(delegate (Line x, Line y)
            {
                Point3d perp1Center = x.PointAt(0.5);
                Point3d perp2Center = y.PointAt(0.5);

                Vector3d gapBetween = perp1Center - perp2Center;
                double decider = Vector3d.Multiply(gapBetween, core.UpstairDirec);

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

        private Point3d SearchLandingCenter(Core core)
        {
            double vTolerance = 1;
            double vLimit = 0;

            Point3d decidingPt1 = core.BaseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * vTolerance;
            Point3d decidingPt2 = core.BaseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * vTolerance;

            double candidate1 = PCXTools.PCXByEquationStrict(decidingPt1, core.Landing, -core.UpstairDirec).Length + vTolerance;
            double candidate2 = PCXTools.PCXByEquationStrict(decidingPt2, core.Landing, -core.UpstairDirec).Length + vTolerance;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            Point3d basePt = core.BaseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * vLimit / 2.0;
            return basePt;
        }
    }

    public class CorridorDimension
    {
        //field
        private static double scale = 1;
        private static double ONE_WAY_CORRIDOR_WIDTH = 1200;
        private static double TWO_WAY_CORRIDOR_WIDTH = 1800;
        private static double MINIMUM_ROOM_WIDTH = 3000;
        private static double MINIMUM_CORRIDOR_LENGTH = 900; //임시

        //property
        public static double MinRoomWidth { get { return MINIMUM_ROOM_WIDTH / scale; } private set { } }
        public static double OneWayWidth { get { return ONE_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double TwoWayWidth { get { return TWO_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double MinLengthForDoor { get { return MINIMUM_CORRIDOR_LENGTH / scale; } private set { } }

    }


    //interface
    public interface ICorridorPattern
    {
        string Name { get; }

        List<Polyline> Draw(List<Line> mainAxis, Core core, Polyline outline);
        List<double> Param { get; set; }
        List<string> ParamName { get; }        
        
    }

    public interface ICorridorDecider
    {
        ICorridorPattern GetPattern(Polyline outline, Core core);
    }

}
