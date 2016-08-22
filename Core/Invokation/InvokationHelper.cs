using System.Collections.Generic;
using System.Linq;
using Fasterflect;

namespace ITVComponents.Scripting.CScript.Core.Invokation
{
    public class InvokationHelper
    {
        /// <summary>
        /// the Delegate which is used to be invoked
        /// </summary>
        private MethodInvoker dlg;

        /// <summary>
        /// Default parameters being passed to the delegate on invokation
        /// </summary>
        private object[] defaultParameters;

        /// <summary>
        /// Initializes a new instance of the InvokationHelper class
        /// </summary>
        /// <param name="dlg">the delegate that can be invoked from somewhere</param>
        /// <param name="defaultParameters">default arguments being passed to the delegate on execution</param>
        public InvokationHelper(MethodInvoker dlg, object[] defaultParameters)
        {
            this.dlg = dlg;
            this.defaultParameters = defaultParameters;
        }

        /// <summary>
        /// Prevents a default instance of the InvokationHelper class from being created
        /// </summary>
        private InvokationHelper()
        {
        }

        /// <summary>
        /// Invokes the underlaying method with the provided defaults and given additional parameters
        /// </summary>
        /// <param name="additionalParameters">the additional parameters that are supported by the method</param>
        /// <returns>the result of the invokation of the method</returns>
        public object Invoke(params object[] additionalParameters)
        {
            object[] args = (from t in defaultParameters select (!(t is FixtureMapper))?t:(t as FixtureMapper).Value).ToArray() ;
            if (additionalParameters != null && additionalParameters.Length != 0)
            {
                List<object> l = new List<object>();
                l.AddRange(defaultParameters);
                l.AddRange(additionalParameters);
                args = l.ToArray();
            }

            return dlg(null, args);
        }
    }
}
