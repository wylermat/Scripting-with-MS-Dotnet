using ITVComponents.Scripting.CScript.Optimization;
using ValueType = ITVComponents.Scripting.CScript.ScriptValues.ValueType;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class IndexerScriptValue:ScriptValue
    {
        /// <summary>
        /// The ScriptValues that are used to execute an indexer.
        /// </summary>
        private ScriptValue[] values;

        /// <summary>
        /// The BaseValue on which to call the indexer
        /// </summary>
        private ScriptValue baseValue;

        /// <summary>
        /// Initializes a new instance of the IndexerScriptValue class
        /// </summary>
        /// <param name="handler">the handler object that is used to lock/unlock this item</param>
        /// <param name="creator">the creator-symbol that leads to this scriptvalue</param>
        public IndexerScriptValue(IScriptSymbol creator):base(creator) 
        {
        }

        /// <summary>
        /// Initializes a new instance of the IndexerScriptValue class
        /// </summary>
        /// <param name="baseValue">the base-property that provides the indexer-acces</param>
        /// <param name="values">the extended values for accessing the index</param>
        public void Initialize(ScriptValue baseValue, ScriptValue[] values)
        {
            baseValue.ValueType = ValueType.PropertyOrField;
            this.baseValue = baseValue;
            this.values = values;
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
            get { return baseValue.GetValue(values); }
        }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public override ValueType ValueType { get; set; }

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
            baseValue.SetValue(value, values);
        }
    }
}
