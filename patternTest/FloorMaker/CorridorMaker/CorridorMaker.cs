using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class CorridorMaker
    {
        //field
        private ICorridorDecider testDecider = new CorridorDeciderForTest();

        //property
        public ICorridorPattern Pattern { get; set; }
        public Core Core { get; set; }
        public Polyline Outline { get; set;}


        //constructor
        public CorridorMaker(Polyline outline, Core core)
        {
            this.Outline = outline;
            this.Core = core;
        }
        
        public CorridorMaker()
        { }


        //main method
        public List<Polyline> Make()
        {
            /*for proto*/
            if (Pattern == null)
                return null;

            List<List<Line>> axisSet = CorridorAxisMaker.MakeAxis(Outline, Core);
            List<Polyline> corridor = Pattern.Draw(axisSet[0], Core, Outline);
            
            return corridor;
        }

        public ICorridorPattern RecommandPattern()
        {
            return testDecider.GetPattern(Outline, Core);
        }
    }


}
