using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{

    public static class Extended
    {
        /// <summary>
        /// 직선일때만 쓰삼
        /// xy평면으로 -90도 회전한 유닛벡터.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>

        public static Vector3d PV(this Curve c)
        {
            var tempv = c.TangentAtStart;
            tempv.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            return tempv;
        }


    }

}
