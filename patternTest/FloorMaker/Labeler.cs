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
        //main
        public static List<LabeledOutline> GetOutlineLabel(Polyline outline, Core core, List<Polyline> corridor)
        {
            //코어 외곽선
            Polyline coreUnion = GetCoreUnion(core, corridor);
            Polyline landingSignedUnion = SignLandingVertexOnUnion(coreUnion, core.Landing);            

            //차집합
            List<Polyline> difference = GetBooleanDifference(outline, landingSignedUnion);

            //순수 건물 외곽선
            List<Polyline> outlinePure = TrimDifferenceByCore(difference, landingSignedUnion);

            //타입 지정된 코어선
            List<Polyline> corePure = TrimDifferenceByOutline(difference, outline);
            List<List<RoomLine>> coreLabeled = LabelUnionSeg(corePure, core, corridor);


            List<LabeledOutline> output = DistributeLabel(difference, outlinePure, coreLabeled, coreUnion);
            return output;
        }

        private static List<LabeledOutline> DistributeLabel(List<Polyline> difference, List<Polyline> outlinePure, List<List<RoomLine>> coreLabeled, Polyline coreUnion)
        {
            List<LabeledOutline> distributedLabels = new List<LabeledOutline>();

            if (outlinePure.Count == 1 && outlinePure.First().IsClosed)
            {
                LabeledOutline nonSeperatedLabel = new LabeledOutline(difference.First(), outlinePure.First(), coreLabeled.First());
                nonSeperatedLabel.DifferenceArea = PolylineTools.GetArea(difference.First()) - PolylineTools.GetArea(coreUnion);
                distributedLabels.Add(nonSeperatedLabel);
                return distributedLabels;
            }

            int seperatedCount = difference.Count;

            for (int i =0; i< seperatedCount; i++)
            {
                LabeledOutline seperatedLabel = new LabeledOutline(difference[i], outlinePure[i], coreLabeled[i]);
                seperatedLabel.DifferenceArea = PolylineTools.GetArea(difference[i]);
                distributedLabels.Add(seperatedLabel);
            }

            return distributedLabels;
        }


        //method
        private static Polyline GetCoreUnion(Core core, List<Polyline> corridor)
        {
            List<Curve> coreAndCorridor = new List<Curve>();

            coreAndCorridor.Add(core.CoreLine.ToNurbsCurve());
            foreach (Polyline i in corridor)
                coreAndCorridor.Add(i.ToNurbsCurve());

            Curve union = Curve.CreateBooleanUnion(coreAndCorridor)[0];
            return CurveTools.ToPolyline(union);
        }

        private static List<double> FindPointParamOnOtherPoly(Polyline basePoly, Polyline vertexFindPoly)
        {
            List<double> vertexOnBaseParam = new List<double>();

            double onDecidingTolerence = 0.005;

            List<Point3d> baseVertex = new List<Point3d>(basePoly);
            List<Point3d> findVertex = new List<Point3d>(vertexFindPoly);
            Curve baseCrv = basePoly.ToNurbsCurve();

            foreach (Point3d i in findVertex)
            {
                double closestParam = new double();
                baseCrv.ClosestPoint(i, out closestParam);
                Point3d closestPtOnCore = baseCrv.PointAt(closestParam);

                bool IsOnCurve = i.DistanceTo(closestPtOnCore) < onDecidingTolerence;
                bool IsSameAsOriginalVertex = Math.Abs(Math.Round(closestParam) - closestParam) < onDecidingTolerence;

                if (IsOnCurve && !IsSameAsOriginalVertex)
                    vertexOnBaseParam.Add(closestParam);
            }

            return vertexOnBaseParam;
        }

        private static Polyline SignLandingVertexOnUnion(Polyline coreUnion, Polyline landing)
        {
            List<Point3d> signedPolyVertex = new List<Point3d>();

            List<double> vertexOnCoreParam = FindPointParamOnOtherPoly(coreUnion, landing);

            for (double i = 0; i < coreUnion.Count; i++)
                vertexOnCoreParam.Add(i);

            vertexOnCoreParam.Sort();

            foreach (double i in vertexOnCoreParam)
            {
                Point3d onCorePt = coreUnion.PointAt(i);
                signedPolyVertex.Add(onCorePt);
            }

            Polyline signedPoly = new Polyline(signedPolyVertex);

            return signedPoly;
        }

        private static List<Polyline> GetBooleanDifference(Polyline outline, Polyline coreUnion)
        {
            List<Polyline> trimmedOutline = new List<Polyline>();

            List<Curve> trimmed = Curve.CreateBooleanDifference(outline.ToNurbsCurve(), coreUnion.ToNurbsCurve()).ToList();

            foreach (Curve i in trimmed)
                trimmedOutline.Add(CurveTools.ToPolyline(i));

            return trimmedOutline;
        }

        private static List<Polyline> TrimDifferenceByCore(List<Polyline> difference, Polyline coreUnion)
        {
            List<Polyline> pureOutline = new List<Polyline>();

            foreach (Polyline i in difference)
            {
                PolylineTools.SetPolylineAlignCCW(i);
                List<Polyline> tempTrimmed = RemoveOverlapSeg(i, coreUnion);
                pureOutline.AddRange(tempTrimmed);
            }

            return pureOutline;
        }

        private static List<Polyline> TrimDifferenceByOutline(List<Polyline> difference, Polyline outline)
        {
            List<Polyline> pureCore = new List<Polyline>();

            foreach (Polyline i in difference)
            {
                PolylineTools.SetPolylineAlignCCW(i);
                List<Polyline> tempTrimmed = RemoveOverlapSeg(i, outline);
                pureCore.AddRange(tempTrimmed);
            }

            foreach (Polyline i in pureCore)
            {
                if (i.IsClosed)
                    PolylineTools.SetPolylineAlignCW(i);
            }

            return pureCore;
        }

        private static List<Polyline> RemoveOverlapSeg(Polyline trimmee, Polyline trimmer)
        {
            List<Polyline> trimmed = new List<Polyline>();
            List<Curve> memberToJoin = new List<Curve>();

            Curve trimmerCrv = trimmer.ToNurbsCurve();
            List<Curve> trimmeeSeg = trimmee.ToNurbsCurve().DuplicateSegments().ToList();

            foreach (Curve i in trimmeeSeg)
            {
                if (!CurveTools.IsOverlap(i, trimmerCrv))
                    memberToJoin.Add(i);
            }

            List<Curve> joinedCrvs = Curve.JoinCurves(memberToJoin, 0, true).ToList();

            foreach (Curve i in joinedCrvs)
                trimmed.Add(CurveTools.ToPolyline(i));

            if (trimmed.Count == 0)
                return new List<Polyline>();

            return trimmed;
        }

        private static List<List<RoomLine>> LabelUnionSeg(List<Polyline> trimmedUnion, Core core, List<Polyline> corridor)
        {
            List<List<RoomLine>> labeledCore = new List<List<RoomLine>>();

            //label decider setting
            Curve coreCrv = core.CoreLine.ToNurbsCurve();
            List<Curve> corridorCrv = new List<Curve>();
            corridorCrv.Add(core.Landing.ToNurbsCurve());
            foreach (Polyline i in corridor)
                corridorCrv.Add(i.ToNurbsCurve());

            //label
            foreach (Polyline i in trimmedUnion)
            {
                List<Line> unlabeledSeg = i.GetSegments().ToList();
                List<RoomLine> currentPolyLabels = new List<RoomLine>();  

                foreach (Line j in unlabeledSeg)
                {
                    Curve jCrv = j.ToNurbsCurve();

                    if (CurveTools.IsOverlap(jCrv, corridorCrv))
                        currentPolyLabels.Add(new RoomLine(j, LineType.Corridor));
                    else if (CurveTools.IsOverlap(jCrv, coreCrv))
                        currentPolyLabels.Add(new RoomLine(j, LineType.Core));
                }

                labeledCore.Add(currentPolyLabels);
            }

            return labeledCore;
        }
    }
}
