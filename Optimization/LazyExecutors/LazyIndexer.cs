using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization.LazyExecutors
{
    internal class LazyIndexer:LazyInvoke
    {
        private PropertyInfo indexProperty;

        public LazyIndexer(PropertyInfo indexProperty, bool lastParams):base((from t in indexProperty.GetIndexParameters() select t.ParameterType).ToArray(), lastParams)
        {
            this.indexProperty = indexProperty;
        }

        #region Overrides of LazyInvoke

        public override bool CanExecute(object value, ScriptValue[] arguments)
        {
            return value != null && (arguments.Length == types.Length || lastParams);
        }

        public override object Invoke(object value, ScriptValue[] arguments)
        {
            return indexProperty.GetValue(value, (from t in arguments select t.GetValue(null)).ToArray());
        }

        #endregion
    }
}
