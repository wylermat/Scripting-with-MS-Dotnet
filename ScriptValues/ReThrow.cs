namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class ReThrow:Throw
    {
        public static readonly ReThrow Instance = new ReThrow();

        /// <summary>
        /// Initializes a new instance of the ReThrow class
        /// </summary>
        /// <param name="handler">the handler object that is used to lock/unlock items</param>
        private ReThrow()
        {
            Initialize(null, true);
        }
    }
}
