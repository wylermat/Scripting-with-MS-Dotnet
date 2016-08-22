using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !Community
using ITVComponents.Logging;
#endif
using ITVComponents.Scripting.CScript.Core.Invokation;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;

namespace ITVComponents.Scripting.CScript.Core.ExternalMethods
{
    /// <summary>
    /// Helper class for registering external method in ExpressionParser - Statements
    /// </summary>
    public static class ExternalMethodHelper
    {
        /// <summary>
        /// a list of methods accessible through the expression evaluation
        /// </summary>
        private static ConcurrentDictionary<string, MethodInfo> expressionMethods;

        /// <summary>
        /// Performs static initializations on the ExternalMethodHelper class
        /// </summary>
        static ExternalMethodHelper()
        {
            expressionMethods = new ConcurrentDictionary<string, MethodInfo>();
        }

        /// <summary>
        /// Registers public static 
        /// </summary>
        /// <param name="type">the type on which accessible methods are registered</param>
        public static void RegisterClass(Type type)
        {
            string[] names;
            MethodInfo[] methods = ExternalMethodAttribute.GetMethods(type, out names);
            for (int i = 0; i < names.Length; i++)
            {
                if (!expressionMethods.TryAdd(names[i], methods[i]))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Failed to register method {0} because the method has already been registered with the same signature",
                            names[i]));
                }
            }
        }

        /// <summary>
        /// Invokes a method and returns the result
        /// </summary>
        /// <param name="methodName">the name to invoke</param>
        /// <param name="arguments">arguments to pass to that method</param>
        /// <returns>the result of the invoked method</returns>
        public static object InvokeMethod(string methodName, object[] arguments)
        {
            if (!expressionMethods.ContainsKey(methodName))
            {
                throw new ArgumentException("provided method not found");
            }

            MethodInfo method = expressionMethods[methodName];
            object[] args;
            bool capable = MakeCapableMethodArguments(method, arguments, out args);
            if (!capable)
            {
                throw new ArgumentException("Method can not be called with given arguments");
            }

            return method.Invoke(null, args);
        }

        /// <summary>
        /// Returns a method that is capable to the provided arguments from a list of available methods
        /// </summary>
        /// <param name="methods">the methods that are callable</param>
        /// <param name="args">provided arguments for the methodcall</param>
        /// <param name="oargs">the calculated arguments if the lat parameter is a params argument</param>
        /// <returns>the methodinfo that is capable to be called with the given parameters</returns>
        public static MethodInfo GetCapableMethod(IList<MethodInfo> methods, object[] args, out object[] oargs)
        {
            Type[] types = GetTypeArray(args);
            MethodInfo retVal = (from t in methods where EqualSignatures(t, types) select t).FirstOrDefault();
            oargs = args;
            if (retVal == null)
            {
                oargs = null;
                foreach (MethodInfo m in methods)
                {
                    if (MakeCapableMethodArguments(m, args, out oargs))
                    {
                        retVal = m;
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Prepares a method to make it callable with default values by the ExpressionParser's methods
        /// </summary>
        /// <param name="variables">the variables that are used for the expressioncall</param>
        /// <param name="fixtures">the fixtures coming from the caller that must be used as default-parameters</param>
        public static void PrepareExpressionVariables(IScope variables, Dictionary<string, object> fixtures)
        {
            foreach (KeyValuePair<string, MethodInfo> method in expressionMethods)
            {
                if (!variables.ContainsKey(method.Key, false))
                {
                    InvokationHelper helper = DefaultParameterAttribute.GenerateHelper(method.Value, fixtures);
                    if (helper != null)
                    {
                        variables[method.Key]= helper;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a typearray that is also supporting null-values
        /// </summary>
        /// <param name="objects">objects that would be passed to a method call</param>
        /// <returns>an array of types capable for the methodcall</returns>
        internal static Type[] GetTypeArray(object[] objects)
        {
            Type[] retVal = new Type[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                Type t = null;
                if (objects[i] != null)
                {
                    t = objects[i].GetType();
                }

                retVal[i] = t;
            }

            return retVal;
        }

        /// <summary>
        /// Creates a capable argumentset for a method given
        /// </summary>
        /// <param name="method">the method to call</param>
        /// <param name="arguments">the arguments coming from the expressionparser</param>
        /// <param name="capableArguments">the arguments that can effectively be used for this method</param>
        /// <returns>a value indicating whether the method is callable</returns>
        private static bool MakeCapableMethodArguments(MethodInfo method, object[] arguments, out object[] capableArguments)
        {
             capableArguments = arguments;
            ParameterInfo[] parameters = method.GetParameters();
            if (arguments.Length >= parameters.Length - 1)
            {
                if (parameters.Length != 0)
                {
                    ParameterInfo last = parameters[parameters.Length - 1];
                    if (Attribute.IsDefined(last, typeof(ParamArrayAttribute)))
                    {
                        capableArguments = new object[parameters.Length];
                        Array.Copy(arguments, capableArguments, capableArguments.Length - 1);
                        capableArguments[capableArguments.Length - 1] = Array.CreateInstance(last.ParameterType.GetElementType(),
                                                                       arguments.Length - capableArguments.Length + 1);
                        if ((capableArguments[capableArguments.Length - 1] as Array).Length != 0)
                        {
                            try
                            {
                                Array.Copy(arguments, capableArguments.Length - 1, capableArguments[capableArguments.Length - 1] as Array, 0,
                                           (capableArguments[capableArguments.Length - 1] as Array).Length);
                            }
                            catch (Exception ex)
                            {
#if !Community
                                LogEnvironment.LogEvent(ex.ToString(), LogSeverity.Error);
#endif
                                return false;
                            }
                        }

                        Type[] types = GetTypeArray(capableArguments);
                        if (EqualSignatures(method, types))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a specific type-signature is capable for a specific method
        /// </summary>
        /// <param name="method">the method that is supposed to be called</param>
        /// <param name="parameterTypes">provided parameter types</param>
        /// <returns>a value indicating whether the method can be called with the given parameters</returns>
        private static bool EqualSignatures(MethodInfo method, Type[] parameterTypes)
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool retVal = parameters.Length == parameterTypes.Length;
            if (retVal)
            {
                for (int i = 0; i < parameters.Count() && retVal; i++)
                {
                    //bool rv = retVal;
                    retVal &= parameters[i].ParameterType == parameterTypes[i] || parameterTypes[i].IsSubclassOf(parameters[i].ParameterType);
                }
            }

            return retVal;
        }
    }
}
