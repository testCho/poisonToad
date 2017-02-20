using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;
using SmallHousing.CorridorPatterns;
namespace SmallHousing.RoomPatterns
{
    partial class RoomP1
    {
        partial class RoomP1Builder
        {
            private class PartitionMakerLast
            {
                //main method
                public static PartitionParam Draw(PartitionParam preParam, PartitionParam initialParam)
                {
                    if (preParam.OutlineLabel.Pure.IsClosed)
                        return DrawAtDoughnutType(preParam, initialParam);

                    PartitionParam lastParam = new PartitionParam(preParam);
                    lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core.Last();
                    lastParam.OriginPost.Point = preParam.OutlineLabel.Core.Last().EndPt;

                    if (IsCrossed(lastParam))
                        return DrawAtPreCrossed(preParam);

                    return PartitionMaker.DrawOrtho(lastParam);
                }


                //method
                private static PartitionParam DrawAtDoughnutType(PartitionParam preParam, PartitionParam initialParam)
                {
                    PartitionParam lastParam = new PartitionParam(preParam);
                    lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core[preParam.OutlineLabel.Core.Count - 1];
                    lastParam.OriginPost.Point = lastParam.OriginPost.BaseLine.EndPt;
                    lastParam.PartitionPost = new Partition(initialParam.PartitionPre);
                    lastParam.PartitionPost.Origin = lastParam.OriginPost;

                    return lastParam;
                }

                private static PartitionParam DrawAtPreCrossed(PartitionParam preParam)
                {
                    PartitionParam lastParam = new PartitionParam(preParam);
                    lastParam.OriginPost.BaseLine = preParam.OutlineLabel.Core[preParam.OutlineLabel.Core.Count - 2];
                    lastParam.OriginPost.Point = lastParam.OriginPost.BaseLine.EndPt;

                    return PartitionMaker.DrawOrtho(lastParam);
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

                        if (PCXTools.IsPtOnLine(crossPt, i.PureLine, 0.5) && PCXTools.IsPtOnLine(crossPt, testLine, 0.5))
                            return true;
                    }

                    return false;
                }

            }
        }
    }
}
