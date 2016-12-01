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
            DividingLine dividerPost = param.DividerPost;
            LabeledOutline outlineLabeled = param.OutlineLabel;

            List<Point3d> partitionVertex = new List<Point3d>();

            //코어쪽 꼭지점 추가, divider끼리 겹치는 경우는 아마 없을 것..
            Point3d divPreOriginEnd = dividerPre.Origin.BaseLine.PointAt(1);
            Point3d divPostOriginStart = dividerPost.Origin.BaseLine.PointAt(0);

            int startIndex = outlineLabeled.Core.FindIndex(i => i.Liner == dividerPre.Origin.Liner);
            int endIndex = outlineLabeled.Core.FindIndex(i => i.Liner == dividerPost.Origin.Liner);

            if (startIndex == endIndex) // 시작점, 끝점 같은 경우
            {
                partitionVertex.Add(dividerPre.Origin.Point);
                partitionVertex.Add(dividerPost.Origin.Point);
            }

            else
            {
                if (divPreOriginEnd == dividerPre.Origin.Point)
                    startIndex++;

                if (divPostOriginStart == dividerPost.Origin.Point)
                    endIndex--;

                if (startIndex == endIndex)
                {
                    partitionVertex.Add(dividerPre.Origin.Point);
                    partitionVertex.Add(dividerPost.Origin.Point);
                }
                else
                {
                    partitionVertex.Add(dividerPre.Origin.Point);

                    for (int i = startIndex; i < endIndex; i++)
                        partitionVertex.Add(outlineLabeled.Core[i].PointAt(1));

                    partitionVertex.Add(dividerPost.Origin.Point);
                }
            }

            //dividerPost 추가
            for (int i = 0; i < dividerPost.Lines.Count; i++)
                partitionVertex.Add(dividerPost.Lines[i].PointAt(1));


            //아웃라인쪽 꼭지점 추가
            Point3d dividerPreEnd = dividerPre.Lines.Last().PointAt(1);
            Point3d dividerPostEnd = dividerPost.Lines.Last().PointAt(1);

            List<Line> outlineSeg = outlineLabeled.Pure.GetSegments().ToList();
            double paramPreEndOnOut = outlineLabeled.Pure.ClosestParameter(dividerPreEnd);
            double paramCurrentEndOnOut = outlineLabeled.Pure.ClosestParameter(dividerPostEnd);

            double paramCurrentCeiling = Math.Ceiling(paramCurrentEndOnOut);
            double paramPreFloor = Math.Floor(paramPreEndOnOut);

            if (paramPreEndOnOut < paramCurrentEndOnOut) //인덱스 꼬인 경우
            {
                if (paramCurrentEndOnOut != paramCurrentCeiling)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentCeiling));

                double paramLast = outlineLabeled.Pure.Count-1;

                for (double i = paramCurrentCeiling+1; i < paramLast; i++)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                for (double i = 0; i < paramPreFloor; i++)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                if (paramPreEndOnOut != paramPreFloor)
                    partitionVertex.Add(outlineLabeled.Pure.PointAt(paramPreFloor));

            }

            else //안 꼬인 경우
            {
                double betweenIndexCounter = paramPreFloor - paramCurrentCeiling;

                if(betweenIndexCounter == 0)
                {
                    if ((paramCurrentEndOnOut != paramCurrentCeiling) && (paramPreEndOnOut != paramPreFloor))
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentCeiling));
                }

                else if(betweenIndexCounter > 0)
                {
                    if (paramCurrentEndOnOut != paramCurrentCeiling)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(paramCurrentCeiling));

                    for (double i = paramCurrentCeiling+1; i < paramPreFloor; i++)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(i));

                    if (paramPreEndOnOut != paramPreFloor)
                        partitionVertex.Add(outlineLabeled.Pure.PointAt(paramPreFloor));
                }
            }


            //dividerPre 추가
            for (int i = 0; i < dividerPre.Lines.Count; i++)
                partitionVertex.Add(dividerPre.Lines[dividerPre.Lines.Count - 1 - i].PointAt(1));

            partitionVertex.Add(partitionVertex[0]);
            return new Polyline(partitionVertex);
        }



    }
}
