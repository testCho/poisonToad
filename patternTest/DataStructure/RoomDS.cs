using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace patternTest
{
    class Core
    {
        //constructor
        public Core(Polyline coreLine, Polyline landing, Vector3d upstairDirec)
        {
            CoreLine = coreLine;
            Landing = landing;
            UpstairDirec = upstairDirec;
        }

        //property
        public Polyline CoreLine { get; private set; }
        public Polyline Landing { get; private set; }
        public Vector3d UpstairDirec{ get; private set; }
    }

    class Corridor
    {
        //field
        private static double scale = 1;
        private static double ONE_WAY_CORRIDOR_WIDTH = 1200;
        private static double TWO_WAY_CORRIDOR_WIDTH = 1800;
        private static double MINIMUM_ROOM_WIDTH = 3000;
        private static double MINIMUM_CORRIDOR_LENGTH = 1200; //임시

        //method

        //property
        public static double MinRoomWidth { get { return MINIMUM_ROOM_WIDTH / scale; } private set { } }
        public static double OneWayWidth { get { return ONE_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double TwoWayWidth { get { return TWO_WAY_CORRIDOR_WIDTH / scale; } private set { } }
        public static double MinLengthForDoor { get { return MINIMUM_CORRIDOR_LENGTH / scale; } private set { } }
    }

    public class LabeledOutline
    {
        //constructor
        public LabeledOutline()
        { }

        public LabeledOutline(Polyline trimmedOutline, Polyline pureOutline, List<RoomLine> labeledCore)
        {
            this.Trimmed = trimmedOutline;
            this.Pure = pureOutline;
            this.Core = labeledCore;
        }

        //property
        public Polyline Trimmed { get; private set; }
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
        { }

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
            for (int i = 0; i < otherDivider.Lines.Count; i++)
                Lines.Add(otherDivider.Lines[i]);

            this.Origin = new DividingOrigin(otherDivider.Origin);
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
        { }

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


    //enum
    public enum LineType { Core, Corridor, Outer, Inner } 
    //선타입 - 코어, 복도, 외벽, 내벽

    public enum corridorType { SH, SV, DH1, DH2, DV }
    //복도타입 - S:single 편복도, D:double 중복도, H:horizontal 횡축, V:vertical 종축, 1:단방향, 2:양방향


}
