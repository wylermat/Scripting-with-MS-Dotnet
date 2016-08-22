using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Core.Methods
{
    internal class WritebackContainer
    {
        public int Index { get; set; }

        public ScriptValue Target { get; set; }
    }
}
