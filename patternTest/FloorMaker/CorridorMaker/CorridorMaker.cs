using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class CorridorMaker
    {
        //field
        private ICorridorDecider testDecider = new CorridorDeciderForTest();

        //property
        public ICorridorPattern Pattern { get; set; }
        public Core Core { get; set; }
        public Polyline Outline { get; set;}


        //constructor
        public CorridorMaker(Polyline outline, Core core)
        {
            this.Outline = outline;
            this.Core = core;
        }
        

        //main method
        public List<Polyline> Make()
        {
            /*for proto*/
            if (Pattern == null)
                return null;
            /*for proto*/

            Line baseLine = SearchBaseLine(Core);
            List<Line> subAxis = new List<Line>();
            List<Line> baseAxis = SetBaseAxis(Outline, Core, baseLine, out subAxis);                       
            List<Polyline> corridor = Pattern.Draw(baseLine, baseAxis, Core, Outline);
            
            return corridor;
        }

        public ICorridorPattern RecommandPattern() { return testDecider.GetPattern(Core, Outline); }


        //drawing method
        private static Line SearchBaseLine(Core core)
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
                     
        private static double SetBasePtVLimit(Core core, Line baseLine)
        {
            double vTolerance = 1;
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * vTolerance;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * vTolerance; 

            double candidate1 = PCXTools.PCXByEquationStrict(decidingPt1, core.Landing, -core.UpstairDirec).Length+ vTolerance;
            double candidate2 = PCXTools.PCXByEquationStrict(decidingPt2, core.Landing, -core.UpstairDirec).Length+ vTolerance;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            return vLimit;
        }

        private static List<Line> SetBaseAxis(Polyline outline, Core core, Line baseLine, out List<Line> counterAxis)
        {
            //output
            List<Line> baseAxis = new List<Line>();
            List<Line> subAxis = new List<Line>();

            //process
            double basePtY = SetBasePtVLimit(core, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * SetBasePtVLimit(core, baseLine) / 2.0;

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.PCXByEquation(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.PCXByEquation(basePt, outline, -baseLine.UnitTangent);

            if (horizonReached1.Length > horizonReached2.Length)
            {
                baseAxis.Add(horizonReached1);
                subAxis.Add(horizonReached2);
            }
            else
            {
                baseAxis.Add(horizonReached2);
                subAxis.Add(horizonReached1);
            }

            //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택
            Line verticalReached1 = PCXTools.PCXByEquation(basePt, outline, core.UpstairDirec);
            Line verticalReached2 = PCXTools.PCXByEquation(basePt, outline, -core.UpstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
            {
                baseAxis.Add(verticalReached1);
                subAxis.Add(verticalReached2);
            }
            else
            {
                baseAxis.Add(verticalReached2);
                subAxis.Add(verticalReached1);
            }

            counterAxis = subAxis;
            return baseAxis;

        }

    }


}
