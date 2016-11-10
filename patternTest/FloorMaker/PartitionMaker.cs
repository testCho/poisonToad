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
        //inner class
        //파티션 시작점 + 기반 라인 + 타입 클래스
        private class PartitionOrigin
        {
            public PartitionOrigin(Point3d origin, RoomLine baseLine)
            {
                this.Origin = origin;
                this.BaseLine = baseLine;
            }

            public PartitionOrigin()
            { }

            public PartitionOrigin(PartitionOrigin partitionOrigin)
            {
                this.Origin = partitionOrigin.Origin;
                this.BaseLine = partitionOrigin.BaseLine;
            }

            //property
            public Point3d Origin { get; set; }
            public RoomLine BaseLine { get; set; }
            public LineType type { get { return BaseLine.Type; } private set { } }
            public Line Liner { get { return BaseLine.Liner; } private set { } }
        }

        //method
        public static List<Polyline> DrawPartitions(RefinedOutline refinedOuter, List<double> roomAreas)
        {
            List<Polyline> partitions = new List<Polyline>();
            PartitionOrigin previousPt = new PartitionOrigin();
            List<double> redistribAreas = NumberTools.ScaleToNewSum(PolylineTools.GetArea(refinedOuter.Outline), roomAreas);

            for (int i = 0; i < redistribAreas.Count; i++)
            {
                PartitionOrigin startPt = FindStartPt(refinedOuter.LabeledCore, previousPt);
                PartitionOrigin currentEndPt = new PartitionOrigin();
                Polyline tempPartition = DrawEachPartition(refinedOuter, startPt, redistribAreas[i], out currentEndPt);
                partitions.Add(tempPartition);

                previousPt = currentEndPt;
            }

            return partitions;
        }

        private static PartitionOrigin FindStartPt(List<RoomLine> coreSeg, PartitionOrigin previousEnd)
        {
            double dotTolerance = 0.005;
            if (previousEnd.type == LineType.Corridor)
                return previousEnd;

            RoomLine nearestCorridor = FindNearestCorridor(coreSeg, previousEnd.BaseLine);
            bool decider = Math.Abs(Vector3d.Multiply(nearestCorridor.UnitTangent, previousEnd.BaseLine.UnitTangent)) < dotTolerance;

            if (decider)
                return previousEnd;
            else
            {
                PartitionOrigin adjustedStart = new PartitionOrigin(nearestCorridor.Liner.PointAt(0), nearestCorridor); //pointAt(0)이 시작점이 안 될 수 있음(ccw에서는 됨)..=> 고친다면 논리로
                return adjustedStart;
            }
        }

        private static RoomLine FindNearestCorridor(List<RoomLine> coreSeg, RoomLine baseLine)
        {
            RoomLine nearestCorridor = new RoomLine();
            int baseIndex = coreSeg.FindIndex(i => (i.Liner == baseLine.Liner));//어쩌면 못 찾을수도

            for (int i = baseIndex; i < coreSeg.Count; i++)
            {
                if (coreSeg[i].Type == LineType.Corridor)
                {
                    nearestCorridor = coreSeg[i];
                    break;
                }
            }

            return nearestCorridor;
        }

        private static Polyline DrawEachPartition(RefinedOutline refinedOuter, PartitionOrigin start, double TargetArea, out PartitionOrigin currentEnd)
        {
            Polyline onePartition = new Polyline();
            PartitionOrigin onePartitionEnd = new PartitionOrigin();

            //set endPt base
            RoomLine endBase = new RoomLine();

            if (start.type != LineType.Corridor)
                endBase = FindNearestCorridor(refinedOuter.LabeledCore, start.BaseLine); //뒷줄에 코어라인밖에 없는 경우는?
            else if (IsCorner(start))//여기 수정중..
                endBase = start.BaseLine;
            else
                endBase = start.BaseLine;

            currentEnd = onePartitionEnd;
            return onePartition;
        }

        private static Boolean IsCorner(PartitionOrigin testOrigin)
        {
            if (IsEndPt(testOrigin))
                return true;
            if (IsStartPt(testOrigin))
                return true;

            return false;
        }

        private static RoomLine FindThisCorridorBase (List<RoomLine> coreSeg, PartitionOrigin testOrigin)
        {
            Point3d origin = testOrigin.Origin;
            Vector3d formerCornerLine = new Vector3d();
            Vector3d laterCornerLine = new Vector3d();
            int testIndex = coreSeg.FindIndex(i => (i.Liner == testOrigin.Liner));

            if (IsStartPt(testOrigin))
                return testOrigin.BaseLine;

            if(IsEndPt(testOrigin)) //진짜 끝일경우는??
            {
                formerCornerLine = coreSeg[testIndex].UnitTangent;
                laterCornerLine = coreSeg[testIndex + 1].UnitTangent;
                CornerState cornerStat = GetCCWCornerState(formerCornerLine, laterCornerLine);

                if(cornerStat == CornerState.Concave)
                    return 

                if(cornerStat == CornerState.Convex)
                { }

                if(cornerStat == CornerState.Straight)
                { }
            }

        }
        
        private static Boolean IsEndPt(PartitionOrigin testOrigin)
        {
            if (testOrigin.Origin == testOrigin.Liner.PointAt(1))
                return true;
            return false;
        }

        private static Boolean IsStartPt(PartitionOrigin testOrigin)
        {
            if (testOrigin.Origin == testOrigin.Liner.PointAt(0))
                return true;
            return false;
        }

        private enum CornerState {Convex, Concave, Straight};
        private static CornerState GetCCWCornerState(Vector3d formerCornerLine, Vector3d laterCornerLine)
        {
            formerCornerLine.Unitize();
            laterCornerLine.Unitize();
            Vector3d crossProduct = Vector3d.CrossProduct(formerCornerLine, laterCornerLine);
            double dotProductWithZ = Vector3d.Multiply(crossProduct, Vector3d.ZAxis);
            double tolerance = 0.005;

            if (dotProductWithZ > tolerance)
                return CornerState.Concave;
            if (dotProductWithZ < -tolerance)
                return CornerState.Convex;
            return CornerState.Straight;
        }
    }
}
        