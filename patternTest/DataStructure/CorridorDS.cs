using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Core
    {
        //constructor
        public Core(Polyline coreLine, Polyline landing, Vector3d upstairDirec)
        {
            CoreLine = coreLine;
            Landing = landing;
            UpstairDirec = upstairDirec;
        }

        //property
        public Polyline CoreLine { get; private set; }
        public Polyline Landing { get; private set; }
        public Vector3d UpstairDirec { get; private set; }
    }

    class Corridor
    {
        //field
        private static double scale = 1;
        private static double ONE_WAY_CORRIDOR_WIDTH = 1200;
        private static double TWO_WAY_CORRIDOR_WIDTH = 1200;
        private static double MINIMUM_ROOM_WIDTH = 1500;
        private static double MINIMUM_CORRIDOR_LENGTH = 1200; //임시

        //method

        //property
        public static double MinRoomWidth { get { return MINIMUM_ROOM_WIDTH / scale; } private set { } }
        public static double OneWayWidth { get { return ONE_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double TwoWayWidth { get { return TWO_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double MinLengthForDoor { get { return MINIMUM_CORRIDOR_LENGTH / scale; } private set { } }
    }


    //interface
    interface ICorridorPattern
    {
        List<Polyline> GetCorridor(Line baseLine, List<Line> mainAxis, Core core, Polyline outline, List<double> lengthFactors);
        List<double> GetInitialLengthFactors();
    }

    interface ICorridorDecider
    {
        ICorridorPattern GetCorridorPattern(Polyline outline, Core core, List<Line> baseAxis, List<Line> subAxis);
    }

}
