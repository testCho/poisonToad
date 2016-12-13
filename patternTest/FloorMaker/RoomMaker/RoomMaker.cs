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
        //변수명 정리 요
        //코어가 가운데 있을 경우 Outline 넓이 수정..!

        //main method
        public static List<Polyline> DrawRooms(List<LabeledOutline> outlineLabel, List<List<double>> distributedArea)
        {
            List<Polyline> rooms = new List<Polyline>();

            for (int i = 0; i < outlineLabel.Count; i++)           
                rooms.AddRange(DrawRoomsAtEachDifference(outlineLabel[i], distributedArea[i]));

            return rooms;
        }

        private static List<Polyline> DrawRoomsAtEachDifference(LabeledOutline outlineLabel, List<double> roomAreas)
        {
            List<Polyline> partitionList = new List<Polyline>();

            double remainedArea = outlineLabel.DifferenceArea;
            double remainTolerance = 0.005;


            //draw initial
            Partition dividerInitial = SetInitialDivider(outlineLabel);
            PartitionParam paramInitial = new PartitionParam(dividerInitial, dividerInitial.Origin, outlineLabel);


            //draw middle
            for (int i = 0; i < roomAreas.Count; i++)
            {
                if(i == roomAreas.Count-1)
                {
                    PartitionParam lastParam = new PartitionParam(paramInitial);
                    lastParam.OriginPost.BaseLine = paramInitial.OutlineLabel.Core.Last();
                    lastParam.OriginPost.Point = paramInitial.OutlineLabel.Core.Last().EndPt;
                     
                    if (IsCrossed(lastParam))
                    {
                        lastParam.OriginPost.BaseLine = paramInitial.OutlineLabel.Core[paramInitial.OutlineLabel.Core.Count - 2];
                        lastParam.OriginPost.Point = lastParam.OriginPost.BaseLine.EndPt;
                    }

                    DivMakerOutput lastPartition = PartitionMaker.DrawOrtho(lastParam);
                    partitionList.Add(lastPartition.Poly);
                    break;
                }

                DivMakerOutput eachPartition = PartitionMaker.DrawEachPartition(paramInitial, roomAreas[i]);
                partitionList.Add(eachPartition.Poly);

                remainedArea -= PolylineTools.GetArea(eachPartition.Poly);

                eachPartition.DivParams.PostToPre();
                paramInitial = eachPartition.DivParams;

                if (remainedArea <= remainTolerance)
                    break;   
            }

            return partitionList;
        }

        //method
        private static Partition SetInitialDivider(LabeledOutline outlineLabel)
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

        private static Partition DrawInitialDivider(RoomLine firstRoomLine, Polyline outlinePure)
        {
            PartitionOrigin originInitial = new PartitionOrigin(firstRoomLine.PureLine.PointAt(0), firstRoomLine);

            Line lineInitial = PCXTools.PCXByEquation(originInitial.Point, outlinePure, originInitial.BaseLine.UnitNormal);
            List<RoomLine> lineInitialLabeled = new List<RoomLine> { new RoomLine(lineInitial, LineType.Inner) };
            Partition dividerInitial = new Partition(lineInitialLabeled, originInitial);

            return dividerInitial;
        }

        private static Boolean IsCrossed(PartitionParam setterParam)
        {
            Partition dividerTest = setterParam.PartitionPre;
            PartitionOrigin originTest = setterParam.OriginPost;

            Vector3d normal = originTest.BaseLine.UnitNormal;
            Polyline trimmed = setterParam.OutlineLabel.Difference;
            double coverAllLength = new BoundingBox(new List<Point3d>(trimmed)).Diagonal.Length * 2;
            Line testLine = new Line(originTest.Point, originTest.Point + normal * coverAllLength);

            foreach (RoomLine i in dividerTest.Lines)
            {
                if (i.UnitTangent == normal)
                    continue;

                Point3d crossPt = CCXTools.GetCrossPt(testLine, i.PureLine);

                if (PCXTools.IsPtOnLine(crossPt, i.PureLine, 0.005) && PCXTools.IsPtOnLine(crossPt, testLine, 0.005))
                    return true;
            }

            return false;
        }


    }

}