using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Fasterflect;
using ITVComponents.Scripting.CScript.Core.Methods;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization.LazyExecutors
{
    internal class LazyMethod:LazyInvoke
    {
        private readonly MethodInfo method;
        private readonly bool isStatic;
        private readonly bool isExtension;

        internal LazyMethod(MethodInfo method, bool isStatic, bool isExtension, bool lastParams) : base((from t in method.GetParameters() select t.ParameterType).ToArray(), lastParams)
        {
            this.method = method;
            this.isStatic = isStatic;
            this.isExtension = isExtension;
        }

        #region Overrides of LazyInvoke

        public override bool CanExecute(object value, ScriptValue[] arguments)
        {
            int diff = !isExtension ? 0 : 1;
            return ((((arguments[1] as SequenceValue)?.Sequence?.Length ?? 0) + diff) == types.Length) || lastParams;
        }

        public override object Invoke(object value, ScriptValue[] arguments)
        {
            ScriptValue[] args = ((SequenceValue) arguments[1]).Sequence;
            object[] raw = (from t in args select t.GetValue(null)).ToArray();
            if (isExtension)
            {
                raw = new[] {value}.Concat(raw).ToArray();
            }

            if (isStatic)
            {
                value = null;
            }

            object[] cargs = TranslateParams(raw);
            var writeBacks = MethodHelper.GetWritebacks(method, cargs, args);
            object retVal;
            try
            {
                retVal = method.Invoke(value, cargs);
            }
            finally
            {
                foreach (var container in writeBacks)
                {
                    container.Target.SetValue(cargs[container.Index]);
                }
            }

            return retVal;
        }

        #endregion
    }
}
