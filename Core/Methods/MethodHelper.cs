//#define UseDelegates

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fasterflect;
using ITVComponents.Scripting.CScript.Core.Literals;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Core.Methods
{
    public static class MethodHelper
    {
        private static Dictionary<Type, TypeInformation> collectedMethodInformation = new Dictionary<Type, TypeInformation>();

        /// <summary>
        /// Returns a method that is capable to the provided arguments from a list of available methods
        /// </summary>
        /// <param name="type">the Type on which to call a method</param>
        /// <param name="typeArguments">the type arguments that are required for the method that is invoked</param>
        /// <param name="methodName">the target method name</param>
        /// <param name="staticMethod">indicates whether to find instance or static methods</param>
        /// <param name="arguments">provided arguments for the methodcall</param>
        /// <param name="oargs">the calculated arguments if the lat parameter is a params argument</param>
        /// <returns>the methodinfo that is capable to be called with the given parameters</returns>
#if UseDelegates
        public static MethodInvoker GetCapableMethod(Type type, Type[] typeArguments, string methodName, bool staticMethod, object[] arguments, out object[] oargs)
#else
        public static MethodInfo GetCapableMethod(Type type, Type[] typeArguments, string methodName, ref bool staticMethod, object[] arguments, out object[] oargs)
#endif
        {
            bool isNotGeneric = (typeArguments == null || typeArguments.Length == 0);
            TypeInformation info = GetTypeInfo(type);
            Dictionary<int, List<MethodBuffer>> typeMethods;
            if (!info.Methods.ContainsKey(methodName))
            {
                info.Methods.Add(methodName, new Dictionary<int, List<MethodBuffer>>());
            }

            typeMethods = info.Methods[methodName];
            if (!typeMethods.ContainsKey(arguments.Length))
            {
                typeMethods.Add(arguments.Length, new List<MethodBuffer>());
            }

            Type[] types = GetTypeArray(arguments);
            ICollection<MethodBuffer> methods =
                    typeMethods[arguments.Length].Where(n => (n.IsGeneric ^ isNotGeneric)).ToArray();

            object[] args = (from t in arguments select !(t is TypedNull) ? t : (t as ReferenceWrapper)?.WrappedValue).ToArray();
            var retVal = FindMethodFromArray(staticMethod, methods, types, args, out oargs);
            if (retVal == null)
            {
                methods = SelectGenerics(type.GetMethods((!staticMethod ? BindingFlags.Instance : BindingFlags.Static) |
                                                         BindingFlags.Public | BindingFlags.InvokeMethod)
                                             .Where(n => n.Name == methodName)
                                             .Where(
                                                 n =>
                                                 (n.ContainsGenericParameters ^
                                                   isNotGeneric)), typeArguments, false)
                    .ToArray();

                retVal = FindMethodFromArray(staticMethod, methods, types, args, out oargs);
                if (retVal != null && !retVal.IsGeneric)
                {
                    typeMethods[arguments.Length].Add(retVal);
                }
            }

            if (retVal == null)
            {
                methods = SelectGenerics(ExtensionMethod.GetExtensions(type, methodName, arguments.Length),
                    typeArguments, true);
                retVal = FindMethodFromArray(staticMethod, methods, types, args, out oargs);
                if (retVal != null)
                {
                    staticMethod = true;
                }
            }
            
#if UseDelegates
            return retVal.Delegate;
#else
            return retVal?.MethodInfo;
#endif
        }

        internal static WritebackContainer[] GetWritebacks(MethodInfo method, object[] evaluatedArguments, ScriptValue[] sequence)
        {
            bool[] isOut = (from t in method.GetParameters() select t.IsOut || t.ParameterType.IsByRef).ToArray();
            var writeBacks = (from t in isOut.Select((v, i) => new { Index = i, Value = v })
                              join r in evaluatedArguments.Select((v, i) => new { Index = i, Value = v }) on t.Index equals r.Index
                              join g in sequence.Select((v, i) => new { Index = i, Value = v }) on t.Index equals g.Index
                              where t.Value && g.Value.Writable
                              select new WritebackContainer{ Index=t.Index, Target = g.Value }).ToArray();
            return writeBacks;
        }

        /// <summary>
        /// Gets a capable Property info from the given type
        /// </summary>
        /// <param name="type">the type from which to get the index values</param>
        /// <param name="arguments">the arguments that were provided to the indexer</param>
        /// <param name="oargs">the correct args for the object arguments</param>
        /// <returns>a propertyInfo that fits the indexer</returns>
        public static PropertyInfo GetCapableIndexer(Type type, object[] arguments, out object[] oargs)
        {
            TypeInformation info = GetTypeInfo(type);
            if (!info.Indexers.ContainsKey(arguments.Length))
            {
                info.Indexers.Add(arguments.Length, new List<PropertyInfo>());
            }

            PropertyInfo[] indexers = info.Indexers[arguments.Length].ToArray();
            Type[] types = GetTypeArray(arguments);
            object[] args = (from t in arguments select !(t is TypedNull) ? t : (t as ReferenceWrapper)?.WrappedValue).ToArray();
            var retVal = FindIndexerFromArray(indexers, types, args, out oargs);
            if (retVal == null)
            {
                indexers = (from m in type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public) select m).ToArray();
                retVal = FindIndexerFromArray(indexers, types, args, out oargs);
                if (retVal != null)
                {
                    info.Indexers[arguments.Length].Add(retVal);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Gets a constructor that accepts the provided parameters
        /// </summary>
        /// <param name="type">the type for which to get a constructor</param>
        /// <param name="arguments">the provided arguments</param>
        /// <param name="oargs">the accepted arguments for the returned constructor</param>
        /// <returns>a ConstructorInfo object that will accept the given parameters</returns>
        public static ConstructorInfo GetCapableConstructor(Type type, object[] arguments, out object[] oargs)
        {
            TypeInformation info = GetTypeInfo(type);
            if (!info.Constructors.ContainsKey(arguments.Length))
            {
                info.Constructors.Add(arguments.Length, new List<ConstructorInfo>());
            }

            ConstructorInfo[] constructors = info.Constructors[arguments.Length].ToArray();
            Type[] types = GetTypeArray(arguments);
            object[] args = (from t in arguments select !(t is TypedNull) ? t : (t as ReferenceWrapper)?.WrappedValue).ToArray();
            var retVal = FindConstructorFromArray(constructors, types, args, out oargs);
            if (retVal == null)
            {
                constructors = type.GetConstructors();
                retVal = FindConstructorFromArray(constructors, types, args, out oargs);
                if (retVal != null)
                {
                    info.Constructors[arguments.Length].Add(retVal);
                }
            }

            return retVal;
        }


        public static MethodInvoker MakeDelegate(MethodInfo method)
        {
            return method.DelegateForCallMethod();
            Type t = null;
            List<Type> typs = new List<Type>();
            bool useRef = false;
            if (!method.IsStatic)
            {
                if (!method.DeclaringType.IsValueType)
                {
                    typs.Add(method.DeclaringType);
                }
                else
                {
                    typs.Add(method.DeclaringType);
                    useRef = true;
                }
            }

            typs.AddRange(from param in method.GetParameters() select param.ParameterType);
            bool isFunc = false;
            if (method.ReturnType != typeof(void))
            {
                typs.Add(method.ReturnType);
                isFunc = true;
            }

            if (typs.Count == 0 && !isFunc)
            {
                t = typeof(Action);
            }
            else if (typs.Count == 1)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof (Func<>) : typeof (Action<>);
                }
                else if (!isFunc)
                {
                    t = typeof (RefAction<>);
                }
            }
            else if (typs.Count == 2)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,>) : typeof(Action<,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,>) : typeof(RefAction<,>);
                }
            }
            else if (typs.Count == 3)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,>) : typeof(Action<,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,>) : typeof(RefAction<,,>);
                }
            }
            else if (typs.Count == 4)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,>) : typeof(Action<,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,>) : typeof(RefAction<,,,>);
                }
            }
            else if (typs.Count == 5)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,>) : typeof(Action<,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,>) : typeof(RefAction<,,,,>);
                }
            }
            else if (typs.Count == 6)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,>) : typeof(Action<,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,>) : typeof(RefAction<,,,,,>);
                }
            }
            else if (typs.Count == 7)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,>) : typeof(Action<,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,>) : typeof(RefAction<,,,,,,>);
                }
            }
            else if (typs.Count == 8)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,>) : typeof(Action<,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,>) : typeof(RefAction<,,,,,,,>);
                }
            }
            else if (typs.Count == 9)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,>) : typeof(Action<,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,>) : typeof(RefAction<,,,,,,,,>);
                }
            }
            else if (typs.Count == 10)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,>) : typeof(Action<,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,>);
                }
            }
            else if (typs.Count == 11)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 12)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 13)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 14)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 15)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 16)
            {
                if (!useRef)
                {
                    t = isFunc ? typeof(Func<,,,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,,,>);
                     }
                else
                {
                    t = isFunc ? typeof(RefFunc<,,,,,,,,,,,,,,,>) : typeof(RefAction<,,,,,,,,,,,,,,,>);
                }
            }
            else if (typs.Count == 17 && isFunc)
            {
                if (!useRef)
                {
                    t = typeof(Func<,,,,,,,,,,,,,,,,>);
                }
                else
                {
                    t = typeof(RefFunc<,,,,,,,,,,,,,,,,>);
                }
            }
            else
            {
                return null;
            }

            if (t.IsGenericTypeDefinition)
            {
                t = t.MakeGenericType(typs.ToArray());
            }

            try
            {
                //return Delegate.CreateDelegate(t, method);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a Type-Info object containing all known methods al indexers of the specified class
        /// </summary>
        /// <param name="type">the class for which to get the descriptor</param>
        /// <returns>a typeInformation object that contains the known part of the provided type</returns>
        private static TypeInformation GetTypeInfo(Type type)
        {
            if (!collectedMethodInformation.ContainsKey(type))
            {
                collectedMethodInformation.Add(type, new TypeInformation());
            }

            return collectedMethodInformation[type];
        }

        /// <summary>
        /// Finds the requested method from a list of provided methods
        /// </summary>
        /// <param name="staticMethod">indicates whether to look for a static method</param>
        /// <param name="oargs">the arguments to pass to the method</param>
        /// <param name="methods">the pre-filtered list of methods that might be capable</param>
        /// <param name="types">the parameter types for the method</param>
        /// <param name="args">the original arguments passed to the method</param>
        /// <returns>a methodinfo if one is found or null</returns>
        private static MethodBuffer FindMethodFromArray(bool staticMethod, ICollection<MethodBuffer> methods, Type[] types,
                                                      object[] args, out object[] oargs)
        {
            oargs = args;
            MethodBuffer retVal =
                methods.FirstOrDefault(
                    n => EqualSignatures(n.MethodInfo.GetParameters(), types, n.IsExtension) && (n.MethodInfo.IsStatic == (staticMethod^n.IsExtension)));
            if (retVal != null)
            {
                ParameterInfo[] parameters = retVal.MethodInfo.GetParameters();
#if (UseDelegates)
                oargs = EnrichWithOptionalParameters(oargs, parameters, retVal.ArgumentsRaw/*, staticMethod*/);
#else
                oargs = EnrichWithOptionalParameters(oargs, parameters, retVal.ArgumentsRaw);
#endif
            }
            else
            {
                foreach (MethodBuffer m in methods)
                {
#if UseDelegates
                    if (MakeCapableMethodArguments(m.MethodInfo.GetParameters(), types, args, m.ArgumentsRaw/*, staticMethod*/))
#else
                    if (MakeCapableMethodArguments(m.MethodInfo.GetParameters(), types, args, m.ArgumentsRaw,m.IsExtension))
#endif
                    {
                        oargs = m.ArgumentsRaw;
                        retVal = m;
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Finds the requested method from a list of provided constructors
        /// </summary>
        /// <param name="oargs">the arguments to pass to the method</param>
        /// <param name="constructors">the pre-filtered list of constructors that might be capable</param>
        /// <param name="types">the parameter types for the constructor</param>
        /// <param name="args">the original arguments passed to the constructor</param>
        /// <returns>a ConstructorInfo if one is found or null</returns>
        private static ConstructorInfo FindConstructorFromArray(ICollection<ConstructorInfo> constructors, Type[] types,
                                                      object[] args, out object[] oargs)
        {
            oargs = args;
            ConstructorInfo retVal =
                constructors.FirstOrDefault(
                    n => EqualSignatures(n.GetParameters(), types, false));
            if (retVal != null)
            {
                ParameterInfo[] parameters = retVal.GetParameters();
                oargs = EnrichWithOptionalParameters(oargs, parameters, new object[parameters.Length]);
            }
            else
            {
                foreach (ConstructorInfo m in constructors)
                {
                    var prm = m.GetParameters();
                    oargs = new object[prm.Length];
                    if (MakeCapableMethodArguments(prm, types, args, oargs,false))
                    {
                        retVal = m;
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Finds the requested Indexer from a list of provided properties
        /// </summary>
        /// <param name="oargs">the arguments to pass to the indexer</param>
        /// <param name="properties">the pre-filtered list of properties that might be capable</param>
        /// <param name="types">the parameter types for the indexer</param>
        /// <param name="args">the original arguments passed to the indexer</param>
        /// <returns>a PropertyInfo if one is found or null</returns>
        private static PropertyInfo FindIndexerFromArray(ICollection<PropertyInfo> properties, Type[] types,
                                                      object[] args, out object[] oargs)
        {
            oargs = args;
            PropertyInfo retVal =
                properties.FirstOrDefault(
                    n => EqualSignatures(n.GetIndexParameters(), types, false));
            if (retVal != null)
            {
                ParameterInfo[] parameters = retVal.GetIndexParameters();
                oargs = EnrichWithOptionalParameters(oargs, parameters, new object[parameters.Length]);
            }
            else
            {
                foreach (PropertyInfo m in properties)
                {
                    var prm = m.GetIndexParameters();
                    oargs = new object[prm.Length];
                    if (MakeCapableMethodArguments(m.GetIndexParameters(), types, args, oargs,false))
                    {
                        retVal = m;
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Alters the list of parameters so that it fits the required parameters. If a parameters is optional, the default value is added
        /// </summary>
        /// <param name="oargs">the passed arguments from the script</param>
        /// <param name="parameters">the parameters that are expected by the method</param>
        /// <returns>a arguments-array that fits the required parameters</returns>
#if UseDelegates
        private static object[] EnrichWithOptionalParameters(object[] oargs, ParameterInfo[] parameters, object[] rawArgs/*, bool isStatic = true*/)
#else
        private static object[] EnrichWithOptionalParameters(object[] oargs, ParameterInfo[] parameters, object[] rawArgs)
#endif
        {
#if UseDelegates
            Array.Copy(oargs, 0, rawArgs, 0/*isStatic ? 0 : 1*/, oargs.Length);
            if (oargs.Length < rawArgs.Length /*- (isStatic ? 0 : 1)*/)
#else
            Array.Copy(oargs, 0, rawArgs, 0, oargs.Length);
            if (oargs.Length < rawArgs.Length)
#endif
            {
                for (int i = oargs.Length; i < parameters.Length; i++)
                {
                    if (parameters[i].IsOptional && !(Attribute.IsDefined(parameters[i], typeof (ParamArrayAttribute))))
                    {
#if (UseDelegates)
                        rawArgs[i /*+ (isStatic ? 0 : 1)*/] = parameters[i].DefaultValue;
#else
                        rawArgs[i] = parameters[i].DefaultValue;
#endif
                    }
                }
            }

            return rawArgs;
        }

        /// <summary>
        /// Creates a capable argumentset for a method given
        /// </summary>
        /// <param name="parameters">the parameters that are expected by the requesting method or property</param>
        /// <param name="args">the arguments coming from the expressionparser</param>
        /// <param name="capableArguments">the arguments that can effectively be used for this method</param>
        /// <returns>a value indicating whether the method is callable</returns>
#if UseDelegates
        private static bool MakeCapableMethodArguments(ParameterInfo[] parameters, Type[] originTypes, object[] arguments, object[] capableArguments/*, bool isStatic=true*/)
#else
            private static bool MakeCapableMethodArguments(ParameterInfo[] parameters, Type[] originTypes, object[] arguments, object[] capableArguments, bool isExtensionMethod)
#endif
        {
            int extensionAddition = (isExtensionMethod ? 1 : 0);
            //ParameterInfo[] parameters = method.GetParameters();
            if (arguments.Length >= parameters.Length - 1 - extensionAddition)
            {
                if (parameters.Length != extensionAddition)
                {
                    ParameterInfo last = parameters[parameters.Length - 1];
                    if (Attribute.IsDefined(last, typeof (ParamArrayAttribute)))
                    {
                        Array paramArray;
                        //capableArguments = new object[parameters.Length];
#if (UseDelegates)
                        Array.Copy(arguments, 0, capableArguments, /*isStatic ? 0 : 1*/0, parameters.Length-1);
                        //Array.Copy(arguments, capableArguments, capableArguments.Length - 1);
                        if (capableArguments[capableArguments.Length - 1] == null ||
                            (paramArray = (Array) capableArguments[capableArguments.Length - 1]).Length !=
                            arguments.Length - capableArguments.Length + /*(isStatic ? 1 : 2)*/1)
                        {
                            paramArray = Array.CreateInstance(last.ParameterType.GetElementType(),
                                                     arguments.Length - capableArguments.Length + /*(isStatic ? 1 : 2)*/1);
                            capableArguments[capableArguments.Length - 1] = paramArray;

                        }
#else
                        Array.Copy(arguments, 0, capableArguments, extensionAddition, parameters.Length-1- extensionAddition);
                        //Array.Copy(arguments, capableArguments, capableArguments.Length - 1);
                        if (capableArguments[capableArguments.Length - 1] == null ||
                            (paramArray=(Array) capableArguments[capableArguments.Length - 1]).Length !=
                            arguments.Length - capableArguments.Length + 1 - extensionAddition)
                        {
                            paramArray = Array.CreateInstance(last.ParameterType.GetElementType(),
                                                              arguments.Length - capableArguments.Length + 1 - extensionAddition);
                            capableArguments[capableArguments.Length - 1] = paramArray;

                        }
#endif
                        if (paramArray.Length != 0)
                        {
                            try
                            {
                                Array.Copy(arguments, parameters.Length-1-extensionAddition,
                                           paramArray, 0,
                                           paramArray.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                return false;
                            }
                        }

#if (UseDelegates)
                        Type[] types = new Type[capableArguments.Length /*- (isStatic ? 0 : 1)*/];
                        for (int i = /*isStatic ? 0 : 1*/0, a=0; i < capableArguments.Length; i++,a++)
                        {
                            types[a] = capableArguments[i] != null ? capableArguments[i].GetType() : originTypes[a];
                        }
#else
                        Type[] types = new Type[capableArguments.Length];
                        for (int i = 0; i < capableArguments.Length; i++)
                        {
                            types[i] = capableArguments[i] != null ? capableArguments[i].GetType() : originTypes[i];
                        }
#endif
                        if (EqualSignatures(parameters, types, false))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static MethodBuffer[] SelectGenerics(IEnumerable<MethodInfo> methods, Type[] typeArguments, bool extensions)
        {
            return methods.Select(n =>
                                      {
#if UseDelegates
                MethodBuffer mi = new MethodBuffer {MethodInfo = n, IsGeneric=n.ContainsGenericParameters, ArgumentsRaw = new object[n.GetParameters().Length/*+(n.IsStatic?0:1)*/]};
#else
                                          MethodBuffer mi = new MethodBuffer
                                          {
                                              MethodInfo = n,
                                              IsGeneric = n.ContainsGenericParameters,
                                              ArgumentsRaw =
                                                  new object[n.GetParameters().Length + (!extensions ? 0 : 1)],
                                              IsExtension = extensions
                                          };
#endif
                if (mi.IsGeneric && typeArguments != null)
                {
                    try
                    {
                        mi.MethodInfo = n.MakeGenericMethod(typeArguments);
                        Console.WriteLine(mi.MethodInfo.Name);
                        //mi.IsGeneric = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }
#if UseDelegates
                mi.Delegate = MakeDelegate(mi.MethodInfo);//Delegate.CreateDelegate(typeof(Dummy), mi.MethodInfo);
                                          if (mi.Delegate != null)
                                          {
                                              return mi;
                                          }

                                          return null;
#else
                                          return mi;
#endif
                                      }).Where(n => n != null).ToArray();
        }

        /// <summary>
        /// Gets a typearray that is also supporting null-values
        /// </summary>
        /// <param name="objects">objects that would be passed to a method call</param>
        /// <returns>an array of types capable for the methodcall</returns>
        private static Type[] GetTypeArray(object[] objects)
        {
            Type[] retVal = new Type[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                Type t = null;
                if (objects[i] != null)
                {
                    if (!(objects[i] is TypedNull))
                    {
                        t = objects[i].GetType();
                    }
                    else
                    {
                        t = ((TypedNull) objects[i]).Type;
                    }
                }

                retVal[i] = t;
            }

            return retVal;
        }

        /// <summary>
        /// Determines whether a specific type-signature is capable for a specific method
        /// </summary>
        /// <param name="parameters">the parameters of the method that is supposed to be called</param>
        /// <param name="parameterTypes">provided parameter types</param>
        /// <returns>a value indicating whether the method can be called with the given parameters</returns>
        private static bool EqualSignatures(ParameterInfo[] parameters, Type[] parameterTypes, bool isExtensionMethod)
        {
            int max = parameters.Length;
            int extensionAddition = (isExtensionMethod ? 1 : 0);
            bool retVal = parameters.Length == parameterTypes.Length+extensionAddition;
            if (!retVal && parameters.Length > parameterTypes.Length + extensionAddition)
            {
                max = parameterTypes.Length;
                retVal = true;
                for (int i = parameterTypes.Length+extensionAddition; i < parameters.Length; i++)
                {
                    retVal &= (parameters[i].IsOptional);
                }
            }

            if (retVal)
            {
                for (int i = 0, a=extensionAddition; i < max && retVal; i++, a++)
                {
                    //bool rv = retVal;
                    retVal &= ((parameterTypes[i] == typeof(ObjectLiteral) && parameters[a].ParameterType.IsInterface) ||
                               (parameterTypes[i] == typeof(FunctionLiteral) && typeof(Delegate).IsAssignableFrom(parameters[a].ParameterType)) ||
                               (parameterTypes[i] == null && !parameters[a].ParameterType.IsValueType) ||
                               parameters[a].ParameterType.IsAssignableFrom(parameterTypes[i]));
                    /*if (rv && !retVal)
                    {
                        /*Console.WriteLine("RetVal changed to false when trying to cast {0} to {1}", parameters[i].ParameterType,
                                          parameterTypes[i]);*/
                    //}
                }
            }

            return retVal;
        }

        private class TypeInformation
        {
            public Dictionary<string, Dictionary<int, List<MethodBuffer>>> Methods =
                new Dictionary<string, Dictionary<int, List<MethodBuffer>>>();

            public Dictionary<int, List<ConstructorInfo>> Constructors = new Dictionary<int, List<ConstructorInfo>>();

            public Dictionary<int, List<PropertyInfo>> Indexers = new Dictionary<int, List<PropertyInfo>>();
        }

        private class MethodBuffer
        {
            public MethodInfo MethodInfo{get; set;}

            public bool IsGeneric { get; set; }

            public object[] ArgumentsRaw { get; set; }

            public bool IsExtension { get; set; }

#if UseDelegates
            public MethodInvoker Delegate { get; set; }
#endif
        }
    }

    public delegate void RefAction<T>(ref T arg);
    public delegate void RefAction<T0, in T1>(ref T0 arg0, T1 arg1);
    public delegate void RefAction<T0, in T1, in T2>(ref T0 arg0, T1 arg1, T2 arg2);
    public delegate void RefAction<T0, in T1, in T2, in T3>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate void RefAction<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    //public delegate void RefAction<T0, in T1,in T2,in T3,in T4,in T5,in T6,in T7,in T8,in T9,in T10,in T11,in T12,in T13,in T14,in T15,T16>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    public delegate TRet RefFunc<T, out TRet>(ref T arg);
    public delegate TRet RefFunc<T0, in T1, out TRet>(ref T0 arg0, T1 arg1);
    public delegate TRet RefFunc<T0, in T1, in T2, out TRet>(ref T0 arg0, T1 arg1, T2 arg2);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    public delegate TRet RefFunc<T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16, out TRet>(ref T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    

}
