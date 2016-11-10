using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class RoomMaker
    {

        //main method
        public List<Polyline> MakeRoom(List<double> roomAreaSet, Polyline outline, Core core)
        {
            List<Polyline> rooms = new List<Polyline>();

            Line baseLine = SearchBaseLine(core);
            List<Line> subAxis = new List<Line>();
            List<Line> baseAxis = SetBaseAxis(outline, core, baseLine, out subAxis);

            List<corridorType> availableTypes = DetectCorridorType(outline, core, baseAxis, subAxis);


            return rooms;
        }

        //method
        private static List<corridorType> DetectCorridorType(Polyline outline, Core core, List<Line> baseAxis, List<Line> subAxis)
        {
            //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
            List<corridorType> available = new List<corridorType>();

            Point3d basePt = baseAxis[0].PointAt(0);

            double toOutlineDistH = subAxis[0].Length;
            double toCoreDistH = PCXTools.ExtendFromPt(basePt, core.CoreLine, subAxis[0].UnitTangent).Length;
            double toOutlineDistV = baseAxis[1].Length;
            double toCoreDistV = PCXTools.ExtendFromPt(basePt, core.CoreLine, baseAxis[1].UnitTangent).Length;

            bool deciderH = toOutlineDistH > toCoreDistH;
            bool deciderV = toOutlineDistV > toCoreDistV;

            if (deciderH)
                available.Add(corridorType.DH2);
            else if (deciderV)
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

    }

    //enum
    public enum LineType { Core, Corridor, Outer, Inner } // 선타입 - 코어, 복도, 외벽, 내벽
    public enum corridorType { SH, SV, DH1, DH2, DV } //복도타입 - S:single 편복도, D: double 중복도, H: 횡축, V: 종축, 1:단방향, 2:양방향

    //data structure class

}
