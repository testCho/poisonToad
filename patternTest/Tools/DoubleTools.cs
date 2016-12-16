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

        public class BinPacker
        {
            public static List<List<double>> PackToBins(List<double> stuffInitial, List<double> bins)
            {
                //scale
                List<double> scaledStuff = new List<double>();

                double binTotal = bins.Sum();
                double stuffTotal = stuffInitial.Sum();

                if (binTotal < stuffTotal)
                    scaledStuff = ScaleToNewSum(binTotal, stuffInitial);
                else
                    scaledStuff = stuffInitial;

                //set
                List<Stuff> stuffList = new List<Stuff>();
                for (int i = 0; i < scaledStuff.Count; i++)
                {
                    Stuff eachStuff = new Stuff(scaledStuff[i], i);
                    stuffList.Add(eachStuff);
                }

                List<BinPack> binList = new List<BinPack>();
                for (int i = 0; i < bins.Count; i++)
                {
                    BinPack eachBinPack = new BinPack(bins[i], i);
                    binList.Add(eachBinPack);
                }


                //initial sorting
                stuffList.Sort((a, b) => -a.Volume.CompareTo(b.Volume));

                foreach (Stuff i in stuffList)
                {
                    binList.Sort((a, b) => BinPackComparer(a, b, i));
                    binList.First().StuffIncluded.Add(i);
                }

                //balancing
                BalanceBinPack(binList);
                binList.Sort((a, b) => (a.IndexInitial.CompareTo(b.IndexInitial)));

                //output
                List<List<double>> packedStuff = new List<List<double>>();
                foreach (BinPack i in binList)
                {
                    List<double> currentPack = new List<double>();

                    i.StuffIncluded.Sort((a, b) => a.IndexInitial.CompareTo(b.IndexInitial));
                    foreach (Stuff j in i.StuffIncluded)
                        currentPack.Add(j.Volume);

                    packedStuff.Add(currentPack);
                }

                return packedStuff;
            }

            private static void BalanceBinPack(List<BinPack> unbalancedPacks)
            {
                List<List<Stuff>> stuffByTotalDescend = new List<List<Stuff>>();
                unbalancedPacks.Sort((a, b) => -a.GetTotal().CompareTo(b.GetTotal()));

                foreach (BinPack i in unbalancedPacks)
                {
                    List<Stuff> tempStuff = new List<Stuff>();

                    foreach (Stuff j in i.StuffIncluded)
                        tempStuff.Add(new Stuff(j));

                    stuffByTotalDescend.Add(tempStuff);
                }

                unbalancedPacks.Sort((a, b) => -a.Volume.CompareTo(b.Volume));
                for (int i = 0; i < unbalancedPacks.Count; i++)
                    unbalancedPacks[i].StuffIncluded = stuffByTotalDescend[i];

                return;
            }

            private static int BinPackComparer(BinPack a, BinPack b, Stuff s)
            {
                //tolerance
                double nearDecidingRate = 1.2;

                //binPackSet
                double aVolume = a.Volume;
                double bVolume = b.Volume;

                double aCost = a.GetCost();
                double bCost = b.GetCost();

                double aProperity = Math.Abs(a.GetCost() - s.Volume);
                double cProperity = Math.Abs(b.GetCost() - s.Volume);

                double sVolume = s.Volume;

                //decider
                bool IsAMoreProper = aProperity < cProperity;


                //
                if (aVolume <= 0)
                {
                    if (bVolume <= 0)
                    {
                        if (IsAMoreProper)
                            return -1;
                        return 1;
                    }

                    if (bCost >= sVolume)
                        return 1;
                    else
                    {
                        if (IsAMoreProper)
                            return -1;
                        return 1;
                    }
                }
                else
                {
                    if (bVolume <= 0)
                    {
                        if (aCost >= sVolume)
                            return -1;
                        else
                        {
                            if (IsAMoreProper)
                                return -1;
                            return 1;
                        }
                    }

                    if (aCost <= sVolume)
                    {
                        if (bCost <= sVolume)
                        {
                            if (IsAMoreProper)
                                return -1;
                            return 1;
                        }

                        if (aCost * nearDecidingRate >= sVolume)
                            return -1;
                        return 1;
                    }

                    if (bCost <= sVolume)
                    {
                        if (bCost * nearDecidingRate >= sVolume)
                            return 1;
                        return -1;
                    }

                    if (IsAMoreProper)
                        return -1;
                    return 1;
                }
            }

            private class Stuff
            {
                public Stuff(double volume, int indexInitial)
                {
                    this.Volume = volume;
                    this.IndexInitial = indexInitial;
                }

                public Stuff(Stuff otherStuff)
                {
                    this.Volume = otherStuff.Volume;
                    this.IndexInitial = otherStuff.IndexInitial;
                }

                public double Volume { get; set; }
                public int IndexInitial { get; set; }
            }

            private class BinPack
            {
                private List<Stuff> stuffIncluded = new List<Stuff>();
                public BinPack(double volume, int indexInitial)
                {
                    this.Volume = volume;
                    this.IndexInitial = indexInitial;
                }

                //
                public double GetCost()
                {
                    double stuffTotal = GetTotal();
                    return Volume - stuffTotal;
                }

                public double GetTotal()
                {
                    double stuffTotal = 0;
                    if (StuffIncluded.Count != 0)
                    {
                        foreach (Stuff i in StuffIncluded)
                            stuffTotal += i.Volume;
                    }

                    return stuffTotal;
                }

                public double Volume { get; private set; }
                public int IndexInitial { get; private set; }
                public List<Stuff> StuffIncluded { get { return stuffIncluded; } set { stuffIncluded = value as List<Stuff>; } }
            }

        }
    }
}
