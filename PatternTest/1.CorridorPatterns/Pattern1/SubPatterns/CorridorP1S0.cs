using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;

namespace SmallHousing.CorridorPatterns
{
    partial class CorridorP1
    {
        class CorridorP1S0 : ICorridorP1Sub
        {
            //field
            private string name = "복도 없음";
            private List<double> lengthFactors = new List<double> {};
            private List<string> factorName = new List<string> {};

            //property
            public string Name { get { return name; } private set { } }
            public List<double> Param
            {
                get { return lengthFactors; }
                set { lengthFactors = value as List<double>; }
            }
            public Floor ParentFloor { get; set;}
            public List<List<Line>> AxisSet { get; set; }
            public P1Core Core { get; set; }
 

            //main
            public List<Polyline> Build()
            {
                List<Polyline> corridors = new List<Polyline>();
                return corridors;               
            }
        }
    }
}
