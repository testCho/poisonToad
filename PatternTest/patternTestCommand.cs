using System;
using System.Collections.Generic;
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
            get { return "patternTestCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
        }
    }
}
