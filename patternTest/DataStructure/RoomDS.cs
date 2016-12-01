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

        public LabeledOutline(Polyline trimmedOutline, Polyline pureOutline, List<RoomLine> labeledCore, Polyline coreUnion)
        {
            this.Trimmed = trimmedOutline;
            this.Pure = pureOutline;
            this.Core = labeledCore;
            this.CoreUnion = coreUnion;
        }

        //property
        public Polyline Trimmed { get; private set; }
        public Polyline CoreUnion { get; private set; }
        public Polyline Pure { get; private set; }
        public List<RoomLine> Core { get; private set; }

    }

    public class RoomLine
    {
        //constructor
        public RoomLine()
        { }

        public RoomLine(Line line, LineType type)
        {
            this.Liner = line;
            this.Type = type;
        }

        public RoomLine(RoomLine roomLine)
        {
            this.Liner = roomLine.Liner;
            this.Type = roomLine.Type;
        }

        //method
        public Point3d PointAt(double t)
        {
            return this.Liner.PointAt(t);
        }

        //property
        public Line Liner { get; private set; }
        public LineType Type { get; private set; }
        public double Length { get { return Liner.Length; } private set { } }
        public Vector3d UnitTangent { get { return Liner.UnitTangent; } private set { } }
        public Vector3d UnitNormal { get { return VectorTools.RotateVectorXY(UnitTangent, Math.PI / 2); } private set { } }
        public Point3d StartPt { get { return Liner.PointAt(0); } private set { } }
        public Point3d EndPt { get { return Liner.PointAt(1); } private set { } }
    }

    public class DividingOrigin
    {
        public DividingOrigin(Point3d basePt, RoomLine baseLine)
        {
            this.Point = basePt;
            this.BaseLine = baseLine;
        }

        public DividingOrigin()
        {
        }

        public DividingOrigin(DividingOrigin otherOrigin)
        {
            this.Point = otherOrigin.Point;
            this.BaseLine = new RoomLine(otherOrigin.BaseLine);
        }

        //property
        public Point3d Point { get; set; }
        public RoomLine BaseLine { get; set; }
        public LineType Type { get { return BaseLine.Type; } private set { } }
        public Line Liner { get { return BaseLine.Liner; } private set { } }
    }

    public class DividingLine
    {
        public DividingLine(List<RoomLine> dividingLine, DividingOrigin thisLineOrigin)
        {
            this.Lines = dividingLine;
            this.Origin = thisLineOrigin;
        }

        public DividingLine(DividingLine otherDivider)
        {
            this.Lines = new List<RoomLine>();
            this.Origin = new DividingOrigin();

            if (otherDivider!=null && otherDivider.BaseLine!=null)
            {
                for (int i = 0; i < otherDivider.Lines.Count; i++)
                    this.Lines.Add(otherDivider.Lines[i]);

                this.Origin = new DividingOrigin(otherDivider.Origin);
            }            
        }

        public DividingLine()
        { }

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
        public Vector3d FirstDirec { get { return Lines[0].Liner.UnitTangent; } private set { } }
        public Vector3d LastDirec { get { return Lines[Lines.Count-1].Liner.UnitTangent; } private set { } }
        public DividingOrigin Origin { get; set; }
        public RoomLine BaseLine { get { return Origin.BaseLine; } private set { } }
    }

    public class DividerParams
    {
        public DividerParams(DividingLine dividerPre, DividingOrigin originPost, LabeledOutline outlineLabel)
        {
            this.DividerPre = dividerPre;
            this.OriginPost = originPost;
            this.OutlineLabel = outlineLabel;
        }

        public DividerParams(DividingLine dividerPre, DividingLine dividerPost, DividingOrigin originPost, LabeledOutline outlineLabel)
        {
            this.DividerPre = dividerPre;
            this.DividerPost = dividerPost;
            this.OriginPost = originPost;
            this.OutlineLabel = outlineLabel;
        }

        public DividerParams(DividerParams otherParams)
        {
            this.DividerPre = new DividingLine(otherParams.DividerPre);
            this.DividerPost = new DividingLine(otherParams.DividerPost);
            this.OriginPost = new DividingOrigin(otherParams.OriginPost);
            this.OutlineLabel = otherParams.OutlineLabel;
        }

        public DividerParams()
        {
        }

        //method
        public void PostToPre()
        {
            this.DividerPre = this.DividerPost;
        }

        //property
        public DividingLine DividerPre { get; set; }
        public DividingLine DividerPost { get; set; }
        public DividingOrigin OriginPost { get; set; }
        public LabeledOutline OutlineLabel { get; set; }
    }

    public class DivMakerOutput
    {
        public DivMakerOutput(Polyline polyline, DividerParams paramNext)
        {
            this.Poly = polyline;
            this.DivParams = paramNext;
        }

        public DivMakerOutput()
        { }

        public DivMakerOutput(DivMakerOutput otherOutput)
        {
            this.Poly = otherOutput.Poly;
            this.DivParams = new DividerParams(otherOutput.DivParams);
        }

        //property
        public Polyline Poly { get; set; }
        public DividerParams DivParams { get; set; }
    }

    //enum
    public enum LineType { Core, Corridor, Outer, Inner } 
    //선타입 - 코어, 복도, 외벽, 내벽


}
