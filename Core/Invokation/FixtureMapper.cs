using System.Collections.Generic;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;

namespace ITVComponents.Scripting.CScript.Core.Invokation
{
    /// <summary>
    /// Allows an object to override fixtures for external method calls with variables provided by a script
    /// </summary>
    public class FixtureMapper
    {
        /// <summary>
        /// the set of variables that is used to obtain the value of this mapper
        /// </summary>
        private IScope variables;

        /// <summary>
        /// the target name of this FixtureMapper instance
        /// </summary>
        private string targetName;

        /// <summary>
        /// Initializes a new instance of the FixtureMapper class
        /// </summary>
        /// <param name="variables">the variables provided by a caller</param>
        /// <param name="targetName">the targetname from which to take the value</param>
        public FixtureMapper(IScope variables, string targetName) : this()
        {
            this.variables = variables;
            this.targetName = targetName;
        }

        /// <summary>
        /// Prevents a default instance of the FixtureMap class from being created
        /// </summary>
        private FixtureMapper()
        {
        }

        /// <summary>
        /// Gets the value of this TargetMapper instance
        /// </summary>
        public object Value { get { return variables[targetName]; } }
    }
}
