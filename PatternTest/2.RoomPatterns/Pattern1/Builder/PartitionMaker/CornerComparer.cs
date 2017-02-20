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
            private class CornerComparer
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
                public List<PartitionParam> Seive(List<PartitionParam> candidates, double targetArea, Plane cornerBasis)
                {
                    List<PartitionParam> sievedByFilling = SieveByFillingRate(candidates, cornerBasis);
                    List<PartitionParam> seivedByAspectRatio = SieveByAspectRatio(sievedByFilling, cornerBasis);
                    seivedByAspectRatio.Sort((a, b) => AreaFitnessComparer(a, b, targetArea));

                    return seivedByAspectRatio;
                }

                public List<PartitionParam> LightSeive(List<PartitionParam> candidates, double targetArea)
                {
                    candidates.Sort((a, b) => AreaFitnessComparer(a, b, targetArea));
                    return candidates;
                }

                //comparer method
                private List<PartitionParam> SieveByFillingRate(List<PartitionParam> unseived, Plane cornerBasis)
                {
                    List<PartitionParam> fillingSeived = new List<PartitionParam>();
                    foreach (PartitionParam i in unseived)
                    {
                        List<double> tempBoundingLength = GetBoundingLength(i.Outline, cornerBasis);
                        double tempPolyArea = PolylineTools.GetArea(i.Outline);
                        double tempFillingRatio = tempPolyArea / (tempBoundingLength[0] * tempBoundingLength[1]);

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

                private List<PartitionParam> SieveByAspectRatio(List<PartitionParam> unseived, Plane cornerBasis)
                {
                    List<PartitionParam> aspectSeived = new List<PartitionParam>();
                    foreach (PartitionParam i in unseived)
                    {
                        List<double> tempBoundingLength = GetBoundingLength(i.Outline, cornerBasis);
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
                private int AreaFitnessComparer(PartitionParam outputA, PartitionParam outputB, double targetArea)
                {
                    //setting
                    double aArea = PolylineTools.GetArea(outputA.Outline);
                    double bArea = PolylineTools.GetArea(outputB.Outline);

                    double aLength = outputA.PartitionPost.GetLength();
                    double bLength = outputB.PartitionPost.GetLength();

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
                    bool isAreaEnough = candidateArea > targetArea;

                    if (isAreaEnough)
                        return Math.Abs(candidateArea - targetArea);

                    return Math.Abs(candidateArea * areaFitTolerance - targetArea);
                }
            }
        }
    }
}

