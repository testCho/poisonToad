using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class CornerComparer
    {
        //field
        private double fillingTolerance = 0.75;
        private double aspectTolerance = 0.5;
        private double areaFitTolerance = 1.25;
        private double areaInitiative = 0.75;
        private double lengthInitiative = 0.75;

        //property
        public double FillingTolerance { get { return fillingTolerance; } set { fillingTolerance = value; } }
        public double AspectTolerance { get { return aspectTolerance; } set { aspectTolerance = value; } }
        public double AreaFitTolerance { get { return areaFitTolerance; } set { areaFitTolerance = value; } }
        public double AreaInitiative { get { return areaInitiative; } set { areaInitiative = value; } }
        public double LengthInitiative { get { return areaInitiative; } set { areaInitiative = value; } }


        //main method
        public List<DivMakerOutput> Seive(List<DivMakerOutput> candidates, double targetArea, Plane cornerBasis)
        {
            List<DivMakerOutput> sievedByFilling = SieveByFillingRate(candidates, cornerBasis);
            List<DivMakerOutput> seivedByAspectRatio = SieveByAspectRatio(sievedByFilling, cornerBasis);
            seivedByAspectRatio.Sort((a, b) => AreaFitnessComparer(a, b, targetArea));
         
            return seivedByAspectRatio;
        }

        public List<DivMakerOutput> LightSeive(List<DivMakerOutput> candidates, double targetArea)
        {
            candidates.Sort((a, b) => AreaFitnessComparer(a, b, targetArea));
            return candidates;
        }

        //comparer method
        private List<DivMakerOutput> SieveByFillingRate(List<DivMakerOutput> unseived, Plane cornerBasis)
        {
            List<DivMakerOutput> fillingSeived = new List<DivMakerOutput>();
            foreach (DivMakerOutput i in unseived)
            {
                List<double> tempBoundingLength = GetBoundingLength(i.Poly,cornerBasis);
                double tempPolyArea = PolylineTools.GetArea(i.Poly);
                double tempFillingRatio = tempPolyArea / (tempBoundingLength[0]*tempBoundingLength[1]);

                if (tempFillingRatio > fillingTolerance)
                    fillingSeived.Add(i);
            }

            if (fillingSeived.Count == 0)
                return unseived;

            return fillingSeived;
        }

        private List<double> GetBoundingLength(Polyline poly, Plane cornerBasis)
        {
            Polyline copyPoly = new Polyline(poly);
            copyPoly.Transform(Transform.ChangeBasis(Plane.WorldXY, cornerBasis));

            copyPoly.Sort((a, b) => a.X.CompareTo(b.X));
            double maxX = copyPoly.Last().X;
            double minX = copyPoly.First().X;

            copyPoly.Sort((a, b) => a.Y.CompareTo(b.Y));
            double maxY = copyPoly.Last().Y;
            double minY = copyPoly.First().Y;

            List<double> boundingLength = new List<double>();
            boundingLength.Add(Math.Abs(maxX - minX));
            boundingLength.Add(Math.Abs(maxY - minY));

            return boundingLength;
        }

        private List<DivMakerOutput> SieveByAspectRatio(List<DivMakerOutput> unseived, Plane cornerBasis)
        {
            List<DivMakerOutput> aspectSeived = new List<DivMakerOutput>();
            foreach (DivMakerOutput i in unseived)
            {
                List<double> tempBoundingLength = GetBoundingLength(i.Poly, cornerBasis);
                tempBoundingLength.Sort();
                double tempAspectRatio = tempBoundingLength.First() / tempBoundingLength.Last();

                if (tempAspectRatio > aspectTolerance)
                    aspectSeived.Add(i);
            }

            if (aspectSeived.Count == 0)
                return unseived;

            return aspectSeived;
        }


        //
        private int AreaFitnessComparer(DivMakerOutput outputA, DivMakerOutput outputB, double targetArea)
        {
            //setting
            double aArea = PolylineTools.GetArea(outputA.Poly);
            double bArea = PolylineTools.GetArea(outputB.Poly);

            double aLength = outputA.DivParams.PartitionPost.GetLength();
            double bLength = outputB.DivParams.PartitionPost.GetLength();

            double aCost = ComputeCost(aArea, targetArea);
            double bCost = ComputeCost(bArea, targetArea);

            //decider
            bool isACostLarger = aCost > bCost;

            //compare

            if (isACostLarger)
            {
                if (bCost / aCost > areaInitiative)
                {
                    if (aLength < bLength)
                        return -1;  
                }

                return 1;
            }

            else
            {
                if (aCost / bCost > areaInitiative)
                {
                    if (bLength < aLength)
                        return 1;
                }

                return -1;
            }

        }

        private double ComputeCost(double candidateArea, double targetArea)
        {
            bool isAreaEnough = candidateArea> targetArea;

            if (isAreaEnough)
                return Math.Abs(candidateArea - targetArea);

            return Math.Abs(candidateArea*areaFitTolerance - targetArea);
        }
         

    
    }
}

