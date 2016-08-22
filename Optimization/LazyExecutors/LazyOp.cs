using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization.LazyExecutors
{
    internal class LazyOp:IExecutor
    {
        private Func<object, object, bool, object> invoker;
        private bool typeSave;
        public LazyOp(Func<object, object, bool, object> invoker, bool typeSave)
        {
            this.invoker = invoker;
            this.typeSave = typeSave;
        }

        #region Implementation of IExecutor

        public bool CanExecute(object value, ScriptValue[] arguments)
        {
            return value == null && arguments.Length == 2;
        }

        public object Invoke(object value, ScriptValue[] arguments)
        {
            return invoker(arguments[0].GetValue(null), arguments[1].GetValue(null), typeSave);
        }

        #endregion
    }
}
