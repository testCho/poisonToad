using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Debugger
    {
        public static List<Polyline> DebugRoom(Polyline outline, Polyline coreLine, Polyline landing)
        {
            Core core = new Core(coreLine, landing);
            List<double> areaSet = new List<double> { 45, 60,60};

            FloorMaker testMaker = new FloorMaker(outline, core, areaSet);
            testMaker.Make();

            List<Polyline> result= testMaker.Room;

            return result;
        }

        public static List<Polyline> DebugCorridor(Polyline outline, Polyline coreLine, Polyline landing)
        {
            Core core = new Core(coreLine, landing);

            CorridorMaker testMaker2 = new CorridorMaker(outline, core);
            List<Polyline> corridor = testMaker2.Make();

            return corridor;
            
        }
    }
}
