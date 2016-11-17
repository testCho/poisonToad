using System;
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

        public static void SetNextDivOrigin(DividerParams dividerParam)
        {
            if (dividerParam.Origin.Type != LineType.Corridor)
            {
                SetOriginFromCore(dividerParam);
                return;
            }

            if (dividerParam.Divider.Lines.Count > 1)
            {
                SetOriginFromFoldedDiv(dividerParam);
                return;
            }

            SetOriginFromSingleDiv(dividerParam);
            return;
        }


        //method
        private static void SetOriginFromCore(DividerParams dividerParam)
        {
            RoomLine nearestCorridor = FindNearestCorridor(dividerParam.OutlineLabel.Core, dividerParam.Origin.BaseLine);
            DividingOrigin originNext = new DividingOrigin(nearestCorridor.Liner.PointAt(0), nearestCorridor);
            dividerParam.Origin = originNext;

            SetNextDivOrigin(dividerParam);
            return;
        }

        private static void SetOriginFromSingleDiv(DividerParams dividerParam)
        {
            if (dividerParam.Divider.Origin.Liner == dividerParam.Origin.Liner)
            {
                SetOriginFromSameBase(dividerParam);
                return;
            }

            SetOriginFromOtherBase(dividerParam);
            return;
        }

        private static void SetOriginFromFoldedDiv(DividerParams dividerParam)
        {
            Vector3d secondDirec = dividerParam.Divider.Lines[1].UnitTangent;

            if (IsCCwPerp(dividerParam.Divider.FirstDirec, secondDirec))
            {
                SetOriginFromSingleDiv(dividerParam);
                return;
            }

            if (secondDirec.IsParallelTo(dividerParam.Origin.Liner.UnitTangent) == 1)
            {
                SetOriginFromOtherBase(dividerParam);
                return;
            }

            return;
        }

        private static void SetOriginFromSameBase(DividerParams dividerParam)
        {
            if (dividerParam.Divider.Origin.Point == dividerParam.Origin.Point)
                return;

            if (IsEndPt(dividerParam.Origin))
                return;

            SetOriginFromEndPt(dividerParam);
            return;
        }

        /*추가해야함 - 수선*/
        private static void SetOriginFromOtherBase(DividerParams dividerParam)
        {
            if(IsCrossed(dividerParam.Divider, dividerParam.Origin))
            {
                //can make perpLine??

                int originCurrentIndex = dividerParam.OutlineLabel.Core.FindIndex
                    (i => (i.Liner == dividerParam.Origin.Liner));

                RoomLine lineNext = dividerParam.OutlineLabel.Core[originCurrentIndex + 1];
                dividerParam.Origin = new DividingOrigin(lineNext.Liner.PointAt(0), lineNext);
                return;
            }

            return;
        }

        private static void SetOriginFromEndPt(DividerParams dividerParam)
        {
            int testIndex = dividerParam.OutlineLabel.Core.FindIndex
                (i => (i.Liner == dividerParam.Origin.Liner));

            RoomLine formerCornerLine = dividerParam.OutlineLabel.Core[testIndex];
            RoomLine laterCornerLine = dividerParam.OutlineLabel.Core[testIndex + 1];

            DividingOrigin laterStart = new DividingOrigin(laterCornerLine.Liner.PointAt(0),laterCornerLine);
            DividingOrigin laterEnd = new DividingOrigin(laterCornerLine.Liner.PointAt(1),laterCornerLine);

            CornerState cornerStat = GetCCWCornerState(formerCornerLine.UnitTangent, laterCornerLine.UnitTangent);

            if (cornerStat == CornerState.Concave)
            {
                dividerParam.Origin = laterEnd;
                SetNextDivOrigin(dividerParam);
                return;
            }

            dividerParam.Origin = laterStart;
            SetNextDivOrigin(dividerParam);
            return;
        }


        //decider method
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

        /*추가해야함*/
        private static Boolean CanMakePerpPt(DividingLine dividerTest, DividingOrigin originTest)
        {
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
