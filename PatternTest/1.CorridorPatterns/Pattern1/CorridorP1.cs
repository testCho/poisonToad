using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;


namespace SmallHousing.CorridorPatterns
{
    partial class CorridorP1
    {
        public Floor ParentFloor { get; set; }

        public Floor Build()
        {
            P1Core refinedCore = new P1Core(ParentFloor.Cores.First());
            List<List<Line>> axis = AxisMaker.Make(ParentFloor.Outline, refinedCore);
            List<double> areaSet = ParentFloor.Rooms.Select(n => n.Area).ToList();

            ICorridorP1Sub recommended = SubPatternDecider.GetPattern(ParentFloor.Outline, refinedCore, areaSet);
            recommended.ParentFloor = ParentFloor;
            recommended.Core = refinedCore;
            recommended.AxisSet = axis;

            ParentFloor.Corridors = recommended.Build();

            return ParentFloor;
        }
    }
}
