using ITVComponents.Scripting.CScript.Optimization;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class Throw:LiteralScriptValue, IPassThroughValue
    {
        /// <summary>
        /// Initializes a new instance of the Throw class
        /// </summary>
        /// <param name="handler">the handler object that is used to lock/unlock values</param>
        public Throw()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Throw class
        /// </summary>
        /// <param name="value">the error that is being thrown</param>
        /// <param name="catchable">Indicates whether the thrown error can be catched by a script-Catch</param>
        public void Initialize(object value, bool catchable)
        {
            Initialize(value);
            Catchable = catchable;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this Error can be catched by a Script-Catch block
        /// </summary>
        public bool Catchable { get; private set; }
    }
}
