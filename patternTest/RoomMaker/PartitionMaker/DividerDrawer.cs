using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class DividerDrawer
    {
        public static Polyline GetPartitionOutline(DividerParams param)
        {
            DividingLine dividerPre = param.DividerPre;
            DividingLine dividerCurrent = param.DividerPost;
            LabeledOutline outlineLabeled = param.OutlineLabel;

            List<Point3d> partitionVertex = new List<Point3d>();

            //코어쪽 꼭지점 추가, divider끼리 겹치는 경우는 아마 없을 것..
            Point3d divPreOriginEnd = dividerPre.Origin.BaseLine.PointAt(1);
            Point3d divCurrentOriginStart = dividerCurrent.Origin.BaseLine.PointAt(0);

            int startIndex = outlineLabeled.Core.FindIndex(i => i.Liner == dividerPre.Origin.Liner);
            int endIndex = outlineLabeled.Core.FindIndex(i => i.Liner == dividerCurrent.Origin.Liner);

            if (startIndex == endIndex)
            {
                partitionVertex.Add(divPreOriginEnd);
                partitionVertex.Add(divCurrentOriginStart);
            }

            else
            {
                if (divPreOriginEnd == dividerPre.Origin.Point)
                    startIndex++;

                if (divCurrentOriginStart == dividerCurrent.Origin.Point)
                    endIndex--;

                if (startIndex == endIndex)
                {
                    partitionVertex.Add(divPreOriginEnd);
                    partitionVertex.Add(divCurrentOriginStart);
                }
                else
                {
                    partitionVertex.Add(divPreOriginEnd);

                    for (int i = startIndex; i < endIndex; i++)
                        partitionVertex.Add(outlineLabeled.Core[i].PointAt(1));

                    partitionVertex.Add(divCurrentOriginStart);
                }
            }

            //dividerCurrent 추가
            for (int i = 0; i < dividerCurrent.Lines.Count - 1; i++)
                partitionVertex.Add(dividerCurrent.Lines[i].PointAt(1));


            //아웃라인쪽 꼭지점 추가
            Point3d dividerPreEnd = dividerPre.Lines.Last().PointAt(1);
            Point3d dividerCurrentEnd = dividerCurrent.Lines.Last().PointAt(1);

            List<Line> outlineSeg = outlineLabeled.Pure.GetSegments().ToList();
            double paramPreEndOnOut = outlineLabeled.Pure.ClosestParameter(dividerPreEnd);
            double paramCurrentEndOnOut = outlineLabeled.Pure.ClosestParameter(dividerCurrentEnd);

            double paramCurrentCeiling = Math.Ceiling(paramCurrentEndOnOut);
            double paramPreFloor = Math.Floor(paramPreEndOnOut);

            if (paramPreEndOnOut < paramCurrentEndOnOut)
            {
                if (paramCurrentEndOnOut != paramCurrentCeiling)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentEndOnOut));

                double paramLast = outlineLabeled.Pure.Count-1;

                for (double i = paramCurrentCeiling; i < paramLast; i++)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                Point3d vertexLast = outlineLabeled.Pure.PointAt(paramLast);
                Point3d vertexInit = outlineLabeled.Pure.PointAt(0);

                for (double i = 0; i < paramPreFloor + 1; i++)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                if (paramPreEndOnOut != paramPreFloor)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramPreEndOnOut));

            }

            else
            {
                double betweenIndexCounter = paramPreFloor - paramCurrentCeiling;
                if (betweenIndexCounter < 0)
                {
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentEndOnOut));
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramPreEndOnOut));
                }

                else
                {
                    if (paramCurrentEndOnOut != paramCurrentCeiling)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentEndOnOut));

                    for (double i = paramCurrentCeiling; i < paramPreFloor + 1; i++)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                    if (paramPreEndOnOut != paramPreFloor)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(paramPreEndOnOut));
                }
            }


            //dividerPre 추가
            for (int i = 0; i < dividerCurrent.Lines.Count - 1; i++)
                partitionVertex.Add(dividerCurrent.Lines[dividerCurrent.Lines.Count - 1 - i].PointAt(0));


            return new Polyline(partitionVertex);
        }



    }
}
