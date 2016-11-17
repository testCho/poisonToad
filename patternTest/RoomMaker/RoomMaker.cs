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

            //CorridorMaker class 안에서 해결할 것.. //
            Line baseLine = CorridorMaker.SearchBaseLine(core);
            List<Line> subAxis = new List<Line>();
            List<Line> baseAxis = CorridorMaker.SetBaseAxis(outline, core, baseLine, out subAxis);

            List<corridorType> availableTypes = DetectCorridorType(outline, core, baseAxis, subAxis);
            List<Polyline> corridor = new List<Polyline>();
            //여기까지//

            LabeledOutline outlineLabel = Labeler.GetOutlineLabel(outline, core, corridor);
            rooms = PartitionMaker.DrawPartitions(outlineLabel, roomAreaSet);

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
}
