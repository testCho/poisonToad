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
        //property
        public Floor ParentFloor { get; set; }


        //main method
        public Floor Build()
        {
            List<LabeledOutline> outlineLabels = Labeler.GetOutlineLabel(ParentFloor.Outline, ParentFloor.Cores.First(), ParentFloor.Corridors);
            List<List<Room>> allocatedRooms = RoomAllocator.AllocateToOutlineParts(ParentFloor.Rooms, outlineLabels);

            RoomP1Builder builder = new RoomP1Builder();
            builder.OutlineLabels = outlineLabels;
            builder.AllocatedRoom = allocatedRooms;

            ParentFloor.Rooms = builder.Build();

            return ParentFloor;
        }
    }
}
