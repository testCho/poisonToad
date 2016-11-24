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
                return DrawAtSeperated(param, targetArea);

            return DrawAtOverlap(param, targetArea);
        }


        //inner dateClass
        public class DivMakerOutput
        {
            public DivMakerOutput(Polyline polyline, DividerParams paramNext)
            {
                this.Poly = polyline;
                this.DivParams = paramNext;
            }

            public DivMakerOutput()
            { }

            public DivMakerOutput(DivMakerOutput otherOutput)
            {
                this.Poly = otherOutput.Poly;
                this.DivParams = new DividerParams(otherOutput.DivParams);
            }

            //property
            public Polyline Poly { get; set; }
            public DividerParams DivParams { get; set; }
        }


        //sub method
        private static DivMakerOutput DrawAtSeperated(DividerParams param, double targetArea)
        {
            DivMakerOutput tempOutput = DrawOrtho(param);

            if (HasEnoughArea(tempOutput,targetArea))
            {
                param.OriginPost.Point = param.OriginPost.BaseLine.PointAt(1);
                if (IsConcave(param))
                    return DrawAtConcaveCorner(param, targetArea);

                tempOutput.DivParams.PostToPre();
                return tempOutput;
            }

            return DrawAtOverlap(param, targetArea);
        }

        private static DivMakerOutput DrawAtOverlap(DividerParams param, double targetArea)
        {
            param.OriginPost.Point = param.OriginPost.BaseLine.PointAt(1);

            if (IsConcave(param))
                return DrawAtConcave(param, targetArea);

            DivMakerOutput tempOutput = DrawOrtho(param);

            if (HasEnoughArea(tempOutput,targetArea))
                return DrawOnThisBase(param, targetArea);

            return DrawAtConvex(param, targetArea);
        }

        private static DivMakerOutput DrawOnThisBase(DividerParams param, double targetArea)
        {
            DivMakerOutput binaryOutput = DrawByBinarySearch(param, targetArea);

            if (IsCloseToStart(binaryOutput.DivParams))
            {
                DivMakerOutput pushStartOutput = PushOriginToStart(binaryOutput.DivParams);
                pushStartOutput.DivParams.PostToPre();
                return pushStartOutput;
            }


            if (IsCloseToEnd(binaryOutput.DivParams))
            {
                DivMakerOutput pushEndOutput = PushOriginToEnd(binaryOutput.DivParams);
                pushEndOutput.DivParams.PostToPre();
                return pushEndOutput;
            }

            binaryOutput.DivParams.PostToPre();
            return binaryOutput;
        }

        private static DivMakerOutput DrawAtConvex(DividerParams param, double targetArea) //볼록 코너 테스트..
        {
            SetPostStartToOrigin(param);
            DivMakerOutput tempOutput = DrawOrtho(param);

            if (HasEnoughArea(tempOutput, targetArea))
                return DrawAtConvexCorner(param, targetArea);

            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput DrawAtConcave(DividerParams param, double targetArea) //고쳐야함.. 오목 코너 테스트 형식..
        {
            SetPostStartToOrigin(param);
            DivMakerOutput tempOutput = DrawOrtho(param);

            if (HasEnoughArea(tempOutput, targetArea))
            {
                SetPreEndToOrigin(param);
                return DrawOnThisBase(param, targetArea); //시작점이 다른 코어선 위에 있어서 처리 필요.. 
            }

            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput DrawAtConvexCorner(DividerParams param, double targetArea)
        {

        }

        private static DivMakerOutput DrawAtConcaveCorner(DividerParams param, double targetArea)
        {

        }

        private static DivMakerOutput DrawAtLastBase(DividerParams param)
        { }

        //tool method
        private static DivMakerOutput DrawOrtho(DividerParams param)
        {
            Line orthoLine = PCXTools.ExtendFromPt(param.OriginPost.Point, param.OutlineLabel.Pure, param.OriginPost.BaseLine.UnitNormal);
            List<RoomLine> orthoList = new List<RoomLine>{ new RoomLine(orthoLine, LineType.Inner)};

            DividingLine dividerCurrent = new DividingLine(orthoList, param.OriginPost);

            DividerParams paramNext = new DividerParams(param.DividerPre, dividerCurrent, dividerCurrent.Origin, param.OutlineLabel); 
            Polyline outline = DividerDrawer.GetPartitionOutline(param.DividerPre, dividerCurrent, param.OutlineLabel);

            return new DivMakerOutput(outline, paramNext);
        }

        private static DivMakerOutput DrawByBinarySearch(DividerParams param, double targetArea)
        {
            int iterNum = 10;

            Point3d ptEnd = param.OriginPost.BaseLine.PointAt(1);
            Point3d ptStart = param.OriginPost.Point;

            Vector3d searchVector = new Vector3d(ptEnd - ptStart);
            double searchRange = searchVector.Length;
            double upperBound = searchRange;
            double lowerBound = 0;

            int loopLimiter = 0;

            while (lowerBound<=upperBound)
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

            Point3d newgOriginPt = ptStart + searchVector / searchRange * lowerBound;
            param.OriginPost.Point = newgOriginPt;

            return DrawOrtho(param);
        }

        private static DivMakerOutput PushOriginToEnd(DividerParams param)
        {

            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int indexCurrent = coreSeg.FindIndex(i => i.Liner == param.OriginPost.Liner);

            if (indexCurrent == coreSeg.Count - 1) // 조기 종료되는 경우..
                return DrawAtLastBase(param);

            DividerSetter.CornerState cornerStat = 
                DividerSetter.GetCCWCornerState(coreSeg[indexCurrent].UnitTangent, coreSeg[indexCurrent + 1].UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
            {
                if (indexCurrent == coreSeg.Count - 2) // 조기 종료되는 경우..
                    return DrawAtLastBase(param);

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

            if (indexCurrent == 0) //이 경우는 거의 없을듯..
                return DrawOrtho(param);

            DividerSetter.CornerState cornerStat =
                DividerSetter.GetCCWCornerState(coreSeg[indexCurrent-1].UnitTangent, coreSeg[indexCurrent - 1].UnitTangent);

            if (cornerStat == DividerSetter.CornerState.Concave)
            {
                if (indexCurrent == 2) //이 경우는 거의 없을듯..
                    return DrawOrtho(param);

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

        //decider method
        private static Boolean IsOverlap(DividerParams param)
        {
            double dotTolerance = 0.005;
            Vector3d originToDividerEnd = new Vector3d(param.DividerPre.Lines.Last().PointAt(1) - param.OriginPost.Point);
            double overlapDecider = Vector3d.Multiply(originToDividerEnd, param.OriginPost.BaseLine.UnitNormal);

            if (Math.Abs(overlapDecider) < dotTolerance)
                return true;

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

        /*최소 복도 길이와 본 메서드 톨러런스 둘 다 벗어나는 경우가 있는지..*/
        private static Boolean IsCloseToEnd(DividerParams paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.DividerPre.Origin.Point;
            Point3d baseLineEnd = paramOutput.DividerPre.BaseLine.EndPt;

            double betweenEnd = new Vector3d(baseLineEnd - currentDivOrigin).Length;

            if (betweenEnd < Corridor.MinLengthForDoor)
                return true;

            return false;
        }

        private static Boolean IsCloseToStart(DividerParams paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.DividerPre.Origin.Point;
            Point3d baseLineStart = paramOutput.DividerPre.BaseLine.StartPt;

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
}