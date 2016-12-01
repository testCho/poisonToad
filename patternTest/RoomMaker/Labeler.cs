﻿using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    //위상이 다른 경우도 표현 가능해야함!! (선, 도넛, 섬)
    class Labeler
    {
        //main
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


        //method
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
            PolylineTools.SetAlignPolylineCW(coreUnion);
            List<Curve> coreSeg = coreUnion.ToNurbsCurve().DuplicateSegments().ToList();

            foreach (Curve i in coreSeg)
            {
                if (!CurveTools.IsOverlap(i, outlineCrv))
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
                if (!CurveTools.IsOverlap(i, coreCrv))
                    memberToJoin.Add(i);
            }

            pureOutline = CurveTools.ToPolyline(Curve.JoinCurves(memberToJoin)[0]);
            return pureOutline;
        }

        private static List<RoomLine> LabelUnionSeg(Polyline trimmedUnion, Core core, List<Polyline> corridor)
        {
            List<RoomLine> labeledRoomBase = new List<RoomLine>();

            //PolylineTools.SetAlignPolyline(trimmedUnion);
            List<Line> unlabeledSeg = trimmedUnion.GetSegments().ToList();

            Curve coreCrv = core.CoreLine.ToNurbsCurve();

            List<Curve> corridorCrv = new List<Curve>();
            corridorCrv.Add(core.Landing.ToNurbsCurve());
            foreach (Polyline i in corridor)
                corridorCrv.Add(i.ToNurbsCurve());

            foreach (Line i in unlabeledSeg)
            {
                Curve iCrv = i.ToNurbsCurve();

                if (CurveTools.IsOverlap(iCrv,corridorCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Corridor));
                else if (CurveTools.IsOverlap(iCrv, coreCrv))
                    labeledRoomBase.Add(new RoomLine(i, LineType.Core));
            }

            return labeledRoomBase;
        }
    }
}
