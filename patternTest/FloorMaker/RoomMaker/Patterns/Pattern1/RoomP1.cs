using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class RoomP1:IRoomPattern
    {
        //field
        private string name = "코어 기준 회전형";
        private List<double> factors = new List<double> { 0.75,0.5};
        private List<string> factorName = new List<string> {"방 최소 면적효율","방 최소 종횡비"};


        //property
        public string Name { get { return name; } private set { } }
        public List<string> ParamName { get { return factorName; } private set { } }
        public List<double> Param{ get { return factors; } set { factors = value as List<double>; }}

        
        //main method
        public List<Polyline> Draw(List<LabeledOutline> outlinelabels, List<List<double>> distributedArea)
        {
            List<Polyline> rooms = new List<Polyline>();

            for (int i = 0; i < outlinelabels.Count; i++)
                rooms.AddRange(DrawRoomsAtEachDifference(outlinelabels[i], distributedArea[i]));

            return rooms;
        }

        private List<Polyline> DrawRoomsAtEachDifference(LabeledOutline outlineLabel, List<double> roomAreas)
        {
            List<Polyline> partitionList = new List<Polyline>();

            double remainedArea = outlineLabel.DifferenceArea;
            double remainTolerance = 0.005;


            //draw initial
            Partition dividerInitial = SetInitialDivider(outlineLabel);
            PartitionParam paramInitial = new PartitionParam(dividerInitial, dividerInitial.Origin, outlineLabel);
            PartitionParam paramPre = new PartitionParam(paramInitial);


            //draw middle
            for (int i = 0; i < roomAreas.Count; i++)
            {
                if (i == roomAreas.Count - 1)
                {
                    DivMakerOutput lastPartition = PartitionMakerLast.Draw(paramPre, paramInitial);
                    partitionList.Add(lastPartition.Poly);
                    return partitionList;
                }

                DivMakerOutput eachPartition = PartitionMaker.DrawEachPartition(paramPre, roomAreas[i]);
                partitionList.Add(eachPartition.Poly);

                remainedArea -= PolylineTools.GetArea(eachPartition.Poly);

                eachPartition.DivParams.PostToPre();
                paramPre = eachPartition.DivParams;

                if (remainedArea <= remainTolerance)
                    break;
            }

            return partitionList;
        }


        //method
        private Partition SetInitialDivider(LabeledOutline outlineLabel)
        {
            RoomLine firstRoomLine = outlineLabel.Core[0];

            if (firstRoomLine.Type == LineType.Corridor)
                return DrawInitialDivider(firstRoomLine, outlineLabel.Pure);

            RoomLine nearestCorridor = PartitionSetter.FindNearestCorridor(outlineLabel.Core, firstRoomLine);
            int parallelDecider = firstRoomLine.UnitTangent.IsParallelTo(nearestCorridor.UnitTangent);

            if (parallelDecider == 0) //첫번째 라인과 복도가 평행이 아닌 경우, not parallel
                return DrawInitialDivider(nearestCorridor, outlineLabel.Pure);
            else //평행인 경우
                return DrawInitialDivider(firstRoomLine, outlineLabel.Pure);

        }

        private Partition DrawInitialDivider(RoomLine firstRoomLine, Polyline outlinePure)
        {
            PartitionOrigin originInitial = new PartitionOrigin(firstRoomLine.PureLine.PointAt(0), firstRoomLine);

            Line lineInitial = PCXTools.PCXByEquationStrict(originInitial.Point, outlinePure, originInitial.BaseLine.UnitNormal);
            List<RoomLine> lineInitialLabeled = new List<RoomLine> { new RoomLine(lineInitial, LineType.Inner) };
            Partition dividerInitial = new Partition(lineInitialLabeled, originInitial);

            return dividerInitial;
        }


    }
}
