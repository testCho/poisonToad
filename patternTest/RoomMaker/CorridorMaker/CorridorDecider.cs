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
        public ICorridorPattern GetCorridorPattern(Polyline outline, Core core, List<Line> baseAxis, List<Line> subAxis)
        {
            double stickTolerance = 0.005;

            //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
            Point3d basePt = baseAxis[0].PointAt(0);

            double toOutlineDistH = subAxis[0].Length;
            double toCoreDistH = PCXTools.ExtendFromPt(basePt, core.CoreLine, subAxis[0].UnitTangent).Length;
            double toOutlineDistV = baseAxis[1].Length;
            double toCoreDistV = PCXTools.ExtendFromPt(basePt, core.CoreLine, baseAxis[1].UnitTangent).Length;

            bool IsHorizontalOff = toOutlineDistH > toCoreDistH+ stickTolerance;
            bool IsHEnoughOff = toOutlineDistH > toCoreDistH + Corridor.TwoWayWidth+ stickTolerance;
            bool IsVerticalOff = toOutlineDistV > toCoreDistV+ stickTolerance;

            if (IsHorizontalOff)
            {
                if (IsVerticalOff)
                    return null;

                if (IsHEnoughOff)
                    return new Pattern3D();

                return new Pattern3S();
            }

            else if (IsVerticalOff)
                return new Pattern2S();

            else
                return new Pattern1S();
        }
    }
}
