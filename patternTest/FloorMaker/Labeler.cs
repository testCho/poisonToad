using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Labeler
    {
        private static Polyline TrimOuterByCoreUnion(Polyline outline, Core core, List<Polyline> corridor, out Polyline unionPoly)
        {
            List<Polyline> trimmedOutline = new List<Polyline>();

            List<Curve> coreAndCorridor = new List<Curve>();

            coreAndCorridor.Add(core.CoreLine.ToNurbsCurve());
            foreach (Polyline i in corridor)
                coreAndCorridor.Add(i.ToNurbsCurve());

            Curve union = Curve.CreateBooleanUnion(coreAndCorridor)[0];
            List<Curve> trimmed = Curve.CreateBooleanDifference(outline.ToNurbsCurve(), union).ToList();

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

        //코어 세그먼트가 너무 길어서 코어와 랜딩에 동시에 닿는 경우 처리 필요..
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

            foreach (Line i in unlabeledSeg)
            {
                Curve iCrv = i.ToNurbsCurve();

                if (iCrv.IsOverlap(corridorCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Corridor));
                else if (iCrv.IsOverlap(coreCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Core));
            }

            return labeledRoomBase;
        }

    }
}
