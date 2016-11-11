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
        public static List<List<RoomLine>> DrawPartitions(RefinedOutline refinedOuter, List<double> roomAreas)
        {
            List<List<RoomLine>> partitionList = new List<List<RoomLine>>();
            List<double> redistribAreas = NumberTools.ScaleToNewSum(PolylineTools.GetArea(refinedOuter.Outline), roomAreas);
            DividingLine initialDivider = SetInitialDivider(refinedOuter);

            for (int i=0; i<redistribAreas.Count; i++)
            {
                DividingLine nextDivider = new DividingLine();
                List<RoomLine> eachPartition = DrawEachPartition(initialDivider, out nextDivider); //자체적으로도 재귀호출 해야함..
                partitionList.Add(eachPartition);
                initialDivider = nextDivider;
            }
            
            return partitionList;
        }

        public DividingLine SetInitialDivider(RefinedOutline refinedOuter)
        {
            DividingLine initialDivider = new DividingLine();

            double dotTolerance = 0.005;
            if (refinedOuter.LabeledCore[0].Type == LineType.Corridor)
                return DrawInitialDivider(refinedOuter.LabeledCore[0]);

            RoomLine nearestCorridor = PartitionBaseMaker.FindNearestCorridor(refinedOuter.LabeledCore, refinedOuter.LabeledCore[0]);
            bool decider = Math.Abs(Vector3d.Multiply(nearestCorridor.UnitTangent, previousEnd.BaseLine.UnitTangent)) < dotTolerance;

            if (decider)
                return DrawInitialDivider(refinedOuter.LabeledCore[0]);
            else
            {
               //pointAt(0)이 시작점이 안 될 수 있음(ccw에서는 됨)..=> 고친다면 논리로
                return DrawInitialDivider(nearestCorridor);
            }

            return initialDivider;
        }

        public DividingLine(RoomLine baseLine)
        { }
}