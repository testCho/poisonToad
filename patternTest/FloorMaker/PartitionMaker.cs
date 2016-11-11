using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PartitionMaker
    {
        //method
        public static List<Polyline> DrawPartitions(RefinedOutline refinedOuter, List<double> roomAreas)
        {
            List<Polyline> partitions = new List<Polyline>();
            PartitionOrigin previousPt = new PartitionOrigin();
            List<double> redistribAreas = NumberTools.ScaleToNewSum(PolylineTools.GetArea(refinedOuter.Outline), roomAreas);

            for (int i = 0; i < redistribAreas.Count; i++)
            {
                PartitionOrigin startPt = PartitionBaseMaker.FindStartPt(refinedOuter.LabeledCore, previousPt);
                PartitionOrigin currentEndPt = new PartitionOrigin();
                Polyline tempPartition = DrawEachPartition(refinedOuter, startPt, redistribAreas[i], out currentEndPt);
                partitions.Add(tempPartition);

                previousPt = currentEndPt;
            }

            return partitions;
        }

        private static Polyline DrawEachPartition(RefinedOutline refinedOuter, PartitionOrigin start, double TargetArea, out PartitionOrigin currentEnd)
        {
            Polyline onePartition = new Polyline();
            PartitionOrigin onePartitionEnd = new PartitionOrigin();

            //set endPartition origin
            RoomLine endBase = PartitionBaseMaker.FindEndPtBase(refinedOuter.LabeledCore, start);

            //draw cuttingLine
            currentEnd = onePartitionEnd;
            return onePartition;
        }

    }
}