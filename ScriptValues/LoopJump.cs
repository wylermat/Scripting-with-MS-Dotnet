using System;
using ITVComponents.Scripting.CScript.Optimization;
using ValueType = ITVComponents.Scripting.CScript.ScriptValues.ValueType;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class LoopJump:ScriptValue, IPassThroughValue
    {
        /// <summary>
        /// Initializes a new instance of the LoopJump class
        /// </summary>
        public LoopJump():base(null)
        {
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
            get { return false; }
        }

        /// <summary>
        /// Gets the Value of this ScriptValue
        /// </summary>
        protected override object Value
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public override ValueType ValueType
        {
            get { return ValueType.Literal; }
            set { }
        }

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
