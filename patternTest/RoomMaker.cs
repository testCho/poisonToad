using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class RoomMaker
    {

        //main method
        public List<Polyline> MakeRoom(List<double> roomAreaSet, Polyline outline, Core core)
        {
            List<Polyline> rooms = new List<Polyline>();

            Line baseLine = SearchBaseLine(core);
            List<Line> subAxis = new List<Line>();
            List<Line> baseAxis = SetBaseAxis(outline, core, baseLine, out subAxis);

            List<corridorType> availableTypes = DetectAvailableType(outline, core, baseAxis, subAxis);


            return rooms;
        }

        private static List<corridorType> DetectAvailableType(Polyline outline, Core core, List<Line> baseAxis, List<Line> subAxis)
        {
            //outline 과 landing 사이 거리,방향으로 가능한 복도타입 판정
            List<corridorType> available = new List<corridorType>();

            Point3d basePt = baseAxis[0].PointAt(0);

            double toOutlineDistH = subAxis[0].Length;
            double toCoreDistH = PCXTools.ExtendFromPt(basePt, core.CoreLine, subAxis[0].UnitTangent).Length;
            double toOutlineDistV = baseAxis[1].Length;
            double toCoreDistV = PCXTools.ExtendFromPt(basePt, core.CoreLine, baseAxis[1].UnitTangent).Length;

            bool deciderH = toOutlineDistH > toCoreDistH;
            bool deciderV = toOutlineDistV > toCoreDistV;

            if (deciderH)
                available.Add(corridorType.DH2);
            else if (deciderV)
            {
                available.Add(corridorType.SV);
                available.Add(corridorType.DH1);
            }
            else
            {
                available.Add(corridorType.SH);
                available.Add(corridorType.SV);
                available.Add(corridorType.DH1);
                available.Add(corridorType.DV);

            }
                return available;
        }

        //method
        private static Line SearchBaseLine(Core core)
        {
            //output
            Line baseSeg = new Line();

            //process
            List<Line> landingSeg = core.Landing.GetSegments().ToList();
            List<Line> perpToStair = new List<Line>();

            double perpTolerance = 0.005;

            foreach (Line i in landingSeg)
            {
                double axisDecider = Math.Abs(Vector3d.Multiply(i.Direction, core.UpstairDirec));
                if (axisDecider < perpTolerance)
                    perpToStair.Add(i);
            }

            perpToStair.Sort(delegate (Line x, Line y)
            {
                Point3d perp1Center = x.PointAt(0.5);
                Point3d perp2Center = y.PointAt(0.5);

                Vector3d gapBetween = perp1Center - perp2Center;
                double decider = Vector3d.Multiply(gapBetween, core.UpstairDirec);

                if (decider > 0)
                    return -1;
                else if (decider == 0)
                    return 0;
                else
                    return 1;
            });

            baseSeg = perpToStair[0];

            return baseSeg;
        }

        private static double SetBasePtVLimit(Core core, Line baseLine)
        {
            double vLimit = 0;

            Point3d decidingPt1 = baseLine.PointAt(0.01) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;
            Point3d decidingPt2 = baseLine.PointAt(0.09) - core.UpstairDirec / core.UpstairDirec.Length * 0.01;

            double candidate1 = PCXTools.ExtendFromPt(decidingPt1, core.Landing, -core.UpstairDirec).Length + 0.01;
            double candidate2 = PCXTools.ExtendFromPt(decidingPt2, core.Landing, -core.UpstairDirec).Length + 0.01;

            if (candidate1 > candidate2)
                vLimit = candidate2;
            else
                vLimit = candidate1;

            return vLimit;
        }

        private static List<Line> SetBaseAxis(Polyline outline, Core core, Line baseLine, out List<Line> counterAxis)
        {
            //output
            List<Line> baseAxis = new List<Line>();
            List<Line> subAxis = new List<Line>();

            //process
            double basePtY = SetBasePtVLimit(core, baseLine);
            Point3d basePt = baseLine.PointAt(0.5) - (core.UpstairDirec / core.UpstairDirec.Length) * SetBasePtVLimit(core, baseLine) / 2;

            //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
            Line horizonReached1 = PCXTools.ExtendFromPt(basePt, outline, baseLine.UnitTangent);
            Line horizonReached2 = PCXTools.ExtendFromPt(basePt, outline, -baseLine.UnitTangent);

            if (horizonReached1.Length > horizonReached2.Length)
            {
                baseAxis.Add(horizonReached1);
                subAxis.Add(horizonReached2);
            }
            else
            {
                baseAxis.Add(horizonReached2);
                subAxis.Add(horizonReached1);
            }
            //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택
            Line verticalReached1 = PCXTools.ExtendFromPt(basePt, outline, core.UpstairDirec);
            Line verticalReached2 = PCXTools.ExtendFromPt(basePt, outline, -core.UpstairDirec);

            if (verticalReached1.Length < verticalReached2.Length)
            {
                baseAxis.Add(verticalReached1);
                baseAxis.Add(verticalReached2);
            }
            else
            {
                baseAxis.Add(verticalReached2);
                baseAxis.Add(verticalReached1);
            }

            counterAxis = subAxis;
            return baseAxis;

        }

        private static Polyline TrimOuterByCoreUnion(Polyline outline, Core core, List<Polyline> corridor, out Polyline unionPoly)
        {
            List<Polyline> trimmedOutline = new List<Polyline>();

            List<Curve> coreAndCorridor = new List<Curve>();

            coreAndCorridor.Add(core.CoreLine.ToNurbsCurve());
            foreach (Polyline i in corridor)
                coreAndCorridor.Add(i.ToNurbsCurve());

            Curve union = Curve.CreateBooleanUnion(coreAndCorridor)[0];
            List<Curve>trimmed = Curve.CreateBooleanDifference(outline.ToNurbsCurve(), union).ToList();

            foreach (Curve i in trimmed)
                trimmedOutline.Add(CurveTools.ToPolyline(i));
            trimmedOutline.Sort((x, y) => -PolylineTools.GetArea(x).CompareTo(PolylineTools.GetArea(y)));

            unionPoly = CurveTools.ToPolyline(union);
            return trimmedOutline[0];
        }

        private static Polyline TrimCoreUnionByOuter(Polyline outline, Polyline coreUnion)
        {
            Polyline roomBase = new Polyline();
            List<Curve> candidateToJoin = new List<Curve>();

            Curve outlineCrv = outline.ToNurbsCurve();
            List<Curve> unionSeg = coreUnion.ToNurbsCurve().DuplicateSegments().ToList();

            foreach (Curve i in unionSeg)
            {
                if (!i.IsOverlap(outlineCrv))
                    candidateToJoin.Add(i);
            }

            roomBase = CurveTools.ToPolyline(Curve.JoinCurves(candidateToJoin)[0]);
            return roomBase;
        }   

        private static List<RoomLine> LabelUnionSeg(Polyline trimmedUnion, Core core, List<Polyline> corridor)
        {
            List<RoomLine> labeledRoomBase = new List<RoomLine>();

            trimmedUnion.AlignCC();
            List<Line> unlabeledSeg = trimmedUnion.GetSegments().ToList();

            Curve coreCrv = core.CoreLine.ToNurbsCurve();

            List<Curve> corridorCrv = new List<Curve>();
            corridorCrv.Add(core.Landing.ToNurbsCurve());
            foreach (Polyline i in corridor)
                corridorCrv.Add(i.ToNurbsCurve());

            foreach(Line i in unlabeledSeg)
            {
                Curve iCrv = i.ToNurbsCurve();

                if (iCrv.IsOverlap(corridorCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Corridor));
                else if (iCrv.IsOverlap(coreCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Core));
            }

            return labeledRoomBase;
        }

        //enum
        private enum LineType {Core,Corridor,Outer,Inner} // 선타입 - 코어, 복도, 외벽, 내벽
        private enum corridorType {SH,SV,DH1,DH2,DV} //복도타입 - S:single 편복도, D: double 중복도, H: 횡축, V: 종축, 1:단방향, 2:양방향
        

        //class
        private class RoomLine
        {
            //constructor
            public RoomLine(Line line, LineType type)
            {
                this.Liner = line;
                this.Type = type;
            }

            //property
            public Line Liner { get; private set; }
            public LineType Type { get; private set; }
            public double Length { get { return Liner.Length; } private set { } }
            public Vector3d UnitTangent { get { return Liner.UnitTangent; } private set { } }
        }        
    }

}
