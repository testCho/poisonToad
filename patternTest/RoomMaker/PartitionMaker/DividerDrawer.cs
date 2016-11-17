using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;


namespace patternTest
{
    class DividerDrawer
    {
        //main method
        public static Polyline DrawEachPartition(DividerParams dividerParam, double targetArea, out DividerParams dividerParamNext)
        {
            Polyline partition = new Polyline();
            DividerParams tempDividerParam = new DividerParams(dividerParam);

            DividerSetter.SetNextDivOrigin(tempDividerParam);

            dividerParamNext = tempDividerParam;
            return partition;
        }


        //method
        private static Boolean IsOverlap(DividingOrigin testPt, LabeledOutline testLabel)
        {
            return false;
        }

        private static Boolean HasAvailableCorridor(DividerParams dividerParam)
        {
            return false;
        }

        private static Polyline GetPartitionOutline()

        
    }
}