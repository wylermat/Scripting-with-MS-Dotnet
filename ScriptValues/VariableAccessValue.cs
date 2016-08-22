using System.Collections.Generic;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class VariableAccessValue:ScriptValue
    {
        /// <summary>
        /// the name of the Variable that is accessible through this variableAccessValue object
        /// </summary>
        private string variableName;

        /// <summary>
        /// provides access to all current variables
        /// </summary>
        private IScope variables;

        /// <summary>
        /// Initializes a new instance of the VariableAccessValue class
        /// </summary>
        /// <param name="handler">the handler object that is used to lock/unlock this value object</param>
        public VariableAccessValue():base(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the VariableAccessValue class
        /// </summary>
        /// <param name="variables">the variables to access using this instance</param>
        /// <param name="variableName">the name of the accessed variable</param>
        public void Initialize(IScope variables, string variableName)
        {
            ValueType = ValueType.PropertyOrField;
            this.variables = variables;
            this.variableName = variableName;
        }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Writable
        /// </summary>
        public override bool Writable
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Getable
        /// </summary>
        public override bool Getable
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the Value of this ScriptValue
        /// </summary>
        protected override object Value
        {
            get
            {
                object retVal = null;
                if (variables.ContainsKey(variableName, false))
                {
                    retVal = variables[variableName];
                }
                return retVal;
            }
        }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public override ValueType ValueType
        {
            get; set; }

        /// <summary>
        /// The Name of the Target object. This is only required for Methods
        /// </summary>
        protected override string Name
        {
            get { return variableName; }
        }

        /// <summary>
        /// Sets the Value of this ScriptValue object
        /// </summary>
        /// <param name="value">the new Value to assign to this Value</param>
        internal override void SetValue(object value)
        {
            variables[variableName] = value;
        }
    }
}
