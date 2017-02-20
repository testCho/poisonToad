using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;
namespace SmallHousing.CorridorPatterns
{
    partial class CorridorP1 
    {
        class AxisMaker//이거 문제있을듯, 코어가 외곽선이랑 붙어있는지 판별필요!
        {
            public static List<List<Line>> Make(Polyline outline, P1Core core)
            {
                //output
                List<Line> baseAxis = new List<Line>();
                List<Line> subAxis = new List<Line>();
                Point3d basePt = core.CenterPt; 
                
                //set horizontalAxis, 횡축은 외곽선에서 더 먼 쪽을 선택
                Line horizonReached1 = PCXTools.PCXByEquation(basePt, outline, core.BaseLine.UnitTangent);
                Line horizonReached2 = PCXTools.PCXByEquation(basePt, outline, -core.BaseLine.UnitTangent);

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

                //set verticalAxis, 종축은 외곽선에서 더 가까운 쪽을 선택 //한쪽만 붙어있다면 붙어있는 쪽을 종축으로 선택
                Line verticalReached1 = PCXTools.PCXByEquation(basePt, outline, core.UpstairDirec);
                Line verticalReached2 = PCXTools.PCXByEquation(basePt, outline, -core.UpstairDirec);
                Line verticalToCore1 = PCXTools.PCXByEquation(basePt, core.CoreLine, core.UpstairDirec);
                Line verticalToCore2 = PCXTools.PCXByEquation(basePt, core.CoreLine, -core.UpstairDirec);

                bool isLongerThanToCore1 = verticalReached1.Length > verticalToCore1.Length + 0.5;
                bool isLongerThanToCore2 = verticalReached2.Length > verticalToCore2.Length + 0.5;

                if (isLongerThanToCore1 == isLongerThanToCore2)
                {
                    if (verticalReached1.Length < verticalReached2.Length)
                    {
                        baseAxis.Add(verticalReached1);
                        subAxis.Add(verticalReached2);
                    }
                    else
                    {
                        baseAxis.Add(verticalReached2);
                        subAxis.Add(verticalReached1);
                    }
                }

                else
                {
                    if (!isLongerThanToCore1)
                    {
                        baseAxis.Add(verticalReached1);
                        subAxis.Add(verticalReached2);
                    }

                    else
                    {
                        baseAxis.Add(verticalReached2);
                        subAxis.Add(verticalReached1);
                    }
                }

                List<List<Line>> axisSet = new List<List<Line>>();
                axisSet.Add(baseAxis);
                axisSet.Add(subAxis);

                return axisSet;

            }
        }
    }
}
