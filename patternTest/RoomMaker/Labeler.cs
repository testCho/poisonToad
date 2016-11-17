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
        public static LabeledOutline GetOutlineLabel(Polyline outline, Core core, List<Polyline> corridor)
        {
            // 차집합
            Polyline coreUnion = new Polyline();
            Polyline outlineTrimmed = GetOutlineCoreDiff(outline, core, corridor, out coreUnion);

            // 순수 외곽선
            Polyline outlinePure = TrimDiffByCore(outlineTrimmed, coreUnion);

            // 코어 
            Polyline coreTrimmed = TrimCoreByOutline(outline, coreUnion);
            List<RoomLine> coreSegments = LabelUnionSeg(coreTrimmed, core, corridor);

            return new LabeledOutline(outlineTrimmed, outlinePure, coreSegments);
        }

        private static Polyline GetOutlineCoreDiff(Polyline outline, Core core, List<Polyline> corridor, out Polyline coreUnion)
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

            coreUnion = CurveTools.ToPolyline(union);
            return trimmedOutline[0];
        }

        private static Polyline TrimCoreByOutline(Polyline outline, Polyline coreUnion)
        {
            Polyline pureCore = new Polyline();
            List<Curve> memberToJoin = new List<Curve>();

            Curve outlineCrv = outline.ToNurbsCurve();
            List<Curve> coreSeg = coreUnion.ToNurbsCurve().DuplicateSegments().ToList();

            foreach (Curve i in coreSeg)
            {
                if (!i.IsOverlap(outlineCrv))
                    memberToJoin.Add(i);
            }

            pureCore = CurveTools.ToPolyline(Curve.JoinCurves(memberToJoin)[0]);
            return pureCore;
        }

        private static Polyline TrimDiffByCore(Polyline difference, Polyline coreUnion)
        {
            Polyline pureOutline = new Polyline();
            List<Curve> memberToJoin = new List<Curve>();

            Curve coreCrv = coreUnion.ToNurbsCurve();
            List<Curve> outlineSeg = difference.ToNurbsCurve().DuplicateSegments().ToList();

            foreach (Curve i in outlineSeg)
            {
                if (!i.IsOverlap(coreCrv))
                    memberToJoin.Add(i);
            }

            pureOutline = CurveTools.ToPolyline(Curve.JoinCurves(memberToJoin)[0]);
            return pureOutline;
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
