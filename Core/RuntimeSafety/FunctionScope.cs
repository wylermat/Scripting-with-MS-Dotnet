using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ITVComponents.Scripting.CScript.Core.RuntimeSafety
{
    public class FunctionScope:IScope
    {
        private Scope innerScope;

        private IScope parentScope;

        private Dictionary<string, object> initialScope;

        public FunctionScope(Dictionary<string, object> initialScope)
        {
            this.initialScope = initialScope;
            innerScope = new Scope();
        }
        #region Implementation of IScope

        object IScope.this[string memberName, bool rootOnly]
        {
            get { return GetValue(memberName, rootOnly); }
        }

        object IScope.this[string memberName]
        {
            get { return GetValue(memberName, false); }
            set {
                if (parentScope?.ContainsKey(memberName, false)??false)
                {
                    parentScope[memberName] = value;
                    return;
                }

                innerScope[memberName] = value;
            }
        }

        public IScope ParentScope { get { return parentScope; } set { parentScope = value; } }

        public bool ContainsKey(string key, bool rootOnly)
        {
            return (parentScope?.ContainsKey(key, true)??false) || innerScope.ContainsKey(key, rootOnly);
        }

        public Dictionary<string, object> CopyInitial()
        {
            return innerScope.CopyInitial();
        }

        public Dictionary<string, object> Snapshot()
        {
            return innerScope.Snapshot();
        }

        public void OpenInnerScope()
        {
            innerScope.OpenInnerScope();
        }

        public void CollapseScope()
        {
            innerScope.CollapseScope();
        }

        public void Clear(IDictionary<string, object> rootVariables)
        {
            innerScope.Clear(initialScope);
            if (rootVariables != null)
            {
                foreach (KeyValuePair<string, object> item in rootVariables)
                {
                    innerScope[item.Key] = item.Value;
                }
            }
        }

        private object GetValue(string memberName, bool rootOnly)
        {
            if (parentScope?.ContainsKey(memberName, true)??false)
            {
                return parentScope[memberName, true];
            }

            if (innerScope.ContainsKey(memberName, rootOnly))
            {
                return innerScope[memberName, rootOnly];
            }

            return null;
        }

        #endregion
    }
}
