using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PartitionMaker
    {
        //변수명 정리 요
        //main method
        public static List<Polyline> DrawPartitions(LabeledOutline outlineLabel, List<double> roomAreas)
        {
            List<Polyline> partitionList = new List<Polyline>();

            List<double> scaledAreas = NumberTools.ScaleToNewSum(PolylineTools.GetArea(outlineLabel.Trimmed), roomAreas);
            DividingLine dividerInitial = SetInitialDivider(outlineLabel);
            DividerParams paramInitial = new DividerParams(dividerInitial, dividerInitial.Origin, outlineLabel);

            for (int i = 0; i < scaledAreas.Count; i++)
            {
                DivMakerOutput eachPartition = DividerMaker.DrawEachPartition(paramInitial, scaledAreas[i]);
                partitionList.Add(eachPartition.Poly);

                paramInitial = eachPartition.DivParams;
            }

            return partitionList;
        }

        //method
        private static DividingLine SetInitialDivider(LabeledOutline outlineLabel)
        {
            RoomLine firstRoomLine = outlineLabel.Core[0];

            if (firstRoomLine.Type == LineType.Corridor)
                return DrawInitialDivider(firstRoomLine, outlineLabel.Pure);

            RoomLine nearestCorridor = DividerSetter.FindNearestCorridor(outlineLabel.Core, firstRoomLine);
            int parallelDecider = firstRoomLine.UnitTangent.IsParallelTo(nearestCorridor.UnitTangent);

            if (parallelDecider == 0) //첫번째 라인과 복도가 평행이 아닌 경우, not parallel
                return DrawInitialDivider(nearestCorridor, outlineLabel.Pure);
            else //평행인 경우
                return DrawInitialDivider(firstRoomLine, outlineLabel.Pure);

        }

        //다음라인에서 그려본 다음 길이가 짧은 쪽부터 시작 <- 할 필요 없을 듯
        private static DividingLine DrawInitialDivider(RoomLine firstRoomLine, Polyline outlinePure)
        {
            DividingOrigin originInitial = new DividingOrigin(firstRoomLine.Liner.PointAt(0), firstRoomLine);

            Line lineInitial = PCXTools.ExtendFromPt(originInitial.Point, outlinePure, originInitial.BaseLine.UnitNormal);
            List<RoomLine> lineInitialLabeled = new List<RoomLine> { new RoomLine(lineInitial, LineType.Inner) };
            DividingLine dividerInitial = new DividingLine(lineInitialLabeled, originInitial);

            return dividerInitial;
        }


    }

}