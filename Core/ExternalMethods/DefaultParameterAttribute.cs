using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fasterflect;
using ITVComponents.Scripting.CScript.Core.Invokation;
using ITVComponents.Scripting.CScript.Core.Methods;

namespace ITVComponents.Scripting.CScript.Core.ExternalMethods
{
    public class DefaultParameterAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the defined fixture that must be provided by a caller to make this method callable
        /// </summary>
        public string FixtureName { get; set; }

        /// <summary>
        /// Generates InvokationHelper object with parameters that have been provided by the expression caller
        /// </summary>
        /// <param name="method">the method that needs to be callable from the objectevaluator in the expressionparser</param>
        /// <param name="fixtures">fixtures provided by the caller</param>
        /// <returns>the invokationHelper object that can be passed to the evaluator to call the underlaying method</returns>
        public static InvokationHelper GenerateHelper(MethodInfo method, Dictionary<string, object> fixtures)
        {
            MethodInvoker d = MethodHelper.MakeDelegate(method);
            object missing = new object();
            object[] parameters = (from DefaultParameterAttribute att in
                                       (from param in method.GetParameters()
                                        where Attribute.IsDefined(param, typeof (DefaultParameterAttribute))
                                        orderby param.Position
                                        select
                                            Attribute.GetCustomAttribute(param,
                                                                         typeof (DefaultParameterAttribute)))
                                   select fixtures.ContainsKey(att.FixtureName) ? fixtures[att.FixtureName] : missing)
                .ToArray();
            if (Array.IndexOf(parameters, missing) == -1)
            {
                return new InvokationHelper(d, parameters);
            }

            return null;
        }
    }
}
