using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ITVComponents.Scripting.CScript.Core.RuntimeSafety
{
    public interface IScope
    {
        object this[string memberName, bool rootOnly] { get; }

        object this[string memberName] { get; set; }

        bool ContainsKey(string key, bool rootOnly);

        /// <summary>
        /// Copies the initial root of the current scope
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> CopyInitial();

        /// <summary>
        /// Creates a Snapshot of the current Scope
        /// </summary>
        /// <returns>a dictionary that contains all variables that are in the current scope</returns>
        Dictionary<string, object> Snapshot();

        /// <summary>
        /// Opens an inner scope
        /// </summary>
        void OpenInnerScope();

        /// <summary>
        /// Collapses an inner scope
        /// </summary>
        void CollapseScope();

        /// <summary>
        /// Clears all elements and initializes this scope with new root values
        /// </summary>
        /// <param name="rootVariables">the root variables to put on the scope</param>
        void Clear(IDictionary<string, object> rootVariables);
    }
}
