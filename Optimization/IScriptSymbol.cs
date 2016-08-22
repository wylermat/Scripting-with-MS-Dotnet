using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization
{
    public interface IScriptSymbol
    {
        void SetPreferredExecutor(IExecutor executor);

        object InvokeExecutor(object value, ScriptValue[] arguments, out bool success);
    }
}
