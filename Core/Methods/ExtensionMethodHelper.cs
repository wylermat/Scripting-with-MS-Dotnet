using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ITVComponents.Scripting.CScript.Core.Methods
{
    public class ExtensionMethod
    {
        private static List<ExtensionMethod> allExtensions = new List<ExtensionMethod>();

        private Type extensionAttrType = typeof(ExtensionAttribute);

        private MethodInfo theMethod;

        public Type ThisType { get; private set; }

        public ExtensionMethod(Type extenderType, string methodName, Type[] genericParameterTypes, Type[] argumentTypes)
        {
            if (!Attribute.IsDefined(extenderType, extensionAttrType))
            {
                throw new InvalidOperationException("Only Types providing Extension Method allowed!");
            }

            theMethod = GetMethod(extenderType, methodName, genericParameterTypes, argumentTypes);
            ThisType = theMethod.GetParameters().First().ParameterType;
            lock (allExtensions)
            {
                if (allExtensions.All(n => n.theMethod != theMethod))
                {
                    allExtensions.Add(this);
                }
            }
        }

        public static IEnumerable<MethodInfo> GetExtensions(Type type, string name, int argumentCount)
        {
            lock (allExtensions)
            {
                return
                    from i in
                        (from t in allExtensions select new {Separation = t.DegreeOfSeparationFrom(type), Extension = t})
                    where i.Separation != -1
                    orderby i.Separation
                    select i.Extension.theMethod;
            }
        }

        public int DegreeOfSeparationFrom(Type extendedType)
        {
            if (!ThisType.IsAssignableFrom(extendedType))
            {
                return -1;
            }

            if (ThisType.IsInterface)
            {
                return 0;
            }

            if (ThisType == extendedType)
            {
                return 0;
            }
            Type t = extendedType;
            int degree = 1;
            while (t != typeof(object))
            {
                t = t.BaseType;
                if (ThisType == t)
                {
                    return degree;
                }

                degree++;
            }

            return -1;
        }

        private MethodInfo GetMethod(Type type, string name, Type[] genericParameterTypes, Type[] argumentTypes)
        {
            bool genericMethod = genericParameterTypes != null && genericParameterTypes.Length != 0;
            MethodInfo[] possibleMethods =
                type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
            possibleMethods = (from t in possibleMethods
                               where
                                   t.Name == name&&
                                   t.IsGenericMethodDefinition == genericMethod
                               select t).ToArray();
            if (genericMethod)
            {
                possibleMethods =
                    (from t in possibleMethods
                     where t.GetGenericArguments().Length == genericParameterTypes.Length
                     select t.MakeGenericMethod(genericParameterTypes)).ToArray();
            }

            possibleMethods =
                (from t in possibleMethods
                 where argumentTypes == null || t.GetParameters().Length == argumentTypes.Length
                 select t).ToArray();
            if (possibleMethods.Length > 1 && (argumentTypes == null || argumentTypes.Length == 0))
            {
                throw new InvalidOperationException("Further Information required for accurate method recognition");
            }

            foreach (MethodInfo info in possibleMethods)
            {
                ParameterInfo[] par = info.GetParameters();
                bool ok = true;
                for (int i = 0; i < argumentTypes.Length; i++)
                {
                    if (!par[i].ParameterType.IsAssignableFrom(argumentTypes[i]))
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    return info;
                }
            }

            throw new InvalidOperationException("Unable to find the demanded Extensionmethod");
        }
    }
}
