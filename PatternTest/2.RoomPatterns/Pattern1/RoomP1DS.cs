using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;
using SmallHousing.Utility;


namespace SmallHousing.RoomPatterns
{
    partial class RoomP1
    {
        public class RoomDimension
        {
            //property
            public static double MinLengthForDoor { get { return Dimensions.MinCorridorLengthForDoor; } private set { } }
        }


        public class RoomLine
        {
            //constructor
            public RoomLine()
            { }

            public RoomLine(Line line, LineType type)
            {
                this.PureLine = line;
                this.Type = type;
            }

            public RoomLine(RoomLine roomLine)
            {
                this.PureLine = roomLine.PureLine;
                this.Type = roomLine.Type;
            }

            //method
            public Point3d PointAt(double t)
            {
                return this.PureLine.PointAt(t);
            }

            //property
            public Line PureLine { get; private set; }
            public LineType Type { get; private set; }
            public double Length { get { return PureLine.Length; } private set { } }
            public Vector3d UnitTangent { get { return PureLine.UnitTangent; } private set { } }
            public Vector3d UnitNormal { get { return VectorTools.RotateVectorXY(UnitTangent, Math.PI / 2); } private set { } }
            public Point3d StartPt { get { return PureLine.PointAt(0); } private set { } }
            public Point3d EndPt { get { return PureLine.PointAt(1); } private set { } }
        }


        public class LabeledOutline
        {
            //constructor
            public LabeledOutline()
            { }

            public LabeledOutline(Polyline booleanDifference, Polyline pureOutline, List<RoomLine> labeledCore)
            {
                this.Difference = booleanDifference;
                this.Pure = pureOutline;
                this.Core = labeledCore;
            }

            //property
            public Polyline Difference { get; private set; }
            public double DifferenceArea { get; set; }
            public Polyline Pure { get; private set; }
            public List<RoomLine> Core { get; private set; }

        }


        //public interface IRoomPattern:IPattern
        //{
        //   List<LabeledOutline> OutlineLabels { get; set; }
        //   List<List<double>> DistributedArea { get; set; }
        //   int CurrentFloor { get; set; }
        //}


        //enum
        public enum LineType { Core, Corridor, Outer, Inner }
        //선타입 - 코어, 복도, 외벽, 내벽
    }
}
