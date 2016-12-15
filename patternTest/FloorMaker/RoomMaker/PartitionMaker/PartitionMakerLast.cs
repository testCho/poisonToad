using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class PartitionMakerLast
    {
        //main method
        public static DivMakerOutput Draw(PartitionParam preParam, PartitionParam initialParam)
        {
            if (preParam.OutlineLabel.Pure.IsClosed)
                return DrawAtDoughnutType(preParam, initialParam);

            PartitionParam lastParam = new PartitionParam(preParam);
            lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core.Last();
            lastParam.OriginPost.Point = preParam.OutlineLabel.Core.Last().EndPt;

            if (IsCrossed(lastParam))
                return DrawAtPreCrossed(preParam);

            DivMakerOutput lastPartition = PartitionMaker.DrawOrtho(lastParam);
            return lastPartition;
        }


        //method
        private static DivMakerOutput DrawAtDoughnutType(PartitionParam preParam, PartitionParam initialParam)
        {
            PartitionParam lastParam = new PartitionParam(preParam);
            lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core[preParam.OutlineLabel.Core.Count - 1];
            lastParam.OriginPost.Point = lastParam.OriginPost.BaseLine.EndPt;
            lastParam.PartitionPost = new Partition(initialParam.PartitionPre);
            lastParam.PartitionPost.Origin = lastParam.OriginPost;

            DivMakerOutput lastPartition = PartitionMaker.DrawOrtho(lastParam);
            return lastPartition;
        }
         
        private static DivMakerOutput DrawAtPreCrossed(PartitionParam preParam)
        {
            PartitionParam lastParam = new PartitionParam(preParam);
            lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core[preParam.OutlineLabel.Core.Count - 2];
            lastParam.OriginPost.Point = lastParam.OriginPost.BaseLine.EndPt;

            DivMakerOutput lastPartition = PartitionMaker.DrawOrtho(lastParam);
            return lastPartition;
        }

        //decider method

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

                if (PCXTools.IsPtOnLine(crossPt, i.PureLine, 0.005) && PCXTools.IsPtOnLine(crossPt, testLine, 0.005))
                    return true;
            }

            return false;
        }

    }
}
