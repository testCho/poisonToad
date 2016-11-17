using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class FakeTools
    {
        public static List<double> MakeRandomFactor(int numOfRoom, int seed)
        {
            List<double> randomFactors = new List<double>();

            for (int i = 0; i < numOfRoom + 1; i++)
            {
                Random rand1 = new Random(seed + i);
                double tempFactor = rand1.NextDouble();
                if (tempFactor < 0.001)
                    tempFactor = 0.001;

                randomFactors.Add(tempFactor);
            }

            return randomFactors;
        }
    }
}
