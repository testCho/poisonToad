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
        class SubPatternDecider
        {
            public static ICorridorP1Sub GetPattern(Polyline outline, P1Core core, List<double> areaSet)
            {

                //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
                double stickTolerance = 0.5;
                List<List<Line>> axisSet = AxisMaker.Make(outline, core);
                List<Line> mainAxis = axisSet[0];
                List<Line> subAxis = axisSet[1];

                //set distance
                Point3d basePt = core.CenterPt;

                double toOutlineDistH = subAxis[0].Length;
                double toCoreDistH = PCXTools.PCXByEquation(basePt, core.CoreLine, subAxis[0].UnitTangent).Length;
                double toOutlineDistV = mainAxis[1].Length;
                double toCoreDistV = PCXTools.PCXByEquation(basePt, core.CoreLine, mainAxis[1].UnitTangent).Length;
                double toLandingDistV = basePt.DistanceTo(core.BaseLine.PointAt(0.5));

                //set decider
                bool IsHorizontalOff = toOutlineDistH > toCoreDistH + stickTolerance;
                bool IsHEnoughOff = toOutlineDistH > toCoreDistH + CorridorDimension.TwoWayWidth + stickTolerance;
                bool IsVerticalOff = toOutlineDistV > toCoreDistV + stickTolerance;
                bool IsVEnoughOff = toOutlineDistV > CorridorDimension.MinRoomWidth + toLandingDistV + stickTolerance;

                bool IsHLognerThanV = mainAxis[0].Length > subAxis[1].Length;


                //seive
                if (outline.GetArea() == core.CoreLine.GetArea())
                    return new CorridorP1S0();

                if (areaSet.Count <= 1)
                    return new CorridorP1S0();

                //compare
                if (IsHorizontalOff)
                {
                    if (IsVerticalOff)
                    {
                        if (!IsVEnoughOff)
                            return new CorridorP1S0();

                        if (IsHEnoughOff)
                            return new CorridorP1S3();

                        return new CorridorP1S4();
                    }

                    if (IsHEnoughOff)
                        return new CorridorP1S3();

                    return new CorridorP1S4();
                }

                if (IsVerticalOff)
                {
                    if (IsHLognerThanV)
                    {
                        if (!IsVEnoughOff)
                            return new CorridorP1S0();

                        return new CorridorP1S4();
                    }

                    return new CorridorP1S2();
                }

                else
                {
                    if (IsHLognerThanV)
                    {
                        CorridorP1S1 corrP1 = new CorridorP1S1();
                        if (mainAxis[0].Length < (mainAxis[1].Length + CorridorDimension.OneWayWidth / 2) * 2.0)
                        {
                            if(areaSet.Count <= 3)
                            corrP1.Param = new List<double> { 0.0 };

                            if (areaSet.Count > 3)
                            {
                                CorridorP1S2 corrP2 = new CorridorP1S2();
                                corrP2.Param = new List<double> { 0.0 };
                                return corrP2;
                            }
                        }
                        return corrP1;
                    }

                    if (subAxis[1].Length < (mainAxis[1].Length + CorridorDimension.OneWayWidth / 2) * 2.0)
                    {
                            CorridorP1S2 corrP2 = new CorridorP1S2();
                            corrP2.Param = new List<double> { 0.0 };
                            return corrP2;
                    }
 
                    return new CorridorP1S2();
                }
            }
        }
    }
}
