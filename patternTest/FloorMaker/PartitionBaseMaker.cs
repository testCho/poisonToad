using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PartitionBaseMaker
    {
        //main method
        public static PartitionOrigin FindStartPt(List<RoomLine> coreSeg, PartitionOrigin previousEnd)
        {
            double dotTolerance = 0.005;
            if (previousEnd.Type == LineType.Corridor)
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

        public static RoomLine FindEndPtBase(List<RoomLine> coreSeg, PartitionOrigin startPt)
        {
            if (startPt.Type != LineType.Corridor)
                return FindEndFromCore(coreSeg, startPt);
            else
                return FindEndFromCorridor(coreSeg, startPt);
        }


        //method
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

        private static RoomLine FindEndFromCore(List<RoomLine> coreSeg, PartitionOrigin startPt)
        { return FindNearestCorridor(coreSeg, startPt.BaseLine); }

        private static RoomLine FindEndFromCorridor(List<RoomLine> coreSeg, PartitionOrigin startPt)
        {
            if (!IsCorner(startPt))
                return startPt.BaseLine;

            if (!IsStartPt(startPt))
                return startPt.BaseLine;

            return FindEndFromEndPt(coreSeg, startPt);
        }

        private static Boolean IsCorner(PartitionOrigin testOrigin)
        {
            if (IsEndPt(testOrigin))
                return true;
            if (IsStartPt(testOrigin))
                return true;

            return false;
        }

        private static RoomLine FindEndFromEndPt(List<RoomLine> coreSeg, PartitionOrigin testOrigin)
        {
            int testIndex = coreSeg.FindIndex(i => (i.Liner == testOrigin.Liner));

            RoomLine formerCornerLine = coreSeg[testIndex];
            RoomLine laterCornerLine = coreSeg[testIndex + 1];

            Point3d laterStartPt = laterCornerLine.Liner.PointAt(0);
            Point3d laterEndPt = laterCornerLine.Liner.PointAt(1);

            CornerState cornerStat = GetCCWCornerState(formerCornerLine.UnitTangent, laterCornerLine.UnitTangent);

            if (cornerStat == CornerState.Concave)
                return FindEndPtBase(coreSeg, new PartitionOrigin(laterEndPt, laterCornerLine));

            if (cornerStat == CornerState.Convex)
                return FindEndPtBase(coreSeg, new PartitionOrigin(laterStartPt, laterCornerLine));

            return FindEndPtBase(coreSeg, new PartitionOrigin(laterStartPt, laterCornerLine));
        }

        private static Boolean IsStartPt(PartitionOrigin testOrigin)
        {
            if (testOrigin.Origin == testOrigin.Liner.PointAt(0))
                return true;
            return false;
        }

        private static Boolean IsEndPt(PartitionOrigin testOrigin)
        {
            if (testOrigin.Origin == testOrigin.Liner.PointAt(0))
                return true;
            return false;
        }

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


        //enum
        private enum CornerState { Convex, Concave, Straight };
    }
}
