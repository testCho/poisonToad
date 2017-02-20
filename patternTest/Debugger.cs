using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.CorridorPatterns;
using SmallHousing.RoomPatterns;
using SmallHousing.Utility;

namespace SmallHousing
{
    class Debugger
    {
        
        private static List<double> roomArea = new List<double> { 30, 60, 60 };

        //main
        public static List<Polyline> DebugRoom(Polyline outline, Polyline core, Polyline landing)
        {
            Floor debugFloor = new Floor();

            foreach (double area in roomArea)
            {
                Room temp = new Room(area);
                debugFloor.Rooms.Add(temp);
            }

            debugFloor.Cores.Add(new Core(core, landing));
            debugFloor.Outline = outline;

            CorridorP1 testMaker2 = new CorridorP1();
            testMaker2.ParentFloor = new Floor(debugFloor);

            RoomP1 testMaker = new RoomP1();
            testMaker.ParentFloor = testMaker2.Build();

            debugFloor = testMaker.Build();

            List<Polyline> result = new List<Polyline>();
            debugFloor.Rooms.ForEach(n => result.Add(n.Outline));

            return result;
        }     

    }
}
