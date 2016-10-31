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
            UpstairDirec = UpstairDirec;
        }

        //property
        public Polyline CoreLine { get; private set; }
        public Polyline Landing { get; private set; }
        public Vector3d UpstairDirec{ get; private set; }
    }

    class Corridor
    {
        //field
        private static double scale = 1;
        private static double oneWayCorridorWidth = 1200;
        private static double twoWayCorridorWidth = 1800;
        private static double minRoomWidth = 3000;

        //method

        //property
        public static double MinRoomWidth { get { return minRoomWidth / scale; } private set { } }
        public static double OneWayWidth { get { return oneWayCorridorWidth / scale; } private set { } }
        public static double TwoWayWidth { get { return twoWayCorridorWidth / scale; } private set { } }
    }
}
