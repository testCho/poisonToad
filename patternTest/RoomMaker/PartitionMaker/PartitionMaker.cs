﻿using System;
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
        //코어가 가운데 있을 경우 Outline 넓이 수정..!

        //main method
        public static List<Polyline> DrawPartitions(LabeledOutline outlineLabel, List<double> roomAreas)
        {
            List<Polyline> partitionList = new List<Polyline>();

            double floorArea = PolylineTools.GetArea(outlineLabel.Trimmed);
            if (outlineLabel.Pure.IsClosed)
                floorArea -= PolylineTools.GetArea(outlineLabel.CoreUnion);

            List<double> scaledAreas = NumberTools.ScaleToNewSum(floorArea, roomAreas);
            double remainedArea = floorArea;
            double remainTolerance = 0.005;


            //draw initial
            DividingLine dividerInitial = SetInitialDivider(outlineLabel);
            DividerParams paramInitial = new DividerParams(dividerInitial, dividerInitial.Origin, outlineLabel);


            //draw middle
            for (int i = 0; i < scaledAreas.Count-1; i++)
            {
                DivMakerOutput eachPartition = DividerMaker.DrawEachPartition(paramInitial, scaledAreas[i]);
                partitionList.Add(eachPartition.Poly);

                remainedArea -= PolylineTools.GetArea(eachPartition.Poly);

                eachPartition.DivParams.PostToPre();
                paramInitial = eachPartition.DivParams;

                if (remainedArea <= remainTolerance)
                    break;   
            }


            //draw last
            
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

        private static DividingLine DrawInitialDivider(RoomLine firstRoomLine, Polyline outlinePure)
        {
            DividingOrigin originInitial = new DividingOrigin(firstRoomLine.Liner.PointAt(0), firstRoomLine);

            Line lineInitial = PCXTools.PCXByEquation(originInitial.Point, outlinePure, originInitial.BaseLine.UnitNormal);
            List<RoomLine> lineInitialLabeled = new List<RoomLine> { new RoomLine(lineInitial, LineType.Inner) };
            DividingLine dividerInitial = new DividingLine(lineInitialLabeled, originInitial);

            return dividerInitial;
        }


    }

}