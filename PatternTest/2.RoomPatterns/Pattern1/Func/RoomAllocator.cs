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
        class RoomAllocator
        {
            public static List<List<Room>> AllocateToOutlineParts(List<Room> rooms, List<LabeledOutline> outlineLabel)
            {
                //bin setting
                List<double> outlinePartAreas = new List<double>();
                foreach (LabeledOutline i in outlineLabel)
                    outlinePartAreas.Add(i.DifferenceArea);

                //stuff setting
                List<double> roomAreas = new List<double>();
                foreach (Room i in rooms)
                    roomAreas.Add(i.Area);


                //bin packing
                List<List<Room>> allocatedRoom = new List<List<Room>>();

                List<List<double>> allocatedAreas = new List<List<double>>();
                List<List<int>> allocatingIndex = new List<List<int>>();               


                allocatedAreas = DoubleTools.BinPacker.PackToBins(roomAreas, outlinePartAreas, out allocatingIndex);

                //adjusting
                List<List<double>> adjustedArea = new List<List<double>>();
                for (int i = 0; i < allocatedAreas.Count; i++)
                    adjustedArea.Add(DoubleTools.ScaleToNewSum(outlinePartAreas[i], allocatedAreas[i]));


                //roomRenewal
                for (int i = 0; i < allocatingIndex.Count; i++)
                {
                    List<Room> allocatedAtPart = new List<Room>();

                    for (int j=0; j< allocatingIndex[i].Count; j++)
                    {
                        Room thisIndexRoom = rooms[j];
                        thisIndexRoom.Area = adjustedArea[i][j];
                        allocatedAtPart.Add(thisIndexRoom);
                    }

                    allocatedRoom.Add(allocatedAtPart);
                }

                return allocatedRoom;
            }
        }
    }
}
