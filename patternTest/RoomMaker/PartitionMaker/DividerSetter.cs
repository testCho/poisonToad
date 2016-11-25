﻿using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class DividerSetter
    {
        //main method
        public static RoomLine FindNearestCorridor(List<RoomLine> coreSeg, RoomLine baseLine)
        {
            RoomLine nearestCorridor = new RoomLine();
            int baseIndex = coreSeg.FindIndex(i => (i.Liner == baseLine.Liner));

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

        public static void SetNextDivOrigin(DividerParams setterParam)
        {
            if (!IsCorridorAvailable(setterParam))
            {
                FindMinCorridor(setterParam);
                return;
            }

            if (setterParam.DividerPre.Lines.Count > 1)
            {
                SetOriginFromFoldedDiv(setterParam);
                return;
            }

            SetOriginFromSingleDiv(setterParam);
            return;
        }


        //method
        /*111공통된 부분 줄일 수 있음.. 일단은 구현*/
        private static void FindMinCorridor(DividerParams setterParam)
        {
            RoomLine nearestCorridor = FindNearestCorridor(setterParam.OutlineLabel.Core, setterParam.OriginPost.BaseLine);
            if (setterParam.OriginPost.Liner == nearestCorridor.Liner)
            {
                FindCorrFromSameBase(setterParam);
                return;
            }
      
            if(nearestCorridor.Length < Corridor.MinLengthForDoor)
            {
                SetOriginNextStart(setterParam);
                SetNextDivOrigin(setterParam);
                return;
            }

            Point3d nearestBase = nearestCorridor.PointAt(0);
            Point3d baseWithMinCorridor = new Point3d(nearestBase + nearestCorridor.UnitTangent * Corridor.MinLengthForDoor);

            DividingOrigin originNext = new DividingOrigin(baseWithMinCorridor, nearestCorridor);

            SetNextDivOrigin(setterParam);
            return;
        }

        private static void FindCorrFromSameBase(DividerParams setterParam)
        {
            RoomLine nearestCorridor = setterParam.OriginPost.BaseLine;

            double originToCorridorEnd = new Vector3d(nearestCorridor.PointAt(1)- setterParam.OriginPost.Point).Length;

            if(originToCorridorEnd<Corridor.MinLengthForDoor)
            {
                SetOriginNextStart(setterParam);
                SetNextDivOrigin(setterParam);
                return;
            }

            Point3d nearestBase = setterParam.OriginPost.Point;
            Point3d baseWithMinCorridor = new Point3d(nearestBase + nearestCorridor.UnitTangent * Corridor.MinLengthForDoor);

            DividingOrigin originNext = new DividingOrigin(baseWithMinCorridor, nearestCorridor);

            SetNextDivOrigin(setterParam);
            return;
        }
        /* 여기까지111*/

        private static void SetOriginNextStart(DividerParams setterParam)
        {
            int currentIndex = setterParam.OutlineLabel.Core.FindIndex
                   (i => (i.Liner == setterParam.OriginPost.Liner));

            if (currentIndex == setterParam.OutlineLabel.Core.Count - 1)
                return;

            RoomLine baseLineNext = setterParam.OutlineLabel.Core[currentIndex + 1];
            setterParam.OriginPost = new DividingOrigin(baseLineNext.PointAt(0), baseLineNext);

            return;
        }

        private static void SetOriginFromSingleDiv(DividerParams setterParam)
        {
            if (setterParam.DividerPre.Origin.Liner == setterParam.OriginPost.Liner)
            {
                SetOriginFromSameBase(setterParam);
                return;
            }

            SetOriginFromOtherBase(setterParam);
            return;
        }

        private static void SetOriginFromFoldedDiv(DividerParams setterParam)
        {
            Vector3d secondDirec = setterParam.DividerPre.Lines[1].UnitTangent;

            if (IsCCwPerp(setterParam.DividerPre.FirstDirec, secondDirec))
            {
                SetOriginFromSingleDiv(setterParam);
                return;
            }

            if (secondDirec.IsParallelTo(setterParam.OriginPost.Liner.UnitTangent) == 1)
            {
                SetOriginFromOtherBase(setterParam);
                return;
            }

            return;
        }

        private static void SetOriginFromSameBase(DividerParams setterParam)
        {
            if (IsEndPt(setterParam.OriginPost))
            {
                SetOriginFromEndPt(setterParam);
                return;
            }

            return; 
        }

        private static void SetOriginFromOtherBase(DividerParams setterParam)
        {
            if(IsCrossed(setterParam.DividerPre, setterParam.OriginPost))
            {
                Point3d perpPt = MakePerpPt(setterParam.DividerPre, setterParam.OriginPost);

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

        private static void SetOriginFromEndPt(DividerParams setterParam)
        {
            int testIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => (i.Liner == setterParam.OriginPost.Liner));

            RoomLine formerCornerLine = setterParam.OutlineLabel.Core[testIndex];
            RoomLine laterCornerLine = setterParam.OutlineLabel.Core[testIndex + 1];

            DividingOrigin laterStart = new DividingOrigin(laterCornerLine.Liner.PointAt(0),laterCornerLine);
            DividingOrigin laterEnd = new DividingOrigin(laterCornerLine.Liner.PointAt(1),laterCornerLine);

            CornerState cornerStat = GetCCWCornerState(formerCornerLine.UnitTangent, laterCornerLine.UnitTangent);

            if (cornerStat == CornerState.Concave)
            {
                setterParam.OriginPost = laterEnd;
                SetNextDivOrigin(setterParam);
                return;
            }

            setterParam.OriginPost = laterStart;
            SetNextDivOrigin(setterParam);
            return;
        }

        private static void SetOriginFromPerpNext(DividerParams setterParam)
        {
            Vector3d cornerPre = new Vector3d(setterParam.OriginPost.Liner.UnitTangent);
            SetOriginNextStart(setterParam);
            Vector3d cornerPost = setterParam.OriginPost.Liner.UnitTangent;

            CornerState cornerStat = GetCCWCornerState(cornerPre, cornerPost);

            if (cornerStat == CornerState.Convex)
                return;

            SetNextDivOrigin(setterParam);
            return;
        }


        //decider method
        private static Boolean IsCorridorAvailable(DividerParams setterParam)
        {
            int dividerIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => i.Liner == setterParam.DividerPre.Origin.Liner);

            int originIndex = setterParam.OutlineLabel.Core.FindIndex
                (i => i.Liner == setterParam.OriginPost.Liner);

            if (dividerIndex == originIndex)
                return IsAvailableOnSameBase(setterParam);

            return IsAvailableOnOtherBase(setterParam, dividerIndex, originIndex);
        }

        private static Boolean IsAvailableOnOtherBase(DividerParams setterParam, int dividerIndex, int originIndex)
        {
            List<RoomLine> coreSeg = setterParam.OutlineLabel.Core;
            if (coreSeg[dividerIndex].Type == LineType.Corridor)
            {
                double toEndLength = 
                    new Vector3d(coreSeg[dividerIndex].EndPt - setterParam.DividerPre.Origin.Point).Length;

                if (toEndLength >= Corridor.MinLengthForDoor)
                    return true;
            }

            if(coreSeg[originIndex].Type == LineType.Corridor)
            {
                double toStartLength =
                   new Vector3d(setterParam.OriginPost.Point- coreSeg[dividerIndex].StartPt).Length;

                if (toStartLength >= Corridor.MinLengthForDoor)
                    return true;
            }

            if (originIndex - dividerIndex > 1)
            {
                for(int i = originIndex+1; i<dividerIndex; i++)
                {
                    if ((coreSeg[i].Type == LineType.Corridor) &&
                        (coreSeg[i].Length >= Corridor.MinLengthForDoor))
                        return true;
                }
            }

            return false;
        }

        private static Boolean IsAvailableOnSameBase(DividerParams setterParam)
        {
            if (setterParam.OriginPost.Type != LineType.Corridor)
                return false;

            double dividerToOrigin = new Vector3d(setterParam.OriginPost.Point - setterParam.DividerPre.Origin.Point).Length;

            if (dividerToOrigin < Corridor.MinLengthForDoor)
                return false;

            return true;
        }

        private static Boolean IsCCwPerp(Vector3d tester, Vector3d testee)
        {
            Vector3d testVector = VectorTools.RotateVectorXY(tester, Math.PI/2);
            double dotProduct = Math.Abs(Vector3d.Multiply(testVector, testee));
            if (dotProduct < 0.005)
                return true;
            return false;
        }

        private static Boolean IsEndPt(DividingOrigin testOrigin)
        {
            if (testOrigin.Point == testOrigin.Liner.PointAt(0))
                return true;
            return false;
        }

        private static Boolean IsCrossed(DividingLine dividerTest, DividingOrigin originTest)
        {
            Vector3d normal = originTest.BaseLine.UnitNormal;

            foreach(RoomLine i in dividerTest.Lines)
            {
                if (i.UnitTangent == normal)
                    continue;

                double testLength = double.PositiveInfinity;
                LineCurve testCrv = new LineCurve(originTest.Point, originTest.Point + normal * testLength);
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i.Liner.ToNurbsCurve(), testCrv,0,0);

                if (tempIntersection.Count > 0)
                    return true;
            }

            return false;     
        }

        private static Point3d MakePerpPt(DividingLine dividerTest, DividingOrigin originTest)
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

            return new Point3d(perpX, perpY, 0);
        }

        private static Boolean IsOnOriginBase(Point3d ptTest, DividingOrigin originTest)
        {
            if (ptTest == Point3d.Unset)
                return false;

            Point3d oStart = originTest.Point;
            Point3d oEnd = originTest.BaseLine.PointAt(1);

            //isOnOriginTest
            double onTolerance = 0.000;
            bool isSatisfyingX = (ptTest.X - (oStart.X - onTolerance)) * ((oEnd.X + onTolerance) - ptTest.X) >= 0;
            bool isSatisfyingY = (ptTest.Y - (oStart.Y - onTolerance)) * ((oEnd.Y + onTolerance) - ptTest.Y) >= 0;

            if (isSatisfyingX && isSatisfyingY)
                return true;

            return false;
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
