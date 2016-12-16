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
        //main method
        public static DivMakerOutput DrawEachPartition(PartitionParam param, double targetArea)
        {
            PartitionParam tempParam = new PartitionParam(param);
            PartitionSetter.SetNextDivOrigin(tempParam);

            if (IsOverlap(tempParam))
                MoveOriginProperly(tempParam);

            if (IsOriginEndConcave(tempParam))
                return DrawAtConcaveCorner(tempParam, targetArea);

            return DrawAtThisBaseEnd(tempParam, targetArea);
        }


        //sub method
        private static DivMakerOutput DrawAtConcaveCorner(PartitionParam param, double targetArea)
        {
            int currentIndex = param.OutlineLabel.Core.FindIndex
                (i => (i.PureLine == param.OriginPost.BasePureLine));


            PartitionParam thisEndParam = new PartitionParam(param);
            thisEndParam.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
            DivMakerOutput thisEndOutput = DrawOrtho(thisEndParam);

            if (HasEnoughArea(thisEndOutput, targetArea))
            {
                PartitionParam tempOutput1 = new PartitionParam(param);
                PartitionParam tempOutput2 = new PartitionParam(param);
                SetPostStartToOrigin(tempOutput2);
                List<DivMakerOutput> outputCandidate = new List<DivMakerOutput>();
                outputCandidate.Add(DrawAtStraight(tempOutput1, targetArea));
                outputCandidate.Add(DrawEachPartition(tempOutput2, targetArea));

                return SelectBetterPartition(outputCandidate, targetArea);
            }

            //바로 위와 순서 바꿈.. 문제 되려나?
            if (param.OriginPost.Point == param.OriginPost.BaseLine.EndPt)
            {
                if (currentIndex == param.OutlineLabel.Core.Count - 2)
                    return DrawOrtho(param);


                SetPostStartToOrigin(param);
                return DrawEachPartition(param, targetArea);
            }

            //이거 나중에 수정
            if (currentIndex == param.OutlineLabel.Core.Count - 2)
            {
                param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
                return DrawOrtho(param);
            }


            //SetPostParallelToOrigin(param);
            SetPostStartToOrigin(param);
            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput DrawAtThisBaseEnd(PartitionParam param, double targetArea)
        {
            PartitionParam thisEndParam = new PartitionParam(param);
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

        private static DivMakerOutput DrawAtStraight(PartitionParam param, double targetArea)
        {

            DivMakerOutput binaryOutput = DrawByBinarySearch(param, targetArea);

            if (IsCloseToEnd(binaryOutput.DivParams))
                return PushOriginToEnd(binaryOutput.DivParams);
            else if (IsCloseToStart(binaryOutput.DivParams))
                return PushOriginToStart(binaryOutput.DivParams);

            return binaryOutput;
        }

        private static DivMakerOutput DrawAtConvexCorner(PartitionParam param, double targetArea)
        {
            SetPostStartToOrigin(param);
            DivMakerOutput straightOutput = DrawOrtho(param);

            if (HasEnoughArea(straightOutput, targetArea))
                return TestAtCorner(param, targetArea);

            return DrawEachPartition(param, targetArea);
        }

        private static DivMakerOutput TestAtCorner(PartitionParam param, double targetArea)
        {
            PartitionParam tempParam1 = new PartitionParam(param);
            DivMakerOutput cornerOutput = PartitionMakerCorner.GetCorner(tempParam1, targetArea);

            if (IsOriginEndConcave(param))
            {
                List<DivMakerOutput> outputCandidate = new List<DivMakerOutput>();
                PartitionParam tempParam2 = new PartitionParam(param);
                SetPostStartToOrigin(tempParam2);
                outputCandidate.Add(cornerOutput);
                outputCandidate.Add(DrawEachPartition(tempParam2, targetArea));

                return SelectBetterPartition(outputCandidate, targetArea);
            }

            return cornerOutput;
        }


        //tool method
        private static void MoveOriginProperly(PartitionParam param)
        {
            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int originIndex = coreSeg.FindIndex(i => i.PureLine == param.OriginPost.BasePureLine);
            int coreSegCount = coreSeg.Count();


            double toEndLength = new Line(param.OriginPost.Point, param.OriginPost.BaseLine.EndPt).Length;

            if (toEndLength < Corridor.MinLengthForDoor)
            {
                if (originIndex == coreSegCount - 1)
                {
                    param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
                    return;
                }

                param.OriginPost = new PartitionOrigin(coreSeg[originIndex + 1].StartPt, coreSeg[originIndex + 1]);
                return;
            }

            param.OriginPost.Point = new Point3d(param.OriginPost.Point + param.OriginPost.BaseLine.UnitTangent * Corridor.MinLengthForDoor);
            return;
        }

        private static void SetPostParallelToOrigin(PartitionParam param)
        {
            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int originIndex = coreSeg.FindIndex(i => i.PureLine == param.OriginPost.BasePureLine);
            int coreSegCount = coreSeg.Count();

            if (originIndex == coreSegCount - 1)
                return;

            for (int i = originIndex + 1; i < coreSegCount; i++)
            {
                if (coreSeg[i].UnitTangent == param.OriginPost.BaseLine.UnitTangent)
                {
                    param.OriginPost = new PartitionOrigin(coreSeg[i].StartPt, coreSeg[i]);
                    return;
                }
            }
        }

        public static DivMakerOutput DrawOrtho(PartitionParam param)
        {
            Line orthoLine = PCXTools.PCXByEquation(param.OriginPost.Point, param.OutlineLabel.Pure, param.OriginPost.BaseLine.UnitNormal);
            List<RoomLine> orthoList = new List<RoomLine> { new RoomLine(orthoLine, LineType.Inner) };

            Partition dividerCurrent = new Partition(orthoList, param.OriginPost);

            PartitionParam paramNext = new PartitionParam(param.PartitionPre, dividerCurrent, dividerCurrent.Origin, param.OutlineLabel);
            Polyline outline = RoomOutlineDrawer.GetRoomOutline(paramNext);

            return new DivMakerOutput(outline, paramNext);
        }

        private static DivMakerOutput DrawByBinarySearch(PartitionParam param, double targetArea)
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

            while (lowerBound < upperBound)
            {
                if (loopLimiter > iterNum)
                    break;

                double tempStatus = (upperBound - lowerBound) / 2 + lowerBound;
                Point3d tempOriginPt = ptStart + searchVector / searchRange * tempStatus;
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

        private static DivMakerOutput PushOriginToEnd(PartitionParam param)
        {

            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int indexCurrent = coreSeg.FindIndex(i => i.PureLine == param.OriginPost.BasePureLine);

            if (indexCurrent >= coreSeg.Count - 2) // 사실상 마지막 세그먼트인 경우..
            {
                param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
                return DrawOrtho(param);
            }

            PartitionSetter.CornerState cornerStat =
                PartitionSetter.GetCCWCornerState(coreSeg[indexCurrent].UnitTangent, coreSeg[indexCurrent + 1].UnitTangent);

            if (cornerStat == PartitionSetter.CornerState.Concave)
            {
                SetPostParallelToOrigin(param);
                return DrawOrtho(param);
            }

            param.OriginPost.Point = param.OriginPost.BaseLine.EndPt;
            return DrawOrtho(param);
        }

        private static DivMakerOutput PushOriginToStart(PartitionParam param)
        {

            List<RoomLine> coreSeg = param.OutlineLabel.Core;
            int indexCurrent = param.OutlineLabel.Core.FindIndex
                (i => i.PureLine == param.OriginPost.BasePureLine);

            if (indexCurrent < 2) //이 경우는 거의 없을듯..
                return DrawOrtho(param);

            PartitionSetter.CornerState cornerStat =
                PartitionSetter.GetCCWCornerState(coreSeg[indexCurrent - 1].UnitTangent, coreSeg[indexCurrent - 1].UnitTangent);

            if (cornerStat == PartitionSetter.CornerState.Concave)
            {
                PartitionOrigin newOrigin = new PartitionOrigin(coreSeg[indexCurrent - 2].EndPt, coreSeg[indexCurrent - 2]);
                param.OriginPost = newOrigin;
                return DrawOrtho(param);
            }

            param.OriginPost.Point = param.OriginPost.BaseLine.StartPt;
            return DrawOrtho(param);

        }

        //setter랑 중복코드 있음!!
        private static void SetPostStartToOrigin(PartitionParam param)
        {
            int currentIndex = param.OutlineLabel.Core.FindIndex
                   (i => (i.PureLine == param.OriginPost.BasePureLine));

            if (currentIndex == param.OutlineLabel.Core.Count - 1)
                return;

            RoomLine baseLinePost = param.OutlineLabel.Core[currentIndex + 1];
            param.OriginPost = new PartitionOrigin(baseLinePost.StartPt, baseLinePost);

            return;
        }


        private static void SetPreEndToOrigin(PartitionParam param)
        {
            int currentIndex = param.OutlineLabel.Core.FindIndex
                   (i => (i.PureLine == param.OriginPost.BasePureLine));

            if (currentIndex == 0)
                return;

            RoomLine baseLinePre = param.OutlineLabel.Core[currentIndex - 1];
            param.OriginPost = new PartitionOrigin(baseLinePre.EndPt, baseLinePre);

            return;
        }
        //setter랑 중복코드 있음!!

        private static DivMakerOutput SelectBetterPartition(List<DivMakerOutput> candidate, double targetArea)
        {
            //체 몇개 더 추가..
            candidate.Sort((x, y) => AreaFitnessComparer(x, y, targetArea));

            //debug
            foreach (DivMakerOutput i in candidate)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.Add(i.Poly.ToNurbsCurve());
                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            }
            //

            return candidate[0];
        }


        //decider method
        private static Boolean IsOverlap(PartitionParam param)
        {
            double crossTolerance = 0.005;

            Vector3d originToPartitionEnd = new Vector3d(param.PartitionPre.Lines.Last().PointAt(1) - param.OriginPost.Point);
            originToPartitionEnd.Unitize();
            Vector3d crossDecider = Vector3d.CrossProduct(originToPartitionEnd, param.OriginPost.BaseLine.UnitNormal);

            if (Math.Abs(crossDecider.Length) < crossTolerance)
            {
                double dotDecider = Vector3d.Multiply(originToPartitionEnd, param.OriginPost.BaseLine.UnitNormal);
                if (dotDecider > 0)
                    return true;
            }


            return false;
        }

        private static Boolean IsConcave(PartitionParam param)
        {
            int originCurrentIndex = param.OutlineLabel.Core.FindIndex
                 (i => (i.PureLine == param.OriginPost.BasePureLine));

            RoomLine cornerLinePre = new RoomLine();
            RoomLine cornerLinePost = new RoomLine();

            if (param.OriginPost.Point == param.OriginPost.BaseLine.EndPt)
            {
                if (originCurrentIndex == param.OutlineLabel.Core.Count - 1)
                    return false;

                cornerLinePre = param.OutlineLabel.Core[originCurrentIndex];
                cornerLinePost = param.OutlineLabel.Core[originCurrentIndex + 1];
            }

            else
            {
                if (originCurrentIndex == 0)
                    return false;

                cornerLinePre = param.OutlineLabel.Core[originCurrentIndex - 1];
                cornerLinePost = param.OutlineLabel.Core[originCurrentIndex];
            }

            PartitionSetter.CornerState cornerStat = PartitionSetter.GetCCWCornerState(cornerLinePre.UnitTangent, cornerLinePost.UnitTangent);

            if (cornerStat == PartitionSetter.CornerState.Concave)
                return true;

            return false;
        }

        private static Boolean IsOriginEndConcave(PartitionParam param)
        {
            int originCurrentIndex = param.OutlineLabel.Core.FindIndex
                 (i => (i.PureLine == param.OriginPost.BasePureLine));

            RoomLine cornerLinePre = new RoomLine();
            RoomLine cornerLinePost = new RoomLine();


            if (originCurrentIndex == param.OutlineLabel.Core.Count - 1)
                return false;

            cornerLinePre = param.OutlineLabel.Core[originCurrentIndex];
            cornerLinePost = param.OutlineLabel.Core[originCurrentIndex + 1];



            PartitionSetter.CornerState cornerStat = PartitionSetter.GetCCWCornerState(cornerLinePre.UnitTangent, cornerLinePost.UnitTangent);

            if (cornerStat == PartitionSetter.CornerState.Concave)
                return true;

            return false;
        }

        private static Boolean IsLastSegment(PartitionParam param)
        {
            int originIndex = param.OutlineLabel.Core.FindIndex
                (i => i.PureLine == param.OriginPost.BaseLine.PureLine);

            int lastIndex = param.OutlineLabel.Core.Count - 1;
            if (originIndex == lastIndex)
                return true;

            return false;
        }

        /*최소 복도 길이와 본 메서드 톨러런스 둘 다 벗어나는 경우가 있는지..*/
        private static Boolean IsCloseToEnd(PartitionParam paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.PartitionPost.Origin.Point;
            Point3d baseLineEnd = paramOutput.PartitionPost.BaseLine.EndPt;

            double betweenEnd = new Vector3d(baseLineEnd - currentDivOrigin).Length;

            if (betweenEnd < Corridor.MinLengthForDoor * 0.9)
                return true;

            return false;
        }

        private static Boolean IsCloseToStart(PartitionParam paramOutput)
        {
            Point3d currentDivOrigin = paramOutput.PartitionPost.Origin.Point;
            Point3d baseLineStart = paramOutput.PartitionPost.BaseLine.StartPt;

            double betweenStart = new Vector3d(currentDivOrigin - baseLineStart).Length;

            if (betweenStart < Corridor.MinLengthForDoor * 0.9)
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

        private static int AreaFitnessComparer(DivMakerOutput outputA, DivMakerOutput outputB, double targetArea)
        {
            if (outputA == null)
                return 1;

            //setting
            double aArea = PolylineTools.GetArea(outputA.Poly);
            double bArea = PolylineTools.GetArea(outputB.Poly);

            double aLength = outputA.DivParams.PartitionPost.GetLength();
            double bLength = outputB.DivParams.PartitionPost.GetLength();

            double aCost = ComputeCost(aArea, targetArea);
            double bCost = ComputeCost(bArea, targetArea);

            //decider
            bool isACostLarger = aCost > bCost;

            //compare

            if (isACostLarger)
            {
                if (bCost / aCost > 0.80)
                {
                    if (aLength < bLength )
                        return -1;
                }

                return 1;
            }

            else
            {
                if (aCost / bCost > 0.80)
                {
                    if (bLength <aLength)
                        return 1;
                }

                return -1;
            }

        }

        private static double ComputeCost(double candidateArea, double targetArea)
        {
            bool isAreaEnough = candidateArea >= targetArea;

            if (isAreaEnough)
                return Math.Abs(candidateArea - targetArea);

            return Math.Abs(candidateArea - targetArea/1.2);
        }
    }
}