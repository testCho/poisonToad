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
        //main method
        public static List<Polyline> MakeCorridor(Polyline outline, Core core)
        {
            Line baseLine = SearchBaseLine(core);
            List<Line> subAxis = new List<Line>();
            List<Line> baseAxis = SetBaseAxis(outline, core, baseLine, out subAxis);

            List<corridorType> availableTypes = DetectCorridorType(outline, core, baseAxis, subAxis);
            List<Polyline> corridor = new List<Polyline>();

            return corridor;
        }


        //method
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

        private static List<corridorType> DetectCorridorType(Polyline outline, Core core, List<Line> baseAxis, List<Line> subAxis)
        {
            //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
            List<corridorType> available = new List<corridorType>();

            Point3d basePt = baseAxis[0].PointAt(0);

            double toOutlineDistH = subAxis[0].Length;
            double toCoreDistH = PCXTools.ExtendFromPt(basePt, core.CoreLine, subAxis[0].UnitTangent).Length;
            double toOutlineDistV = baseAxis[1].Length;
            double toCoreDistV = PCXTools.ExtendFromPt(basePt, core.CoreLine, baseAxis[1].UnitTangent).Length;

            bool IsHorizontalOff = toOutlineDistH > toCoreDistH;
            bool IsVerticalOff = toOutlineDistV > toCoreDistV;

            if (IsHorizontalOff)
                available.Add(corridorType.DH2);
            else if (IsVerticalOff)
            {
                available.Add(corridorType.SV);
                available.Add(corridorType.DH1);
            }
            else
            {
                available.Add(corridorType.SH);
                available.Add(corridorType.SV);
                available.Add(corridorType.DH1);
                available.Add(corridorType.DV);

            }
            return available;
        }              
        
        private static double SetBasePtVLimit(Core core, Line baseLine)
        {
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;

            double candidate1 = PCXTools.ExtendFromPt(decidingPt1, core.Landing, -core.UpstairDirec).Length + 0.01;
            double candidate2 = PCXTools.ExtendFromPt(decidingPt2, core.Landing, -core.UpstairDirec).Length + 0.01;

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
            Point3d basePt = baseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * SetBasePtVLimit(core, baseLine) / 2;

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.ExtendFromPt(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.ExtendFromPt(basePt, outline, -baseLine.UnitTangent);

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
            Line verticalReached1 = PCXTools.ExtendFromPt(basePt, outline, core.UpstairDirec);
            Line verticalReached2 = PCXTools.ExtendFromPt(basePt, outline, -core.UpstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
            {
                baseAxis.Add(verticalReached1);
                baseAxis.Add(verticalReached2);
            }
            else
            {
                baseAxis.Add(verticalReached2);
                baseAxis.Add(verticalReached1);
            }

            counterAxis = subAxis;
            return baseAxis;

        }


    }
}
