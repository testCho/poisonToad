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
        //field
        private CorridorMaker corridorMaker;
        private RoomMaker roomMaker;

        //property
        public ICorridorPattern CorridorPattern
        { get { return corridorMaker.Pattern; } set { corridorMaker.Pattern = value as ICorridorPattern;}}

        public ICorridorPattern Recomm_CorridorPattern { get { return corridorMaker.RecommandPattern(); } }

        public IRoomPattern RoomPattern { get; set; }
        public IRoomPattern Recomm_RoomPattern { get;}

        public Polyline Outline { get; set; }
        public Core Core { get; set; }
        public List<double> RoomAreaSet { get; set; }


        //constructor
        public FloorMaker(Polyline outline, Core core, List<double> roomAreaSet)
        {
            this.corridorMaker = new CorridorMaker(outline, core);
            this.RoomAreaSet = roomAreaSet;
        }


        //main method
        public List<Polyline> Make()
        {
            if (CorridorPattern == null)
                corridorMaker.Pattern = Recomm_CorridorPattern;

            List<Polyline> corridor = corridorMaker.Make();

            List<Polyline> rooms = new List<Polyline>();

            /*for proto*/
            if (corridor == null)
            {
                rooms.Add(Outline);
                return rooms;
            }
            /*for proto*/

            List<LabeledOutline> outlineLabel = Labeler.GetOutlineLabel(Outline, Core, corridor);
            List<List<double>> distributedAreaSet = DistributeArea(RoomAreaSet, outlineLabel);
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
