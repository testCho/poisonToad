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
            Vector3d upDirec = Test.MakeUpDirec(coreLine, landing);
            Core core = new Core(coreLine, landing, upDirec);
            List<double> areaSet = new List<double> { 30, 30, 60 };

            List<Polyline> result = RoomMaker.MakeRoom(areaSet, outline, core);

            return result;
        }

        public static List<Polyline> DebugCorridor(Polyline outline, Polyline coreLine, Polyline landing)
        {

            Vector3d upDirec = Test.MakeUpDirec(coreLine, landing);
            Core core = new Core(coreLine, landing, upDirec);

            List<Polyline> corridor = CorridorMaker.MakeCorridor(outline, core);

            return corridor;
            
        }
    }
}
