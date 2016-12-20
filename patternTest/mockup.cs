using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;


namespace patternTest
{
    public interface ICommander
    {
        void Excute();
    }

    public interface IInvoker
    {
        void SetCommand();
    }

    public class CorridorParamControl: ICommander
    {
        private ICorridorPattern executedCorridor;
        
        public CorridorParamControl(ICorridorPattern corridor)
        {
            this.executedCorridor = corridor;
        } 

        public void Excute()
        {
        }
    } 
}
