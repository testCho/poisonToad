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
        public static List<Polyline> MakeRoom(List<double> roomAreaSet, Polyline outline, Core core)
        {
            List<Polyline> rooms = new List<Polyline>();

            List<Polyline> corridor = CorridorMaker.MakeCorridor(outline, core);
            
            /*for proto*/
            if (corridor == null)
            {
                rooms.Add(outline);
                return rooms;
            }
            /*for proto*/

            LabeledOutline outlineLabel = Labeler.GetOutlineLabel(outline, core, corridor);
            rooms = PartitionMaker.DrawPartitions(outlineLabel, roomAreaSet);

            return rooms;
        }       
    }
}
