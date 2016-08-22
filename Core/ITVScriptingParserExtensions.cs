using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.Optimization;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Core
{
    public partial class ITVScriptingParser
    {
        public partial class MemberDotExpressionContext:IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class MemberIndexExpressionContext:IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class MemberDotQExpressionContext:IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class AssignmentOperatorExpressionContext : IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class MultiplicativeExpressionContext : IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class AdditiveExpressionContext : IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        public partial class RelationalExpressionContext : IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, ScriptValue[] arguments, out bool success)
            {
                success = preferredExecutor != null && preferredExecutor.CanExecute(value, arguments);
                if (success)
                {
                    return preferredExecutor.Invoke(value, arguments);
                }

                return null;
            }
        }

        /*public partial class NewExpressionContext : IScriptSymbol
        {
            private IExecutor preferredExecutor;

            public void SetPreferredExecutor(IExecutor executor)
            {
                preferredExecutor = executor;
            }

            public object InvokeExecutor(object value, object[] arguments, out bool success)
            {
                success = preferredExecutor.CanExecute(value, arguments);
                return preferredExecutor.Invoke(value, arguments);
            }
        }*/
    }
}
