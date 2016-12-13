using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class FloorMaker
    {
        //main method
        public static List<Polyline> MakeFloor(List<double> roomAreaSet, Polyline outline, Core core)
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

            List<LabeledOutline> outlineLabel = Labeler.GetOutlineLabel(outline, core, corridor);
            List<List<double>> distributedAreaSet = DistributeArea(roomAreaSet, outlineLabel);
            rooms = RoomMaker.DrawRooms(outlineLabel, distributedAreaSet);

            return rooms;
        }

        //method
        private static List<List<double>> DistributeArea(List<double> areaSet, List<LabeledOutline> outlineLabel)
        {
            List<List<double>> distributedArea = new List<List<double>>();
            List<double> outlineArea = new List<double>();

            foreach (LabeledOutline i in outlineLabel)            
                outlineArea.Add(i.DifferenceArea);

            if (areaSet.Count <= outlineArea.Count)
            {
                for (int i = 0; i < outlineArea.Count; i++)       
                    distributedArea.Add(new List<double> {outlineArea[i]});

                return distributedArea;
            }

            distributedArea = DoubleTools.BinPacker.PackToBins(areaSet, outlineArea);
            List<List<double>> adjustedArea = new List<List<double>>();

            for (int i = 0; i < distributedArea.Count; i++)
                adjustedArea.Add(DoubleTools.ScaleToNewSum(outlineArea[i], distributedArea[i]));

            return adjustedArea;
        }

    }
}
