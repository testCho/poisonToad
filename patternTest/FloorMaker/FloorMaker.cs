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
        private List<LabeledOutline> outlineLabels;
        private List<List<double>> distributedArea;


        //property
        
        //-pattern
        public ICorridorPattern CorridorPattern { get { return corridorMaker.Pattern; } set { corridorMaker.Pattern = value as ICorridorPattern;}}
        public ICorridorPattern Recomm_CorridorPattern { get { return corridorMaker.RecommandPattern(); } }
        public IRoomPattern RoomPattern { get; set; }
        public IRoomPattern Recomm_RoomPattern { get { return roomMaker.RecommandPattern();}}     
            
        //-input
        public Polyline Outline { get; set; }
        public Core Core { get; set; }
        public List<double> RoomAreaSet { get; set; }
        
        //-output
        public List<Polyline> Corridor { get; private set; }
        public List<Polyline> Room { get; private set; }


        //constructor
        public FloorMaker(Polyline outline, Core core, List<double> roomAreaSet)
        {
            this.corridorMaker = new CorridorMaker();
            this.roomMaker = new RoomMaker();

            this.RoomAreaSet = roomAreaSet;
            this.Outline = outline;
            this.Core = core;            
        }


        //main method
        public void Make()
        {
            DrawCorridor();

            /*for proto*/
            if (Corridor == null)
            {
                this.Room = new List<Polyline> { Outline };
                return;
            }

            LabelAndDistributeArea();
            DrawRoom();
        }


        //method
        private void DrawCorridor()
        {
            corridorMaker.Outline = this.Outline;
            corridorMaker.Core = this.Core;

            if (CorridorPattern == null)
                corridorMaker.Pattern = Recomm_CorridorPattern;

            this.Corridor = corridorMaker.Make();
        }

        private void LabelAndDistributeArea()
        {
            this.outlineLabels = Labeler.GetOutlineLabel(Outline, Core, Corridor);
            this.distributedArea = DistributeArea(RoomAreaSet, outlineLabels);
        }

        private void DrawRoom()
        {
            roomMaker.OutlineLabels = this.outlineLabels;
            roomMaker.TargetArea = this.distributedArea;

            if (RoomPattern == null)
                roomMaker.Pattern = Recomm_RoomPattern;

            this.Room = roomMaker.Make();
        }

        private List<List<double>> DistributeArea(List<double> areaSet, List<LabeledOutline> outlineLabel)
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
