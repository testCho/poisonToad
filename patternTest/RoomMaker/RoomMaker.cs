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
           
            //여기까지//

            LabeledOutline outlineLabel = Labeler.GetOutlineLabel(outline, core, corridor);
            rooms = PartitionMaker.DrawPartitions(outlineLabel, roomAreaSet);

            return rooms;
        }


        //method
       
    }
}
