using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;

namespace ITVComponents.Scripting.CScript.Helpers
{
    public class ScopePreparationCallbackArguments
    {
        private readonly IScope scope;
        private readonly IDisposable replSession;

        /// <summary>
        /// Initializes a new instance of the ScopePreparationCallbackArguments class
        /// </summary>
        /// <param name="scope">the variable scope that contains all current variables</param>
        /// <param name="replSession">a repl-session that is provides access to the current interpreter</param>
        public ScopePreparationCallbackArguments(IScope scope, IDisposable replSession)
        {
            this.scope = scope;
            this.replSession = replSession;
        }

        /// <summary>
        /// Gets the variableScope of the current repl session
        /// </summary>
        public IScope Scope { get { return scope;} }

        /// <summary>
        /// Gets the current repl session
        /// </summary>
        public IDisposable ReplSession { get {return replSession; } }
    }

    public delegate void InitializeScopeVariables(ScopePreparationCallbackArguments args);
}
