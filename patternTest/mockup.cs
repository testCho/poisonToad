using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;


namespace patternTest
{
    interface IRoomMakerPattern
    {
        List<Polyline> DrawRooms();
    }

    interface IRoomMakerDecider
    {
        IRoomMakerPattern GetRoomMaker();
    }

    class Room_LinearSweeper : IRoomMakerPattern
    {
        public List<Polyline> DrawRooms()
        {
            List<Polyline> rooms = new List<Polyline>();
            return rooms;
        }
    }

    
}
