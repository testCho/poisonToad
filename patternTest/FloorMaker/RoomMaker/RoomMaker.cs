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
        //property
        public IRoomPattern Pattern { get; set;}
        public List<LabeledOutline> OutlineLabels { get; set; }
        public List<List<double>> TargetArea {get; set;}


        //constructor
        public RoomMaker(List<LabeledOutline> outlineLabels, List<List<double>> distrubutedArea)
        {
            this.OutlineLabels = outlineLabels;
            this.TargetArea = distrubutedArea;
        }

        public RoomMaker()
        { }


        //main method
        public List<Polyline> Make()
        {
            List<Polyline> room = Pattern.Draw(OutlineLabels,TargetArea);
            return room;
        }

        public IRoomPattern RecommandPattern() { return new RoomP1();}
    }

}