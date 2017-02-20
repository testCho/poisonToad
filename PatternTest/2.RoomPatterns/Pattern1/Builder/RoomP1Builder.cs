using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;
using SmallHousing.Utility;

namespace SmallHousing.RoomPatterns
{
    partial class RoomP1
    {
        partial class RoomP1Builder 
        {
            //property
            public Floor ParentFloor { get; set;}
            public List<LabeledOutline> OutlineLabels { get; set; }
            public List<List<Room>> AllocatedRoom { get; set; }

            //main
            public List<Room> Build()
            {
                List<Room> roomList = new List<Room>();

                if (OutlineLabels.Count == 1 && AllocatedRoom.First().Count == 1) 
                {
                    Room onlyThisRoom = AllocatedRoom[0][0];
                    onlyThisRoom.Outline = OutlineLabels.First().Difference;
                    onlyThisRoom.Area = OutlineLabels.First().Difference.GetArea();
                    roomList.Add(onlyThisRoom);
                }

                else
                    roomList = MakeRooms(OutlineLabels, AllocatedRoom);                

                return roomList;              
            }


            //process method
            private List<Room> MakeRooms(List<LabeledOutline> outlinelabels, List<List<Room>> allocatedRoom)
            {
               
                //draw outline.
                List<Polyline> roomOutlines = new List<Polyline>();
                for (int i = 0; i < outlinelabels.Count; i++)
                {
                    roomOutlines.AddRange(
                        DrawRoomAtEachPart(outlinelabels[i], allocatedRoom[i].Select(n => n.Area).ToList()));
                }

                //merge smallRoom. //tolerance 문제 있을듯
                if( roomOutlines.Count > 1 && roomOutlines.Last().GetArea() < Dimensions.MinRoomArea)
                {
                    Curve lastRoom = roomOutlines.Last().ToNurbsCurve();
                    roomOutlines.RemoveAt(roomOutlines.Count - 1);
                    Curve preLastRoom = roomOutlines.Last().ToNurbsCurve();
                    roomOutlines.RemoveAt(roomOutlines.Count - 1);

                    Curve[] merged = Curve.CreateBooleanUnion(new List<Curve>{ lastRoom, preLastRoom });
                    merged.OrderByDescending(n => n.GetArea());

                    roomOutlines.Add(CurveTools.ToPolyline(merged.First()));
                }

                //assign to RoomClass
                List<Room> rooms = new List<Room>();

                List<Room> outlineLackedRoom = new List<Room>();
                allocatedRoom.ForEach(n => outlineLackedRoom.AddRange(n));

                for (int i = 0; i < roomOutlines.Count; i++)
                {
                    Room currentRoom = outlineLackedRoom[i];
                    currentRoom.Outline = roomOutlines[i];
                    currentRoom.Area = roomOutlines[i].GetArea();
                    rooms.Add(currentRoom);
                }               

                return rooms;
            }

            private List<Polyline> DrawRoomAtEachPart(LabeledOutline outlineLabel, List<double> roomAreas)
            {
                List<Polyline> partitionList = new List<Polyline>();

                double remainedArea = outlineLabel.DifferenceArea;
                double remainTolerance = 0.5;


                //draw initial
                Partition dividerInitial = SetInitialDivider(outlineLabel);
                PartitionParam paramInitial = new PartitionParam(dividerInitial, dividerInitial.Origin, outlineLabel);
                PartitionParam paramPre = new PartitionParam(paramInitial);


                //draw middle
                for (int i = 0; i < roomAreas.Count; i++)
                {
                    if (i == roomAreas.Count - 1)
                    {
                        PartitionParam lastPartition = PartitionMakerLast.Draw(paramPre, paramInitial);
                        partitionList.Add(lastPartition.Outline);
                        return partitionList;
                    }

                    PartitionParam eachPartition = PartitionMaker.DrawEachPartition(paramPre, roomAreas[i]);
                    partitionList.Add(eachPartition.Outline);

                    remainedArea -= eachPartition.Outline.GetArea();

                    eachPartition.PostToPre();
                    paramPre = eachPartition;

                    if (remainedArea <= remainTolerance)
                        break;
                }

                return partitionList;
            }


            //sub method
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
                List<RoomLine> lineInitialLabeled = new List<RoomLine> { new RoomLine(lineInitial, LineType.Inner) }; //외곽선과 겹칠 경우 추가 요
                Partition dividerInitial = new Partition(lineInitialLabeled, originInitial);

                return dividerInitial;
            }
        }
    }
}
