using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class DoubleTools
    {
        public static double SumDouble(List<double> doubleList)
        {
            double result = new double();

            foreach (double i in doubleList)
                result += i;

            return result;
        }

        public static List<double> GetPercentage(List<double> doubleList, int decimals)
        {
            List<double> proportions = new List<double>();

            double sum = SumDouble(doubleList);
            foreach (double i in doubleList)
                proportions.Add(Math.Round((i / sum) * 100, decimals));

            return proportions;
        }

        public static List<double> ScaleToNewSum(double newSum, List<double> oldPortions)
        {
            List<double> newPortions = new List<double>();

            double oldSum = SumDouble(oldPortions);
            foreach (double i in oldPortions)
                newPortions.Add(newSum * (i / oldSum));

            return newPortions;
        }
    }
}
