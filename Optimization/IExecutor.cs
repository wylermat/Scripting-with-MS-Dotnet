using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization
{
    public interface IExecutor
    {
        bool CanExecute(object value, ScriptValue[] arguments);

        object Invoke(object value, ScriptValue[] arguments);
    }
}
