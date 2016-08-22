using System;
using ITVComponents.Scripting.CScript.Optimization;
using ValueType = ITVComponents.Scripting.CScript.ScriptValues.ValueType;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class LiteralScriptValue: ScriptValue
    {
        private object value;

        /// <summary>
        /// Initializes a new instance of the LiteralScriptValue class
        /// </summary>
        public LiteralScriptValue():base(null) 
        {
        }

        public LiteralScriptValue(IScriptSymbol creator) : base(creator)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the LiteralScriptValue class
        /// </summary>
        /// <param name="value">the value of this ScriptValue object</param>
        public void Initialize(object value)
        {
            ValueType = ValueType.Literal;
            this.value = value;
        }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Writable
        /// </summary>
        public override bool Writable
        {
            get { return false; }
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
            get { return value; }
        }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public override sealed ValueType ValueType
        {
            get; set; }

        /// <summary>
        /// The Name of the Target object. This is only required for Methods
        /// </summary>
        protected override string Name
        {
            get { return null; }
        }

        /// <summary>
        /// Sets the Value of this ScriptValue object
        /// </summary>
        /// <param name="value">the new Value to assign to this Value</param>
        internal override void SetValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
