using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class VectorTools
    {
        public static Vector3d RotateVectorXY(Vector3d baseVector, double angle)
        {
            Vector3d rotatedVector = new Vector3d(baseVector.X * Math.Cos(angle) - baseVector.Y * Math.Sin(angle), baseVector.X * Math.Sin(angle) + baseVector.Y * Math.Cos(angle), 0);
            return rotatedVector;
        } 
        
        public static Vector3d ChangeCoordinate(Vector3d baseVector, Plane fromPln, Plane toPln)
        {
            Vector3d changedVector = baseVector;
            changedVector.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedVector;
        }
    }

    class RectangleTools
    {
        public static Rectangle3d DrawP2PRect(Point3d pointStart, Point3d pointEnd, double thickness)
        {
            Rectangle3d p2pRect = new Rectangle3d();

            Vector3d alignP2P = new Line(pointStart, pointEnd).UnitTangent;
            Vector3d perpP2P = VectorTools.RotateVectorXY(alignP2P, Math.PI / 2);

            Point3d corner1 = pointStart - alignP2P * thickness / 2 + perpP2P * thickness / 2;
            Point3d corner2 = pointEnd + alignP2P * thickness / 2 - perpP2P * thickness / 2;
            Plane p2pPlane = new Plane(pointStart, alignP2P, perpP2P);

            p2pRect = new Rectangle3d(p2pPlane, corner1, corner2);

            return p2pRect;
        }

        public static Rectangle3d DrawP2PRect(Point3d pointStart, Point3d pointEnd, double perpThickness, double alignThickness)
        {
            Rectangle3d p2pRect = new Rectangle3d();

            Vector3d alignP2P = new Line(pointStart, pointEnd).UnitTangent;
            Vector3d perpP2P = VectorTools.RotateVectorXY(alignP2P, Math.PI / 2);

            Point3d corner1 = pointStart - alignP2P * alignThickness/2 + perpP2P * perpThickness / 2;
            Point3d corner2 = pointEnd + alignP2P * alignThickness / 2 - perpP2P * perpThickness / 2;
            Plane p2pPlane = new Plane(pointStart, alignP2P, perpP2P);

            p2pRect = new Rectangle3d(p2pPlane, corner1, corner2);

            return p2pRect;
        }
    }

    class PolylineTools
    {
        public static double GetArea(Polyline input)
        {
            List<Point3d> y = new List<Point3d>(input);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        /// <summary>
        /// 닫힌 폴리라인 방향을 반시계 방향으로 만들어줍니다. (꼭지점의 index 순서 기준)
        /// </summary>
        public static Polyline AlignPolyline(Polyline polyline)
        {
            Polyline output = new Polyline(polyline);
            if (output.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
            {
                output.Reverse();
            }
            return output;
        }

        public static List<Point3d> GetVertex(Polyline polyline)
        {
            List<Point3d> tempVertex = new List<Point3d>(polyline);
            tempVertex.RemoveAt(tempVertex.Count - 1);

            return tempVertex;
        }

        /// <summary>
        /// 폴리라인의 각 세그먼트들에 대해 평행한 또는 안쪽으로 수직인 벡터 리스트를 구해줍니다.(반시계방향 기준)
        /// </summary>
        public class SegmentVector
        {
            public static List<Vector3d> GetAlign(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> alignVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    alignVector.Add(tempVector);
                }
                return alignVector;
            }

            public static List<Vector3d> GetPerpendicular(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> perpVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length
                    tempVector.Transform(Transform.Rotation(Math.PI / 2, tempStart));

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    perpVector.Add(tempVector);
                }
                return perpVector;
            }
        }

        /// <summary>
        /// Offset 대상인 폴리라인의 벡터를 이용해 Offset된 폴리라인의 내부 고리를 없애줍니다.
        /// </summary>
        public static List<Curve> RemoveLoop(Polyline loopedPoly, Polyline baseBound, double tolerance)
        {
            Curve loopedCrv = loopedPoly.ToNurbsCurve();
            Curve boundCrv = baseBound.ToNurbsCurve();
            var tempX = Rhino.Geometry.Intersect.Intersection.CurveSelf(loopedCrv, 0);

            List<double> splitParam = new List<double>();
            foreach (var i in tempX)
            {
                splitParam.Add(i.ParameterA);
                splitParam.Add(i.ParameterB);
            }

            List<Curve> splittedSegment = loopedCrv.Split(splitParam).ToList();
            List<Curve> loopOut = new List<Curve>();

            foreach (Curve i in splittedSegment)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Vector3d testVec = new Vector3d(j.PointAtEnd - j.PointAtStart);
                    Point3d testBase = new Point3d((j.PointAtStart + j.PointAtEnd) / 2);
                    testVec.Transform(Transform.Scale(j.PointAtStart, 1 / testVec.Length));
                    testVec.Transform(Transform.Rotation(Math.PI / 2, j.PointAtStart));
                    Point3d testPt = new Point3d(testBase + testVec * tolerance);

                    int decider = (int)loopedCrv.Contains(testPt, Plane.WorldXY, tolerance / 2);
                    int decider2 = (int)boundCrv.Contains(testPt, Plane.WorldXY, tolerance / 2);

                    if ((decider != 2) && (decider2 != 2))
                        loopOut.Add(j);
                }
            }

            List<Curve> joined = Curve.JoinCurves(loopOut).ToList();
            List<Curve> final = new List<Curve>();
            foreach (Curve k in joined)
            {
                Boolean decider3 = k.IsClosed;
                if (decider3 == true)
                    final.Add(k);
            }

            return final;
        }

        /// <summary>
        /// 폴리라인의 각 변마다 거리를 지정해 Offset 커브를 만들어줍니다.
        /// </summary>
        public static List<Curve> ImprovedOffset(Polyline bound, List<double> offsetDist)
        {
            Polyline ccwBound = AlignPolyline(bound);
            List<Point3d> trimmedOffsetPt = new List<Point3d>();

            //set vectors
            List<Vector3d> alignVector = SegmentVector.GetAlign(ccwBound, true);
            List<Vector3d> perpVector = SegmentVector.GetPerpendicular(ccwBound, true);
            List<Point3d> boundVertex = GetVertex(ccwBound);

            int numSegment = alignVector.Count;
            int numVertex = boundVertex.Count;


            //offset and trim segments
            for (int i = 0; i < numSegment; i++)
            {
                double a = offsetDist[i];
                double b = offsetDist[(i + 1) % offsetDist.Count];
                double dotProduct = Vector3d.Multiply(alignVector[i], alignVector[(i + 1) % numSegment]);
                Vector3d crossProduct = Vector3d.CrossProduct(alignVector[i], alignVector[(i + 1) % numSegment]);
                double cos = Math.Abs(dotProduct / (alignVector[i].Length * alignVector[(i + 1) % numSegment].Length));
                double sin = Math.Sqrt(1 - Math.Pow(cos, 2));

                double decider1 = Vector3d.Multiply(Plane.WorldXY.ZAxis, crossProduct);
                double decider2 = Vector3d.Multiply(-alignVector[i], alignVector[(i + 1) % numSegment]);

                Point3d tempPt = new Point3d();

                if (decider1 > 0) // concave
                {
                    if (decider2 < 0) // blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos - b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos - b) / sin);
                }

                else if (decider1 < 0) // convex
                {
                    if (decider2 < 0) //blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos + b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos + b) / sin);
                }

                else //straight
                    tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * Math.Max(a, b);

                trimmedOffsetPt.Add(tempPt);
            }

            trimmedOffsetPt.Add(trimmedOffsetPt[0]);
            Polyline offBound = new Polyline(trimmedOffsetPt);

            //remove loop
            //List<Curve> loopOut = RemoveLoop(offBound, ccwBound, 0.001);
            //return loopOut;

            List<Curve> debug = new List<Curve>();
            debug.Add(offBound.ToNurbsCurve());
            return debug;
        }

        public static Polyline ChangeCoordinate(Polyline basePoly, Plane fromPln, Plane toPln)
        {
            Polyline changedPoly = new Polyline(basePoly);
            changedPoly.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedPoly;
        }

    }

    class PointTools
    {
        public static Point3d ChangeCoordinate(Point3d basePt, Plane fromPln, Plane toPln)
        {
            Point3d changedPt = basePt;
            changedPt.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedPt;
        }
    }

    class CurveTools
    {
        public static double GetArea(Curve input)
        {
            Polyline plotPolyline;
            input.TryGetPolyline(out plotPolyline);

            List<Point3d> y = new List<Point3d>(plotPolyline);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        public static Polyline ToPolyline(Curve curve)
        {
            Polyline output = new Polyline();
            curve.TryGetPolyline(out output);
            return output;
        }

        public static Curve ChangeCoordinate(Curve baseCrv, Plane fromPln, Plane toPln)
        {
            Curve changedCrv = baseCrv.DuplicateCurve();
            changedCrv.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedCrv;
        }
    }

    class NumberTools
    {
        public static double SumDouble(List<double> doubleList)
        {
            double result = new double();

            foreach (double i in doubleList)
                result += i;

            return result;
        }

        /// <summary>
        /// 리스트 전체 합에 대한 각 요소들의 비율을 구해줍니다.
        /// </summary>
        public static List<double> GetPropotion(List<double> doubleList, int decimals)
        {
            List<double> proportions = new List<double>();

            double sum = SumDouble(doubleList);
            foreach (double i in doubleList)
                proportions.Add(Math.Round((i / sum) * 100, decimals));

            return proportions;
        }
    }

    class Tools
    {
        public class IsotheticOutput
        {
            //constructor
            public IsotheticOutput(Point3d origin, List<Line> axes, Polyline isothetic)
            {
                this.Origin = origin;
                this.Axes = axes;
                this.Isothetic = isothetic;
            }

            public IsotheticOutput(IsotheticOutput isotheticOutput)
            {
                this.Origin = isotheticOutput.Origin;
                this.Axes = isotheticOutput.Axes;
                this.Isothetic = isotheticOutput.Isothetic;
            }

            public IsotheticOutput()
            { }

            //property
            public Point3d Origin { get; }
            public Polyline Isothetic { get; }
            public List<Line> Axes { get; }


        }

        /// <summary>
        /// 지정한 폴리라인에 내접하는, k번 꺾인 직각볼록다각형을 만들어줍니다.
        /// </summary>
        public class kConcaveIsothetic
        {
            //output class


            //main method
            public static IsotheticOutput DrawIsothetic(Polyline boundary, int numberOfConcave, int seed)
            {
                if ((boundary == null) || (boundary.Count == 0))
                    return null;

                else
                {
                    Point3d origin = new Point3d();
                    List<Line> axes = new List<Line>();
                    List<List<double>> rectFactors = GetIsotheticFactors(boundary, numberOfConcave, seed, out origin, out axes);

                    List<Point3d> polylineVertices = new List<Point3d>();
                    polylineVertices.Add(origin);

                    Vector3d mainAxis = axes[0].UnitTangent;
                    Vector3d subAxis = axes[1].UnitTangent;

                    double baseHeight = 0;


                    for (int i = 0; i < rectFactors.Count; i++)
                    {
                        List<double> blockFactor = rectFactors[i];
                        polylineVertices.Add(new Point3d(origin + mainAxis * blockFactor[0] + subAxis * baseHeight));
                        polylineVertices.Add(new Point3d(origin + mainAxis * blockFactor[0] + subAxis * (baseHeight + blockFactor[1])));

                        if (i == rectFactors.Count - 1)
                            polylineVertices.Add(new Point3d(origin + subAxis * (baseHeight + blockFactor[1])));

                        baseHeight += blockFactor[1];
                    }

                    polylineVertices.Add(polylineVertices[0]);
                    Polyline polylineOutput = new Polyline(polylineVertices);

                    IsotheticOutput output = new IsotheticOutput(origin, axes, polylineOutput);
                    return output;
                }
            }

            public static IsotheticOutput DrawStrict(Polyline boundary, int numberOfConcave, Point3d origin, List<Line> biasAxes)
            {
                List<Line> newAxes = new List<Line>();
                List<List<double>> rectFactors = GetIsotheticFactorsStrict(boundary, numberOfConcave, origin, biasAxes, out newAxes);

                List<Point3d> polylineVertices = new List<Point3d>();
                polylineVertices.Add(origin);

                Vector3d mainAxis = newAxes[0].UnitTangent;
                Vector3d subAxis = newAxes[1].UnitTangent;

                double baseHeight = 0;


                for (int i = 0; i < rectFactors.Count; i++)
                {
                    List<double> blockFactor = rectFactors[i];
                    polylineVertices.Add(new Point3d(origin + mainAxis * blockFactor[0] + subAxis * baseHeight));
                    polylineVertices.Add(new Point3d(origin + mainAxis * blockFactor[0] + subAxis * (baseHeight + blockFactor[1])));

                    if (i == rectFactors.Count - 1)
                        polylineVertices.Add(new Point3d(origin + subAxis * (baseHeight + blockFactor[1])));

                    baseHeight += blockFactor[1];
                }

                polylineVertices.Add(polylineVertices[0]);
                Polyline polylineOutput = new Polyline(polylineVertices);

                IsotheticOutput isotheticOutput = new IsotheticOutput(origin, newAxes, polylineOutput);
                return isotheticOutput;
            }

            //method for common
            private static List<List<double>> GetIsotheticFactors(Polyline bound, int NumberOfConcave, int seed, out Point3d basePt, out List<Line> baseAxes)
            {
                double bestArea = new Double();
                Point3d bestBasePt = new Point3d();
                List<List<double>> blockFactors = new List<List<double>>();
                List<Line> bestAxes = new List<Line>();

                List<double> axisAngleSet = SetAxisAngle(bound, Math.PI, Math.PI, 5, true);
                List<Point3d> initPts = SetInitPts(bound, seed + 11, 20);
                initPts.AddRange(SetInitPts(bound, seed + 66, 3));

                for (int i = 0; i < axisAngleSet.Count; i++)
                {
                    double testAngle = axisAngleSet[i];
                    for (int j = 0; j < initPts.Count; j++)
                    {
                        Point3d testBasePt = initPts[j];
                        List<Line> testAxes = setMainAxis(bound, testBasePt, testAngle);
                        double testWidth = testAxes[0].Length;
                        double testHeight = testAxes[1].Length;
                        double testBest = new double();
                        List<List<double>> testBlockFactors = new List<List<double>>();

                        if (testWidth * testHeight > bestArea) //first seive
                        {
                            List<double> testRatios = SetAspectRatios(1, 3, 9);
                            testBlockFactors = makeBlock(bound, testBasePt, testAxes, testRatios, NumberOfConcave, 3, 0, out testBest);
                        }

                        if (testBest > bestArea)
                        {
                            bestArea = testBest;
                            bestBasePt = testBasePt;
                            blockFactors = testBlockFactors;
                            bestAxes = testAxes;
                        }

                    }
                }

                basePt = bestBasePt;
                baseAxes = bestAxes;
                return blockFactors;
            }

            private static List<List<double>> makeBlock(Polyline bound, Point3d basePt, List<Line> axes, List<double> ratioSet, int NumberOfConcave, double minLength, double minOffset, out double blockArea)
            {
                double bestArea = new Double();

                List<List<double>> output = new List<List<double>>();
                Point3d nextPt = basePt;

                foreach (double aspectRatio in ratioSet)
                {
                    double currentBest = new double();
                    double subBest = new double();
                    List<List<double>> currentOutput = new List<List<double>>();


                    List<double> currentBlockFactors = FindMaxWidthHeight(bound, basePt, new Plane(basePt, axes[0].UnitTangent, axes[1].UnitTangent), aspectRatio, 0, axes[0].Length - minOffset, minLength, 8); //second seive
                    nextPt = new Point3d(basePt + axes[1].UnitTangent * currentBlockFactors[1]);
                    List<Line> nextAxes = new List<Line>();
                    nextAxes.Add(new Line(nextPt, nextPt + (axes[0].UnitTangent) * currentBlockFactors[0]));
                    nextAxes.Add(new Line(nextPt, axes[1].To));


                    if (NumberOfConcave == 0)
                    {
                        currentOutput.Add(currentBlockFactors);
                        subBest = 0;
                    }
                    else
                    {
                        currentOutput.Add(currentBlockFactors);
                        currentOutput.AddRange(makeBlock(bound, nextPt, nextAxes, ratioSet, NumberOfConcave - 1, 3, 3, out subBest));
                    }

                    currentBest = subBest + currentBlockFactors[0] * currentBlockFactors[1];

                    if (currentBest > bestArea)
                    {
                        bestArea = currentBest;
                        output = currentOutput;
                    }
                }

                blockArea = bestArea;
                return output;
            }

            private static List<double> SetAxisAngle(Polyline bound, double baseAngle, double angleRange, int angleRes, bool addSegAlign)
            {
                List<double> outputAngle = new List<double>();

                //add angle by rotation
                for (int i = -angleRes; i < angleRes + 1; i++)
                    outputAngle.Add(baseAngle + angleRange / (angleRes * 2) * i);


                //add angle by segments
                if (addSegAlign)
                {
                    List<Line> boundSeg = bound.GetSegments().ToList();
                    boundSeg.OrderByDescending(i => i.Length);
                    int numberOfSegmentAngle = 5;

                    for (int i = 0; i < Math.Min(numberOfSegmentAngle, boundSeg.Count); i++)
                        outputAngle.Add(Vector3d.VectorAngle(boundSeg[i].UnitTangent, Vector3d.XAxis));
                }

                //
                return outputAngle;
            }

            private static List<Point3d> SetInitPts(Polyline bound, int seed, double strictLevel)
            {
                List<Point3d> outputPts = new List<Point3d>();

                Random rand1 = new Random(seed);
                Random rand2 = new Random(seed + 7);

                Polyline ccwBound = AlignPolyline(bound);
                List<Point3d> boundVertex = new List<Point3d>(ccwBound);
                boundVertex.RemoveAt(boundVertex.Count - 1);
                int vertCount = boundVertex.Count;

                for (int i = 0; i < vertCount; i++)
                {
                    Vector3d segVectorA = new Vector3d(boundVertex[(vertCount + i - 1) % vertCount] - boundVertex[i]);
                    Vector3d segVectorB = new Vector3d(boundVertex[(vertCount + i + 1) % vertCount] - boundVertex[i]);
                    int isConvex = Math.Sign(Vector3d.CrossProduct(segVectorA, segVectorB) * Vector3d.ZAxis * -1);

                    Point3d tempPt = Point3d.Unset;
                    int breaker = 0;

                    while ((int)ccwBound.ToNurbsCurve().Contains(tempPt) != 1)
                    {
                        if (breaker == 5) // need to make a solution for near-straight angles
                        {
                            tempPt = bound.CenterPoint();
                            break;
                        }

                        double coefficientA = rand1.NextDouble() * isConvex * 1 / strictLevel;
                        double coefficientB = rand2.NextDouble() * isConvex * 1 / strictLevel;
                        tempPt = new Point3d(boundVertex[i] + coefficientA * segVectorA + coefficientB * segVectorB);

                        breaker++;
                    }

                    outputPts.Add(tempPt);
                }
                return outputPts;
            }

            private static List<Line> setMainAxis(Polyline bound, Point3d basePt, double axisAngle)
            {
                List<Line> outputLines = new List<Line>();

                List<Point3d> boundVertex = new List<Point3d>(bound);
                BoundingBox boundaryBox = new BoundingBox(boundVertex);
                double axisLength = boundaryBox.Diagonal.Length;

                List<Vector3d> axisSet = new List<Vector3d>();
                List<Point3d> reachedPts = new List<Point3d>();

                for (int i = 0; i < 4; i++)
                {
                    Vector3d tempAxis = RotateVectorXY(Vector3d.XAxis, axisAngle + Math.PI / 2 * i);

                    LineCurve acrossing = new LineCurve(basePt, new Point3d(basePt + tempAxis * axisLength));
                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(acrossing, bound.ToNurbsCurve(), 0, 0);

                    List<Point3d> tempPtSet = new List<Point3d>();
                    foreach (var j in tempIntersection)
                        tempPtSet.Add(j.PointA);

                    List<Point3d> sortedPtSet = tempPtSet.OrderBy(p => basePt.DistanceTo(p)).ToList();

                    if (sortedPtSet.Count != 0)
                        reachedPts.Add(sortedPtSet[0]);
                    else
                        reachedPts.Add(basePt); //예외..
                }

                List<Point3d> sortedReachedPts = reachedPts.OrderByDescending(p => basePt.DistanceTo(p)).ToList();
                outputLines.Add(new Line(basePt, sortedReachedPts[0]));

                if (Vector3d.Multiply(new Vector3d(sortedReachedPts[0] - basePt), new Vector3d(sortedReachedPts[1] - basePt)) < -0.005) // could not catch a right angle properly.. a problem of tolerance
                    outputLines.Add(new Line(basePt, sortedReachedPts[2]));
                else
                    outputLines.Add(new Line(basePt, sortedReachedPts[1]));

                return outputLines;
            }

            private static List<double> SetAspectRatios(double minRatio, double maxRatio, int ratioRes)
            {
                List<double> outputRatios = new List<double>();

                if (minRatio < maxRatio)
                    for (int i = 0; i < ratioRes + 1; i++)
                        outputRatios.Add(minRatio * (Math.Pow(maxRatio / minRatio, (double)i / ratioRes)));


                return outputRatios;
            }

            private static List<double> FindMaxWidthHeight(Polyline bound, Point3d basePt, Plane basePlane, double aspectRatio, double lowerBound, double upperBound, double heightLimit, int numberOfIter)
            {
                //using binary search
                Curve tester = bound.ToNurbsCurve();
                double tempLower = lowerBound;
                double tempUpper = upperBound;
                List<double> widthAndHeight = new List<double>();

                int iterCount = 0;

                while (tempLower <= tempUpper)
                {
                    if (iterCount == numberOfIter)
                        break;

                    double tempWidth = tempLower + (tempUpper - tempLower) / 2;
                    double tempHeight = tempWidth / aspectRatio;

                    if (tempHeight > heightLimit) //sieve
                    {
                        Point3d corner1 = new Point3d(basePt);
                        Point3d corner2 = new Point3d(basePt + basePlane.XAxis * tempWidth + basePlane.YAxis * tempHeight);

                        Rectangle3d tempRect = new Rectangle3d(basePlane, corner1, corner2);
                        Curve testRect = tempRect.ToNurbsCurve();

                        var tempIntersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(testRect, tester, 0, 0);
                        int decider = tempIntersect.Count;

                        if (decider == 0)
                            tempLower = tempWidth;

                        else
                            tempUpper = tempWidth;
                    }

                    iterCount++;
                }

                double resultHeight = tempLower / aspectRatio;
                if (resultHeight < heightLimit)
                    resultHeight = 0;

                widthAndHeight.Add(tempLower);
                widthAndHeight.Add(resultHeight);

                return widthAndHeight;
            }


            //method for strict drawing

            private static List<List<double>> GetIsotheticFactorsStrict(Polyline boundary, int numberOfConcave, Point3d origin, List<Line> biasAxes, out List<Line> newAxes)
            {
                double bestArea = new Double();
                List<List<double>> blockFactors = new List<List<double>>();

                List<Line> axes = setMainAxisStrict(boundary, origin, biasAxes);

                List<double> testRatios = SetAspectRatios(1, 3, 9);
                blockFactors = makeBlock(boundary, origin, axes, testRatios, numberOfConcave, 3, 0, out bestArea);

                newAxes = axes;
                return blockFactors;
            }

            public static List<Line> setMainAxisStrict(Polyline bound, Point3d basePt, List<Line> biasAxes)
            {
                List<Line> outputLines = new List<Line>();

                Curve boundCrv = bound.ToNurbsCurve();
                List<Point3d> reachedPts = new List<Point3d>();

                double axisRayLength = new BoundingBox(bound).Diagonal.Length;
                List<Vector3d> biasVector = new List<Vector3d>();

                for (int i = 0; i < biasAxes.Count; i++)
                {
                    biasVector.Add(biasAxes[i].UnitTangent);
                    biasVector.Add(biasAxes[i].UnitTangent * -1);
                }

                for (int i = 0; i < biasVector.Count; i++)
                {
                    LineCurve tempAxisRay = new LineCurve(basePt, basePt + biasVector[i] * axisRayLength);
                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempAxisRay, boundCrv, 0, 0);

                    List<Point3d> intersectPts = new List<Point3d>();
                    foreach (var j in tempIntersection)
                        intersectPts.Add(j.PointA);

                    if (intersectPts.Count() == 0)
                        intersectPts.Add(basePt);

                    intersectPts.Sort((Point3d x, Point3d y) => basePt.DistanceTo(x).CompareTo(basePt.DistanceTo(y)));
                    reachedPts.Add(intersectPts[0]);
                }

                reachedPts.Sort((Point3d x, Point3d y) => -(basePt.DistanceTo(x).CompareTo(basePt.DistanceTo(y))));

                outputLines.Add(new Line(basePt, reachedPts[0]));

                if (Vector3d.Multiply(new Vector3d(reachedPts[0] - basePt), new Vector3d(reachedPts[1] - basePt)) < -0.05) // could not catch a right angle properly.. a problem of tolerance
                    outputLines.Add(new Line(basePt, reachedPts[2]));
                else
                    outputLines.Add(new Line(basePt, reachedPts[1]));

                return outputLines;
            }
        }
    }

    class PCXTools
    {
        public static Line ExtendFromPt(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            Line output = new Line();

            double coverAllLength = new BoundingBox(boundary).Diagonal.Length * 2;
            LineCurve lay = new LineCurve(basePt, basePt + direction / direction.Length * coverAllLength);

            var layIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(lay, boundary.ToNurbsCurve(), 0, 0);

            List<Point3d> intersectedPts = new List<Point3d>();
            foreach (var i in layIntersection)
                intersectedPts.Add(i.PointA);

            intersectedPts.Sort((x, y) => basePt.DistanceTo(x).CompareTo(basePt.DistanceTo(y)));
            output = new Line(basePt, intersectedPts[0]);

            return output;
        }
    }

    class CCXTools
    {
        public static Polyline RegionIntersect(Polyline polyline1, Polyline polyline2)
        {
            Polyline resultPolyine = new Polyline();

            List<double> tempParamA = new List<double>(); //Polyline1 위의 교차점
            List<double> tempParamB = new List<double>(); //Polyline1 위의 교차점
            Curve polyCurve1 = polyline1.ToNurbsCurve();
            Curve polyCurve2 = polyline2.ToNurbsCurve();

            List<Curve> tempLocalResult = new List<Curve>();

            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(polyCurve1, polyCurve2, 0, 0);
            foreach (var i in tempIntersection)
            {
                tempParamA.Add(i.ParameterA);
                tempParamB.Add(i.ParameterB);
            }

            List<Curve> tempSplittedA = polyCurve1.Split(tempParamA).ToList();
            List<Curve> tempSplittedB = polyCurve2.Split(tempParamB).ToList();

            //case of Polyline1
            foreach (Curve i in tempSplittedA)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = new Point3d((j.PointAtEnd + j.PointAtStart) / 2);
                    int decider = (int)polyCurve2.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }

            //case of Polyline2
            foreach (Curve i in tempSplittedB)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = new Point3d((j.PointAtEnd + j.PointAtStart) / 2);
                    int decider = (int)polyCurve1.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }
            List<Curve> resultList = Curve.JoinCurves(tempLocalResult).ToList();
            resultList.OrderByDescending(i => CurveTools.GetArea(i));

            if (resultList.Count != 0)
                resultPolyine = CurveTools.ToPolyline(resultList[0]);

            return resultPolyine;
        }

        public static List<Curve> RegionIntersect(List<Curve> curveSet1, List<Curve> curveSet2)
        {
            List<Curve> IntersectCrvs = new List<Curve>();
            foreach (Curve i in curveSet1)
            {
                foreach (Curve j in curveSet2)
                {
                    List<double> tempParamA = new List<double>();
                    List<double> tempParamB = new List<double>();
                    List<Curve> tempLocalResult = new List<Curve>();

                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i, j, 0, 0);
                    foreach (var k in tempIntersection)
                    {
                        tempParamA.Add(k.ParameterA);
                        tempParamB.Add(k.ParameterB);
                    }

                    List<Curve> tempSplittedA = i.Split(tempParamA).ToList();
                    List<Curve> tempSplittedB = j.Split(tempParamB).ToList();

                    //case of Curve1
                    foreach (Curve k in tempSplittedA)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)j.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }

                    //case of Curve2
                    foreach (Curve k in tempSplittedB)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)i.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }
                    IntersectCrvs.AddRange(Curve.JoinCurves(tempLocalResult).ToList());
                }
            }
            return IntersectCrvs;
        }
    }
}
