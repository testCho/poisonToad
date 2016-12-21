using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PartitionSetter
    {
        //main method
        public static RoomLine FindNearestCorridor(List<RoomLine> coreSeg, RoomLine baseLine)
        {
            RoomLine nearestCorridor = new RoomLine();
            int baseIndex = coreSeg.FindIndex(i => (i.PureLine == baseLine.PureLine));

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

        public static void SetNextDivOrigin(PartitionParam setterParam)
        {
            if (!IsCorridorAvailable(setterParam))
            {
               int originIndex = setterParam.OutlineLabel.Core.FindIndex
                    (i => i.PureLine == setterParam.OriginPost.BaseLine.PureLine);

                if (originIndex != setterParam.OutlineLabel.Core.Count - 1)
                    FindMinCorridor(setterParam);
            }
            

            if (setterParam.PartitionPre.Lines.Count > 1)
            {
                SetOriginFromFoldedDiv(setterParam);
                return;
            }

            SetOriginFromSingleDiv(setterParam);
            return;
        }


        //method
        /*111공통된 부분 줄일 수 있음.. 일단은 구현*/
        private static void FindMinCorridor(PartitionParam setterParam)
        {
            RoomLine nearestCorridor = FindNearestCorridor(setterParam.OutlineLabel.Core, setterParam.OriginPost.BaseLine);
            if (setterParam.OriginPost.BasePureLine == nearestCorridor.PureLine)
            {
                FindCorrFromSameBase(setterParam);
                return;
            }
      
            if(nearestCorridor.Length < CorridorDimension.MinLengthForDoor)
            {
                SetOriginNextStart(setterParam);
                FindCorrFromSameBase(setterParam);
                return;
            }

            Point3d nearestBase = nearestCorridor.StartPt;
            Point3d baseWithMinCorridor = new Point3d(nearestBase + nearestCorridor.UnitTangent * CorridorDimension.MinLengthForDoor);

            PartitionOrigin originNext = new PartitionOrigin(baseWithMinCorridor, nearestCorridor);

            setterParam.OriginPost = originNext;
            return;
        }

        private static void FindCorrFromSameBase(PartitionParam setterParam)
        {
            RoomLine nearestCorridor = setterParam.OriginPost.BaseLine;

            double originToCorridorEnd = new Vector3d(nearestCorridor.PointAt(1)- setterParam.OriginPost.Point).Length;

            if(originToCorridorEnd<CorridorDimension.MinLengthForDoor)
            {
                SetOriginNextStart(setterParam);
                FindCorrFromSameBase(setterParam);
                return;
            }

            Point3d nearestBase = setterParam.OriginPost.Point;
            Point3d baseWithMinCorridor = new Point3d(nearestBase + nearestCorridor.UnitTangent * CorridorDimension.MinLengthForDoor);

            PartitionOrigin originNext = new PartitionOrigin(baseWithMinCorridor, nearestCorridor);

            setterParam.OriginPost = originNext;
            return;
        }
        /* 여기까지111*/

        private static void SetOriginNextStart(PartitionParam setterParam)
        {
            int currentIndex = setterParam.OutlineLabel.Core.FindIndex
                   (i => (i.PureLine == setterParam.OriginPost.BasePureLine));

            if (currentIndex == setterParam.OutlineLabel.Core.Count - 1)
                return;

            RoomLine baseLineNext = setterParam.OutlineLabel.Core[currentIndex + 1];
            setterParam.OriginPost = new PartitionOrigin(baseLineNext.PointAt(0), baseLineNext);

            return;
        }

        private static void SetOriginFromSingleDiv(PartitionParam setterParam)
        {
            if (setterParam.PartitionPre.Origin.BasePureLine == setterParam.OriginPost.BasePureLine)
            {
                SetOriginFromSameBase(setterParam);
                return;
            }

            SetOriginFromOtherBase(setterParam);
            return;
        }

        private static void SetOriginFromFoldedDiv(PartitionParam setterParam)
        {
            Vector3d secondDirec = setterParam.PartitionPre.Lines[1].UnitTangent;

            if (IsCCwPerp(setterParam.PartitionPre.FirstDirec, secondDirec))
            {
                SetOriginFromSingleDiv(setterParam);
                return;
            }

            if (secondDirec.IsParallelTo(setterParam.OriginPost.BasePureLine.UnitTangent) == 1)
            {
                SetOriginFromOtherBase(setterParam);
                return;
            }
            
            return;
        }

        private static void SetOriginFromSameBase(PartitionParam setterParam)
        {
            if (IsEndPt(setterParam.OriginPost))
            {
                SetOriginFromEndPt(setterParam);
                return;
            }

            return; 
        }

        private static void SetOriginFromOtherBase(PartitionParam setterParam)
        {
            if(IsCrossed(setterParam))
            {
                Point3d perpPt = MakePerpPt(setterParam.PartitionPre, setterParam.OriginPost);

                if (IsOnOriginBase(perpPt, setterParam.OriginPost))
                {
                    if (perpPt != setterParam.OriginPost.BaseLine.PointAt(1))
                    {
                        setterParam.OriginPost.Point = perpPt;
                        return;
                    }
                }

                SetOriginFromPerpNext(setterParam);
                return;
            }

            SetOriginFromSameBase(setterParam);
            return;
        }

        private static void SetOriginFromEndPt(PartitionParam setterParam)
        {
            int testIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => (i.PureLine == setterParam.OriginPost.BasePureLine));

            if (testIndex == setterParam.OutlineLabel.Core.Count - 1)
                return;

            RoomLine formerCornerLine = setterParam.OutlineLabel.Core[testIndex];
            RoomLine laterCornerLine = setterParam.OutlineLabel.Core[testIndex + 1];

            PartitionOrigin laterStart = new PartitionOrigin(laterCornerLine.PureLine.PointAt(0),laterCornerLine);
            PartitionOrigin laterEnd = new PartitionOrigin(laterCornerLine.PureLine.PointAt(1),laterCornerLine);

            CornerState cornerStat = GetCCWCornerState(formerCornerLine.UnitTangent, laterCornerLine.UnitTangent);

            if (cornerStat == CornerState.Concave)
            {
                setterParam.OriginPost = laterEnd;
                SetNextDivOrigin(setterParam);
                return;
            }

            return;
        }

        private static void SetOriginFromPerpNext(PartitionParam setterParam)
        {
            Vector3d cornerPre = new Vector3d(setterParam.OriginPost.BasePureLine.UnitTangent);
            SetOriginNextStart(setterParam);
            Vector3d cornerPost = setterParam.OriginPost.BasePureLine.UnitTangent;

            CornerState cornerStat = GetCCWCornerState(cornerPre, cornerPost);

            if (cornerStat == CornerState.Convex)
                return;

            int currentIndex = setterParam.OutlineLabel.Core.FindIndex
                   (i => (i.PureLine == setterParam.OriginPost.BasePureLine));
            if (currentIndex == setterParam.OutlineLabel.Core.Count-1)
            {
                SetPreEndToOrigin(setterParam);
                return;
            }

            SetNextDivOrigin(setterParam);
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


        //decider method
        private static Boolean IsCorridorAvailable(PartitionParam setterParam)
        {
            int dividerIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => i.PureLine == setterParam.PartitionPre.Origin.BasePureLine);

            int originIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => i.PureLine == setterParam.OriginPost.BasePureLine);

            if (dividerIndex == originIndex)
                return IsAvailableOnSameBase(setterParam);

            return IsAvailableOnOtherBase(setterParam, dividerIndex, originIndex);
        }

        private static Boolean IsAvailableOnOtherBase(PartitionParam setterParam, int dividerIndex, int originIndex)
        {
            List<RoomLine> coreSeg = setterParam.OutlineLabel.Core;
            if (coreSeg[dividerIndex].Type == LineType.Corridor)
            {
                double toEndLength = 
                    new Vector3d(coreSeg[dividerIndex].EndPt - setterParam.PartitionPre.Origin.Point).Length;

                if (toEndLength >= CorridorDimension.MinLengthForDoor)
                    return true;
            }

            if(coreSeg[originIndex].Type == LineType.Corridor)
            {
                double toStartLength =
                   new Vector3d(setterParam.OriginPost.Point- coreSeg[originIndex].StartPt).Length;

                if (toStartLength >= CorridorDimension.MinLengthForDoor)
                    return true;
            }

            if (originIndex - dividerIndex > 1)
            {
                for(int i = dividerIndex+1; i<originIndex; i++)
                {
                    if ((coreSeg[i].Type == LineType.Corridor) &&
                        (coreSeg[i].Length >= CorridorDimension.MinLengthForDoor))
                        return true;
                }
            }

            return false;
        }

        private static Boolean IsAvailableOnSameBase(PartitionParam setterParam)
        {
            if (setterParam.OriginPost.Type != LineType.Corridor)
                return false;

            double dividerToOrigin = new Vector3d(setterParam.OriginPost.Point - setterParam.PartitionPre.Origin.Point).Length;

            if (dividerToOrigin < CorridorDimension.MinLengthForDoor)
                return false;

            return true;
        }

        private static Boolean IsCCwPerp(Vector3d tester, Vector3d testee)
        {
            double dotTolerance = 0.005;

            double dotProduct = Math.Abs(Vector3d.Multiply(tester, testee));
            if (dotProduct < dotTolerance)
            {
                Vector3d crossProduct = Vector3d.CrossProduct(tester, testee);
                double dotWithZ = Vector3d.Multiply(crossProduct, Vector3d.ZAxis);
                if (dotWithZ > 0)
                    return true;
            }
                
            return false;
        }

        private static Boolean IsEndPt(PartitionOrigin testOrigin)
        {
            double nearTolerance = 0.005;
            double distance = testOrigin.BaseLine.EndPt.DistanceTo(testOrigin.Point);
            
            if (distance <= nearTolerance)
                return true;

            return false;
        }

        private static Boolean IsCrossed(PartitionParam setterParam)
        {
            Partition dividerTest = setterParam.PartitionPre;
            PartitionOrigin originTest = setterParam.OriginPost;

            Vector3d normal = originTest.BaseLine.UnitNormal;
            Polyline trimmed = setterParam.OutlineLabel.Difference;
            double coverAllLength = new BoundingBox(new List<Point3d>(trimmed)).Diagonal.Length * 2;
            Line testLine = new Line(originTest.Point, originTest.Point + normal * coverAllLength);

            foreach (RoomLine i in dividerTest.Lines)
            {
                if (i.UnitTangent == normal)
                    continue;
               
                Point3d crossPt = CCXTools.GetCrossPt(testLine, i.PureLine);

                if (PCXTools.IsPtOnLine(crossPt, i.PureLine, 0.005)&&PCXTools.IsPtOnLine(crossPt, testLine, 0.005))
                    return true;                              
            }

            return false;     
        }

        private static Point3d MakePerpPt(Partition dividerTest, PartitionOrigin originTest)
        {
            //dSide: divider 쪽, oSide: origin 쪽
            Point3d dSidePt = dividerTest.Lines[dividerTest.Lines.Count - 1].PointAt(1);
            Vector3d dSideDirection = -originTest.BaseLine.UnitNormal;

            Point3d oSidePt = originTest.Point;
            Vector3d oSideDirection = originTest.BaseLine.UnitTangent;

            //ABC is coefficient of linear Equation, Ax+By=C 
            double dSideA = dSideDirection.Y;
            double dSideB = -dSideDirection.X;
            double dSideC = dSideA * dSidePt.X + dSideB * dSidePt.Y;

            double oSideA = oSideDirection.Y;
            double oSideB = -oSideDirection.X;
            double oSideC = oSideA * oSidePt.X + oSideB * oSidePt.Y;

            //det=0: isParallel, 평행한 경우
            double detTolerance = 0.005;
            double det = dSideA * oSideB - dSideB * oSideA;

            if (Math.Abs(det) < detTolerance)
                return Point3d.Unset;

            double perpX = (oSideB * dSideC - dSideB * oSideC) / det;
            double perpY = (dSideA * oSideC - oSideA * dSideC) / det;

            return new Point3d(perpX, perpY, oSidePt.Z);
        }

        private static Boolean IsOnOriginBase(Point3d ptTest, PartitionOrigin originTest)
        {    
            Line testLine = new Line(originTest.Point, originTest.BaseLine.PointAt(1));
            return PCXTools.IsPtOnLine(ptTest, testLine, 0);
        }

        public static CornerState GetCCWCornerState(Vector3d formerCornerLine, Vector3d laterCornerLine)
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
        public enum CornerState { Convex, Concave, Straight };
    }
}
