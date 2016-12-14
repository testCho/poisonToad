using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace patternTest
{
    [System.Runtime.InteropServices.Guid("5520beef-8761-4ab0-abb3-442ac054e300")]
    public class patternTestCommand : Command
    {
        public patternTestCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static patternTestCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "roooom"; }
        }

        public void Print(List<Polyline> lc, Color color, RhinoDoc doc)
        {

            foreach (var r in lc)
            {
                Guid temp = doc.Objects.Add(r.ToNurbsCurve());
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        public void Print(Polyline r, Color color, RhinoDoc doc)
        {

       
                Guid temp = doc.Objects.Add(r.ToNurbsCurve());
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            
        }

        public void Print(List<Brep> lc, Color color, RhinoDoc doc)
        {

            foreach (var r in lc)
            {
                Guid temp = doc.Objects.Add(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        public void Print(Brep lc, Color color, RhinoDoc doc)
        {

     
                Guid temp = doc.Objects.Add(lc);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            
        }

        public void Print(Curve[] lc, Color color, RhinoDoc doc)
        {

            foreach (var r in lc)
            {
                Guid temp = doc.Objects.Add(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("안녕하세요 방만드는기계 입니다.");

            Polyline outline = new Polyline();
            Polyline coreLine = new Polyline();
            Polyline landing = new Polyline();

            GetObject getPolyline = new GetObject();

            getPolyline.SetCommandPrompt("외곽선,코어,랜딩 입력해주세요");
            getPolyline.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            getPolyline.GeometryAttributeFilter = GeometryAttributeFilter.ClosedCurve;
            getPolyline.SubObjectSelect = false;

            getPolyline.GetMultiple(3, 0);

            if (getPolyline.CommandResult() != Result.Success)
            {
                RhinoApp.WriteLine("달라고!");
                return getPolyline.CommandResult();
            }
            else
            { RhinoApp.WriteLine("입력받았습니다."); }

            if (null == getPolyline.Object(0).Curve())
            {
                RhinoApp.WriteLine("없잖아!");
                return getPolyline.CommandResult();
            }

            List<Polyline> testPoly = new List<Polyline>();
            foreach (var i in getPolyline.Objects())
            {
                testPoly.Add(CurveTools.ToPolyline(i.Curve()));
            }

            outline = testPoly[0];
            coreLine = testPoly[1];
            landing = testPoly[2];

            List<Polyline> rooms = Debugger.DebugRoom(outline, coreLine, landing);
            //List<Brep> breps = new List<Brep>();
            //foreach (Polyline i in rooms)
            //    breps.Add(Extrusion.Create(i.ToNurbsCurve(), -3, true).ToBrep());

            List<Polyline> corridor = Debugger.DebugCorridor(outline, coreLine, landing);
            //List<Brep> corridorBreps = new List<Brep>();
            //foreach (Polyline i in corridor)
            //    corridorBreps.Add(Extrusion.Create(i.ToNurbsCurve(), -0.01, true).ToBrep());

            Brep outlineBrep = Extrusion.Create(outline.ToNurbsCurve(), -0.01, true).ToBrep();
            Print(rooms, Color.LightGoldenrodYellow, doc);
            Print(corridor, Color.Turquoise, doc);
            doc.Views.Redraw();

            RhinoApp.WriteLine("최선을 다했습니다만...");

            return Result.Success;
        }
    }
}
