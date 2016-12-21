using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class CorridorDeciderForTest : ICorridorDecider
    {
        public ICorridorPattern GetPattern(Polyline outline, Core core)
        {

            //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
            double stickTolerance = 0.5;
            List<List<Line>> axisSet = CorridorAxisMaker.MakeAxis(outline, core);
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
            bool IsHorizontalOff = toOutlineDistH > toCoreDistH+ stickTolerance;
            bool IsHEnoughOff = toOutlineDistH > toCoreDistH + CorridorDimension.TwoWayWidth+ stickTolerance;
            bool IsVerticalOff = toOutlineDistV > toCoreDistV+ stickTolerance;
            bool IsVEnoughOff = toOutlineDistV > CorridorDimension.MinRoomWidth+ toLandingDistV+ stickTolerance;

            bool IsHLognerThanV = mainAxis[0].Length > subAxis[1].Length;


            //compare
            if (IsHorizontalOff)
            {
                if (IsVerticalOff)
                {
                    if (!IsVEnoughOff)
                        return null;

                    if (IsHEnoughOff)
                        return new Corr_TwoWayHorizontal2();

                    return new Corr_TwoWayHorizontal1();
                }

                if (IsHEnoughOff)
                    return new Corr_TwoWayHorizontal2();

                return new Corr_TwoWayHorizontal1();
            }

            else if (IsVerticalOff)
            {
                if (IsHLognerThanV)
                {
                    if (!IsVEnoughOff)
                        return null;

                    return new Corr_TwoWayHorizontal1();
                }

                return new Corr_OneWayVertical1();
            }

            else
            {
                if (IsHLognerThanV)
                    return new Corr_OneWayHorizontal1();

                return new Corr_OneWayVertical1();
            }
        }
    }
}
