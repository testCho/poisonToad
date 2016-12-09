using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{

    public class LabeledOutline
    {
        //constructor
        public LabeledOutline()
        { }

        public LabeledOutline(List<Polyline> booleanDifference, List<Polyline> pureOutline, List<List<RoomLine>> labeledCore)
        {
            this.Difference = booleanDifference;
            this.Pure = pureOutline;
            this.Core = labeledCore;
        }

        //property
        public List<Polyline> Difference { get; private set; }
        public List<Polyline> Pure { get; private set; }
        public List<List<RoomLine>> Core { get; private set; }

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

    public class PartitionOrigin
    {
        public PartitionOrigin(Point3d basePt, RoomLine baseLine)
        {
            this.Point = basePt;
            this.BaseLine = baseLine;
        }

        public PartitionOrigin()
        {
            this.Point = Point3d.Unset;
            this.BaseLine = new RoomLine();
        }

        public PartitionOrigin(PartitionOrigin otherOrigin)
        {
            this.Point = otherOrigin.Point;
            this.BaseLine = new RoomLine(otherOrigin.BaseLine);
        }

        //method

        //property
        public Point3d Point { get; set; }
        public RoomLine BaseLine { get; set; }
        public LineType Type { get { return BaseLine.Type; } private set { } }
        public Line BasePureLine { get { return BaseLine.PureLine; } private set { } }
    }

    public class Partition
    {
        public Partition(List<RoomLine> dividingLine, PartitionOrigin thisLineOrigin)
        {
            this.Lines = dividingLine;
            this.Origin = thisLineOrigin;
        }

        public Partition(Partition otherPartition)
        {
            List<RoomLine> cloneLines = new List<RoomLine>();
            PartitionOrigin cloneOrigin = new PartitionOrigin();

            //assign
            foreach (RoomLine i in otherPartition.Lines)
                    cloneLines.Add(i);

            cloneOrigin = new PartitionOrigin(otherPartition.Origin);            

            //return
            this.Lines = cloneLines;
            this.Origin = cloneOrigin;
        }

        public Partition()
        {
            this.Lines = new List<RoomLine>();
            this.Origin = new PartitionOrigin();
        }

        //method
        public double GetLength()
        {
            double totalLength = 0;
            foreach (RoomLine i in Lines)
                totalLength += i.Length;

            return totalLength;
        }

        //property
        public List<RoomLine> Lines { get; set; }
        public Vector3d FirstDirec { get { return Lines.First().PureLine.UnitTangent; } private set { } }
        public Vector3d LastDirec { get { return Lines.Last().PureLine.UnitTangent; } private set { } }
        public PartitionOrigin Origin { get; set; }
        public RoomLine BaseLine { get { return Origin.BaseLine; } private set { } }
    }

    public class PartitionParam
    {
        public PartitionParam(Partition dividerPre, PartitionOrigin originPost, LabeledOutline outlineLabel)
        {
            this.PartitionPre = dividerPre;
            this.PartitionPost = new Partition();
            this.OriginPost = originPost;
            this.OutlineLabel = outlineLabel;            
        }

        public PartitionParam(Partition dividerPre, Partition dividerPost, PartitionOrigin originPost, LabeledOutline outlineLabel)
        {
            this.PartitionPre = dividerPre;
            this.PartitionPost = dividerPost;
            this.OriginPost = originPost;
            this.OutlineLabel = outlineLabel;
        }

        public PartitionParam(PartitionParam otherParam)
        {
            this.PartitionPre = new Partition(otherParam.PartitionPre);
            this.PartitionPost = new Partition(otherParam.PartitionPost);
            this.OriginPost = new PartitionOrigin(otherParam.OriginPost);
            this.OutlineLabel = otherParam.OutlineLabel;
        }

        public PartitionParam()
        {
            this.PartitionPre = new Partition();
            this.PartitionPost = new Partition();
            this.OriginPost = new PartitionOrigin();
            this.OutlineLabel = new LabeledOutline();
        }

        //method
        public void PostToPre()
        {
            this.PartitionPre = this.PartitionPost;
        }

        //property
        public Partition PartitionPre { get; set; }
        public Partition PartitionPost { get; set; }
        public PartitionOrigin OriginPost { get; set; }
        public LabeledOutline OutlineLabel { get; set; }
    }

    public class DivMakerOutput
    {
        public DivMakerOutput(Polyline polyline, PartitionParam paramNext)
        {
            this.Poly = polyline;
            this.DivParams = paramNext;
        }

        public DivMakerOutput()
        { }

        public DivMakerOutput(DivMakerOutput otherOutput)
        {
            this.Poly = otherOutput.Poly;
            this.DivParams = new PartitionParam(otherOutput.DivParams);
        }

        //property
        public Polyline Poly { get; set; }
        public PartitionParam DivParams { get; set; }
    }

    //enum
    public enum LineType { Core, Corridor, Outer, Inner } 
    //선타입 - 코어, 복도, 외벽, 내벽


}
