using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization.LazyExecutors
{
    internal class LazyConstructor:LazyInvoke
    {
        private ConstructorInfo constructor;

        public LazyConstructor(ConstructorInfo constructor, bool lastParams) : base((from c in constructor.GetParameters() select c.ParameterType).ToArray(), lastParams)
        {
            this.constructor = constructor;
        }

        #region Overrides of LazyInvoke

        public override bool CanExecute(object value, ScriptValue[] arguments)
        {
            return (((arguments[1] as SequenceValue)?.Sequence?.Length ?? 0 ) == types.Length) || lastParams;
        }

        public override object Invoke(object value, ScriptValue[] arguments)
        {
            object[] args = TranslateParams((from a in arguments select a.GetValue(null)).ToArray());
            return constructor.Invoke(args);
        }

        #endregion
    }
}
