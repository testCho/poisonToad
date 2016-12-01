using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;


namespace patternTest
{
    class DividerMaker
    {
        //main method
        public static DivMakerOutput DrawEachPartition(DividerParams param, double targetArea)
        {
            DividerParams tempParam = new DividerParams(param);
            DividerSetter.SetNextDivOrigin(tempParam);

            if (IsOverlap(tempParam))
                MoveOriginProperly(tempParam);

            if (IsOriginEndConcave(tempParam))
                return DrawAtConcaveCorner(tempParam, targetArea);

            return DrawAtThisBaseEnd(tempParam, targetArea);
        }


        //sub method
        private static DivMakerOutput DrawAtConcaveCorner(DividerParams param, double targetArea)
        {
            if (param.OriginPost.Point == param.OriginPost.BaseLine.EndPt)
            {
                SetPostStartToOrigin(param);
                return DrawEachPartition(param, targetArea);
            }

            DividerParams thisEndParam = new DividerParams(param);
            thisEndParam.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
            DivMakerOutput thisEndOutput = DrawOrtho(thisEndParam);

            if (HasEnoughArea(thisEndOutput, targetArea))
            {
                DividerParams tempOutput1 = new DividerParams(param);
                DividerParams tempOutput2 = new DividerParams(param);
                SetPostStartToOrigin(tempOutput2);
                List<DivMakerOutput> outputCandidate = new List<DivMakerOutput>();
                outputCandidate.Add(DrawAtStraight(tempOutput1, targetArea));
                outputCandidate.Add(DrawEachPartition(tempOutput2, targetArea));

                return SelectBetterDivider(outputCandidate, targetArea);
            }

            //SetPostParallelToOrigin(param);
            SetPostStartToOrigin(param);
            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput DrawAtThisBaseEnd(DividerParams param, double targetArea)
        {
            DividerParams thisEndParam = new DividerParams(param);
            thisEndParam.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
            DivMakerOutput thisEndOutput = DrawOrtho(thisEndParam);

            if (HasEnoughArea(thisEndOutput, targetArea))
            {
                if (thisEndParam.OriginPost.Point == param.OriginPost.Point)
                    return thisEndOutput;

                return DrawAtStraight(param, targetArea);
            }
            

            if (IsLastSegment(param))
                return thisEndOutput;

            return DrawAtConvexCorner(param, targetArea);
        }

        private static DivMakerOutput DrawAtStraight(DividerParams param, double targetArea)
        { 

            DivMakerOutput binaryOutput = DrawByBinarySearch(param, targetArea);

            if (IsCloseToEnd(binaryOutput.DivParams))
                return PushOriginToEnd(binaryOutput.DivParams);
            else if (IsCloseToStart(binaryOutput.DivParams))
                return PushOriginToStart(binaryOutput.DivParams);

            return binaryOutput;
        }

        private static DivMakerOutput DrawAtConvexCorner(DividerParams param, double targetArea)
        {
            SetPostStartToOrigin(param);
            DivMakerOutput straightOutput = DrawOrtho(param);

            if (HasEnoughArea(straightOutput, targetArea))
                return TestAtCorner(param, targetArea);

            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput TestAtCorner(DividerParams param, double targetArea)
        {
            DividerParams tempParam1 = new DividerParams(param);
            DivMakerOutput cornerOutput = CornerMaker.GetCorner(tempParam1, targetArea);

            if (IsOriginEndConcave(param))
            {
                List<DivMakerOutput> outputCandidate = new List<DivMakerOutput>();
                DividerParams tempParam2 = new DividerParams(param);
                SetPostStartToOrigin(tempParam2);
                outputCandidate.Add(cornerOutput);
                outputCandidate.Add(DrawEachPartition(tempParam2, targetArea));

                return SelectBetterDivider(outputCandidate, targetArea);
            }

            return cornerOutput;
        }


        //tool method
        private static void MoveOriginProperly(DividerParams param)
        {
            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int originIndex = coreSeg.FindIndex(i => i.Liner == param.OriginPost.Liner);
            int coreSegCount = coreSeg.Count();

            if (originIndex == coreSegCount - 1)
            {
                param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
                return;
            }

            double toEndLength = new Line(param.OriginPost.Point, param.OriginPost.BaseLine.EndPt).Length;

            if (toEndLength<Corridor.MinLengthForDoor)
            {
                param.OriginPost = new DividingOrigin(coreSeg[originIndex + 1].StartPt, coreSeg[originIndex + 1]);
                return;
            }

            param.OriginPost.Point = new Point3d(param.OriginPost.Point + param.OriginPost.BaseLine.UnitTangent * Corridor.MinLengthForDoor);
            return;
        }

        private static void SetPostParallelToOrigin(DividerParams param)
        {
            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int originIndex = coreSeg.FindIndex(i => i.Liner == param.OriginPost.Liner);
            int coreSegCount = coreSeg.Count();

            if (originIndex == coreSegCount - 1)
                return;

            for(int i = originIndex+1; i<coreSegCount; i++)
            {
                if(coreSeg[i].UnitTangent == param.OriginPost.BaseLine.UnitTangent)
                { 
                    param.OriginPost = new DividingOrigin(coreSeg[i].StartPt, coreSeg[i]);
                    return;
                }
            } 
        }

        public static DivMakerOutput DrawOrtho(DividerParams param)
        {
            //Line orthoLine = PCXTools.ExtendFromPt(param.OriginPost.Point, param.OutlineLabel.Pure, param.OriginPost.BaseLine.UnitNormal);
            Line orthoLine = PCXTools.PCXByEquation(param.OriginPost.Point, param.OutlineLabel.Pure, param.OriginPost.BaseLine.UnitNormal);
            List<RoomLine> orthoList = new List<RoomLine>{ new RoomLine(orthoLine, LineType.Inner)};

            DividingLine dividerCurrent = new DividingLine(orthoList, param.OriginPost);

            DividerParams paramNext = new DividerParams(param.DividerPre, dividerCurrent, dividerCurrent.Origin, param.OutlineLabel); 
            Polyline outline = DividerDrawer.GetPartitionOutline(paramNext);

            return new DivMakerOutput(outline, paramNext);
        }

        private static DivMakerOutput DrawByBinarySearch(DividerParams param, double targetArea)
        {
            double skipTolerance = 0.005;
            int iterNum = 10;

            Point3d ptEnd = param.OriginPost.BaseLine.PointAt(1);
            Point3d ptStart = param.OriginPost.Point;


            Vector3d searchVector = new Vector3d(ptEnd - ptStart);
            

            double searchRange = searchVector.Length;
            double upperBound = searchRange;
            double lowerBound = 0;

            if (searchRange < skipTolerance)
                return DrawOrtho(param);

            int loopLimiter = 0;

            while (lowerBound<upperBound)
            {
                if (loopLimiter > iterNum)
                    break;

                double tempStatus = (upperBound - lowerBound) / 2 + lowerBound;
                Point3d tempOriginPt = ptStart + searchVector/searchRange * tempStatus;
                param.OriginPost.Point = tempOriginPt;

                DivMakerOutput tempOutput = DrawOrtho(param);
                double tempArea = PolylineTools.GetArea(tempOutput.Poly);

                if (targetArea > tempArea)
                    lowerBound = tempStatus;
                else if (targetArea < tempArea)
                    upperBound = tempStatus;
                else
                {
                    lowerBound = tempArea;
                    upperBound = tempArea;
                }

                loopLimiter++;
            }

            Point3d newOriginPt = ptStart + searchVector / searchRange * lowerBound;
            param.OriginPost.Point = newOriginPt;

            return DrawOrtho(param);
        }

        private static DivMakerOutput PushOriginToEnd(DividerParams param)
        {

            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int indexCurrent = coreSeg.FindIndex(i => i.Liner == param.OriginPost.Liner);

            if (indexCurrent >= coreSeg.Count - 2) // 사실상 마지막 세그먼트인 경우..
            {
                param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
                return DrawOrtho(param);
            }

            DividerSetter.CornerState cornerStat = 
                DividerSetter.GetCCWCornerState(coreSeg[indexCurrent].UnitTangent, coreSeg[indexCurrent + 1].UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
            {
                DividingOrigin newOrigin = new DividingOrigin(coreSeg[indexCurrent + 2].StartPt, coreSeg[indexCurrent + 2]);
                param.OriginPost = newOrigin;
                return DrawOrtho(param);
            }

            param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
            return DrawOrtho(param);
        }

        private static DivMakerOutput PushOriginToStart(DividerParams param)
        {

            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int indexCurrent = param.OutlineLabel.Core.FindIndex
                (i => i.Liner == param.OriginPost.Liner);

            if (indexCurrent <2) //이 경우는 거의 없을듯..
                return DrawOrtho(param);

            DividerSetter.CornerState cornerStat =
                DividerSetter.GetCCWCornerState(coreSeg[indexCurrent-1].UnitTangent, coreSeg[indexCurrent - 1].UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
            {
                DividingOrigin newOrigin = new DividingOrigin(coreSeg[indexCurrent - 2].EndPt, coreSeg[indexCurrent - 2]);
                param.OriginPost = newOrigin;
                return DrawOrtho(param);
            }

            param.OriginPost.Point = param.OriginPost.BaseLine.StartPt;
            return DrawOrtho(param);

        }

        private static void SetPostStartToOrigin(DividerParams param)
        {
            int currentIndex = param.OutlineLabel.Core.FindIndex
                   (i => (i.Liner == param.OriginPost.Liner));

            if (currentIndex == param.OutlineLabel.Core.Count - 1)
                return;

            RoomLine baseLinePost = param.OutlineLabel.Core[currentIndex + 1];
            param.OriginPost = new DividingOrigin(baseLinePost.StartPt, baseLinePost);

            return;
        }

        private static void SetPreEndToOrigin(DividerParams param)
        {
            int currentIndex = param.OutlineLabel.Core.FindIndex
                   (i => (i.Liner == param.OriginPost.Liner));

            if (currentIndex == 0)
                return;

            RoomLine baseLinePre = param.OutlineLabel.Core[currentIndex - 1];
            param.OriginPost = new DividingOrigin(baseLinePre.EndPt, baseLinePre);

            return;
        }

        private static DivMakerOutput SelectBetterDivider(List<DivMakerOutput> candidate, double targetArea)
        {
            //체 몇개 더 추가..
            candidate.Sort((x, y) => 
            (x.DivParams.DividerPost.GetLength().CompareTo(y.DivParams.DividerPost.GetLength())));

            double length1 = candidate[0].DivParams.DividerPost.GetLength();
            double length2 = candidate[1].DivParams.DividerPost.GetLength();

            if(length1/length2 > 0.85)
            {
                candidate.Sort((x, y) => 
                (Math.Abs(targetArea - PolylineTools.GetArea(x.Poly))).CompareTo(Math.Abs(targetArea - PolylineTools.GetArea(y.Poly))));
            }

            return candidate[0];
        }


        //decider method
        private static Boolean IsOverlap(DividerParams param)
        {
            double crossTolerance = 0.005;

            Vector3d originToDividerEnd = new Vector3d(param.DividerPre.Lines.Last().PointAt(1) - param.OriginPost.Point);
            originToDividerEnd.Unitize();
            Vector3d crossDecider = Vector3d.CrossProduct(originToDividerEnd, param.OriginPost.BaseLine.UnitNormal);

            if (Math.Abs(crossDecider.Length) < crossTolerance)
            {
                double dotDecider = Vector3d.Multiply(originToDividerEnd, param.OriginPost.BaseLine.UnitNormal);
                if (dotDecider > 0)
                    return true;
            }
                

            return false;
        }

        private static Boolean IsConcave(DividerParams param)
        {
            int originCurrentIndex = param.OutlineLabel.Core.FindIndex
                 (i => (i.Liner == param.OriginPost.Liner));

            RoomLine cornerLinePre = new RoomLine();
            RoomLine cornerLinePost = new RoomLine();

            if(param.OriginPost.Point == param.OriginPost.BaseLine.EndPt)
            {
                if (originCurrentIndex == param.OutlineLabel.Core.Count-1)
                    return false;

                cornerLinePre = param.OutlineLabel.Core[originCurrentIndex];
                cornerLinePost = param.OutlineLabel.Core[originCurrentIndex + 1];
            }

            else
            {
                if (originCurrentIndex == 0)
                    return false;

                cornerLinePre = param.OutlineLabel.Core[originCurrentIndex-1];
                cornerLinePost = param.OutlineLabel.Core[originCurrentIndex];
            }

            DividerSetter.CornerState cornerStat = DividerSetter.GetCCWCornerState(cornerLinePre.UnitTangent, cornerLinePost.UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
                return true;
            
            return false;
        }

        private static Boolean IsOriginEndConcave(DividerParams param)
        {
            int originCurrentIndex = param.OutlineLabel.Core.FindIndex
                 (i => (i.Liner == param.OriginPost.Liner));

            RoomLine cornerLinePre = new RoomLine();
            RoomLine cornerLinePost = new RoomLine();


            if (originCurrentIndex == param.OutlineLabel.Core.Count - 1)
                return false;

            cornerLinePre = param.OutlineLabel.Core[originCurrentIndex];
            cornerLinePost = param.OutlineLabel.Core[originCurrentIndex + 1];
            


            DividerSetter.CornerState cornerStat = DividerSetter.GetCCWCornerState(cornerLinePre.UnitTangent, cornerLinePost.UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
                return true;

            return false;
        }

        private static Boolean IsLastSegment(DividerParams param)
        {
            int originIndex = param.OutlineLabel.Core.FindIndex
                (i => i.Liner == param.OriginPost.BaseLine.Liner);

            int lastIndex = param.OutlineLabel.Core.Count-1;
            if (originIndex == lastIndex)
                return true;

            return false;
        }

        /*최소 복도 길이와 본 메서드 톨러런스 둘 다 벗어나는 경우가 있는지..*/
        private static Boolean IsCloseToEnd(DividerParams paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.DividerPost.Origin.Point;
            Point3d baseLineEnd = paramOutput.DividerPost.BaseLine.EndPt;

            double betweenEnd = new Vector3d(baseLineEnd - currentDivOrigin).Length;

            if (betweenEnd < Corridor.MinLengthForDoor)
                return true;

            return false;
        }

        private static Boolean IsCloseToStart(DividerParams paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.DividerPost.Origin.Point;
            Point3d baseLineStart = paramOutput.DividerPost.BaseLine.StartPt;

            double betweenStart = new Vector3d(currentDivOrigin - baseLineStart).Length;

            if (betweenStart < Corridor.MinLengthForDoor)
                return true;

            return false;
        }

        private static Boolean HasEnoughArea(DivMakerOutput divOutput, double targetArea)
        {
            double outputArea = PolylineTools.GetArea(divOutput.Poly);

            if (outputArea > targetArea)
                return true;

            return false;
        }

        
    }

    //inner dateClass
   
}