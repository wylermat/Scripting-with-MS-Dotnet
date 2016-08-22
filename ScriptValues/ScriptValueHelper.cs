using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.Core.Literals;
using ITVComponents.Scripting.CScript.Exceptions;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    internal static class ScriptValueHelper
    {
        public static T GetScriptValueResult<T>(ScriptValue value, bool alwaysReturn) 
        {
            if (value is Throw)
            {
                object error = value.GetValue(null);
                if (error is Exception)
                {
                    throw new ScriptException("Error while executing Script", (Exception)error);
                }

                throw new ScriptException(error.ToString());
            }

            if ((value is IPassThroughValue && !(value is ReturnValue))||!(alwaysReturn || value is ReturnValue))
            {
                return default(T);
            }

            object tmp = value.GetValue(null);
            ObjectLiteral olit = tmp as ObjectLiteral;
            Type t = typeof(T);
            if (olit != null && t.IsInterface)
            {
                tmp = olit.Cast(t);
            }
            return (T) tmp;
        }
    }
}
