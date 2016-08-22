using System;
using System.Runtime.Serialization;

namespace ITVComponents.Scripting.CScript.Exceptions
{
    [Serializable]
    public class ScriptException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ScriptException()
        {
        }

        public ScriptException(string message) : base(message)
        {
        }

        public ScriptException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
