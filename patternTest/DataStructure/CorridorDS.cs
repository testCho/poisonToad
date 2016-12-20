using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    public class Core
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

    public class CorridorDimension
    {
        //field
        private static double scale = 1;
        private static double ONE_WAY_CORRIDOR_WIDTH = 1200;
        private static double TWO_WAY_CORRIDOR_WIDTH = 1800;
        private static double MINIMUM_ROOM_WIDTH = 3000;
        private static double MINIMUM_CORRIDOR_LENGTH = 900; //임시

        //method

        //property
        public static double MinRoomWidth { get { return MINIMUM_ROOM_WIDTH / scale; } private set { } }
        public static double OneWayWidth { get { return ONE_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double TwoWayWidth { get { return TWO_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double MinLengthForDoor { get { return MINIMUM_CORRIDOR_LENGTH / scale; } private set { } }
    }


    //interface
    public interface ICorridorPattern
    {
        string Name { get; }

        List<Polyline> Draw(Line baseLine, List<Line> mainAxis, Core core, Polyline outline);
        List<double> Param { get; set; }
        List<string> ParamName { get; }        
        
    }

    public interface ICorridorDecider
    {
        ICorridorPattern GetPattern(Core core, Polyline outline);
    }

}
