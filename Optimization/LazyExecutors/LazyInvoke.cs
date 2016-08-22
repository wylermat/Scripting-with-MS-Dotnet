using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.Core;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization.LazyExecutors
{
    internal abstract class LazyInvoke:IExecutor
    {
        protected readonly Type[] types;

        protected readonly bool lastParams;

        public LazyInvoke(Type[] types, bool lastParams)
        {
            this.types = types;
            this.lastParams = lastParams;
        }

        public abstract bool CanExecute(object value, ScriptValue[] arguments);

        public abstract object Invoke(object value, ScriptValue[] arguments);

        protected object[] TranslateParams(object[] parameters)
        {
            int diff = parameters.Length - types.Length + 1;
            object[] retVal = new object[types.Length];
            for (int i = 0; i < types.Length - ((!lastParams) ? 0 : 1); i++)
            {
                if (!(parameters[i] is TypedNull))
                {
                    retVal[i] = parameters[i];
                }
                else
                {
                    retVal[i] = (parameters[i] as ReferenceWrapper)?.WrappedValue;
                }
            }

            if (lastParams)
            { 
                Array pargs = Array.CreateInstance(types[types.Length - 1].GetElementType(), diff);
                for (int i = 0, a = types.Length - 1; a < parameters.Length; i++,a++)
                {
                    pargs.SetValue(parameters[a], i);
                }

                retVal[retVal.Length - 1] = pargs;
            }

            return retVal;
        }
    }
}
