using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ITVComponents.Scripting.CScript.Core.ExternalMethods
{
    /// <summary>
    /// Attribute to mark methods as Callable by ExpressionParser
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExternalMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name as which the method will be mapped in the ExpressionParser. Default is the OriginalName
        /// </summary>
        public string MappedMethodName { get; set; }

        /// <summary>
        /// Gets methods marked with the ExternalMethodAttribute  from a specified type
        /// </summary>
        /// <param name="type">the type on which methods are marked as ExternalMethods</param>
        /// <returns>an array containing usable methods</returns>
        public static MethodInfo[] GetMethods(Type type, out string[] methodNames)
        {
            List<MethodInfo> retVal = new List<MethodInfo>();
            List<string> names = new List<string>();
            MethodInfo[] methods = type.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public);
            (from t in methods where t.GetCustomAttributes(typeof (ExternalMethodAttribute), true).Length != 0 select t)
                .ToList().ForEach(
                    (n) =>
                        {
                            Type[] t = n.GetGenericArguments();
                            if (t.Length != 0)
                            {
                                throw new InvalidOperationException("Generic methods are not supported!");
                            }

                            ExternalMethodAttribute ema =
                                n.GetCustomAttributes(typeof (ExternalMethodAttribute), true)[0] as ExternalMethodAttribute;
                            retVal.Add(n);
                            names.Add(string.IsNullOrEmpty(ema.MappedMethodName) ? n.Name : ema.MappedMethodName);
                        });
            methodNames = names.ToArray();
            return retVal.ToArray();
        }
    }
}
