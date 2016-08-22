using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Tree;

namespace ITVComponents.Scripting.CScript.Core
{
    public partial class ITVScriptingBaseVisitor<Result> 
    {
        /// <summary>
        /// Gets a value indicating whether this Scriptor supports reactivation
        /// </summary>
        public bool Reactivateable { get; protected set; } = true;
    }
}
