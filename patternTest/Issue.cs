using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    /*
     <단일외곽선 패턴 완성 후 할 일..>

     복도가 외곽선을 완전히 분할했을 경우?
     <= 분할된 폴리라인 각각에 대해 단일 외곽선일 경우와 동일한 과정을 진행 
     <= 분할된 면적에 따라 입력 면적 다시 할당 

     ex)
     1. 전체 면적에 맞는 조정 면적
     2. 각 폴리라인들의 면적
     3. 2. 의 면적비에 따라 1. 면적 할당 (bin Packing Algorithm)
     *4. 3에서 각 폴리라인 면적에 따라 할당된 면적 조정
   
    */

    /*
     <CorridorMaker issue>

     패턴별로 받는 인자 수가 다른 경우 제어는 어떻게?
     <= 

    */

    /* 
     <PartitionMaker issue>

     마지막 코어 세그먼트일 경우 다음 엔드 베이스를 어떻게 찾지? 
     <= 파티션 드로어 레벨에서 마지막에 도달할 경우 찾지 않고 종료 
    */

    /*
     <DividerSetter issue>

     IsOverlap 판정 확실히 할것..
     꺾인 분할선에서 아웃라인 쪽 분할선 점들이 겹치는 경우..
    */

    /*
    <합칠 떄 issue>
    
    코어가 외곽선에서 미묘하게 떨어져 있는 경우?
    */

    class Test
    {
        public static Vector3d MakeUpDirec(Polyline coreLine, Polyline landing)
        {
            Point3d basePt = landing.CenterPoint();
            Curve coreCrv = coreLine.ToNurbsCurve();
            List<Curve> coreSeg = coreCrv.DuplicateSegments().ToList();
            coreSeg.Sort(delegate (Curve x, Curve y)
            {
                double xParam = new double();
                x.ClosestPoint(basePt, out xParam);
                double xDist = basePt.DistanceTo(x.PointAt(xParam));

                double yParam = new double();
                y.ClosestPoint(basePt, out yParam);
                double yDist = basePt.DistanceTo(y.PointAt(yParam));

                if (xDist == yDist)
                    return 0;
                else if (xDist > yDist)
                    return -1;
                else

                    return 1;
            });

            double finalParam = new double();
            coreSeg[0].ClosestPoint(basePt, out finalParam);
            Point3d closestPt = coreSeg[0].PointAt(finalParam);

            return -new Line(basePt, closestPt).UnitTangent;
        }
    }


}
