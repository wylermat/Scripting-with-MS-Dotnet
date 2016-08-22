//#define UseDelegates

using System;
using System.Linq;
using System.Reflection;
using ITVComponents.Scripting.CScript.Core;
using ITVComponents.Scripting.CScript.Core.Invokation;
using ITVComponents.Scripting.CScript.Core.Literals;
using ITVComponents.Scripting.CScript.Core.Methods;
using ITVComponents.Scripting.CScript.Exceptions;
using ITVComponents.Scripting.CScript.Optimization;
using ITVComponents.Scripting.CScript.Optimization.LazyExecutors;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public abstract class ScriptValue
    {
        /// <summary>
        /// The Creator symbol that leads to this ScriptValue
        /// </summary>
        private IScriptSymbol creator;

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Writable
        /// </summary>
        public abstract bool Writable { get; }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Getable
        /// </summary>
        public abstract bool Getable { get; }

        /// <summary>
        /// Gets the Value of this ScriptValue
        /// </summary>
        protected abstract object Value { get; }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public abstract ValueType ValueType { get; set; }

        /// <summary>
        /// The Name of the Target object. This is only required for Methods
        /// </summary>
        protected abstract string Name { get; }

        /// <summary>
        /// Initializes a new instance of the ScriptValue class
        /// </summary>
        public ScriptValue(IScriptSymbol creator)
        {
            this.creator = creator;
        }

        /// <summary>
        /// Gets the effective Value of this ScriptValue instance
        /// </summary>
        /// <param name="arguments">indexer/method/constructor arguments</param>
        /// <returns>an object that represents the value of this ScriptValue</returns>
        public virtual object GetValue(ScriptValue[] arguments)
        {
            if (!Getable)
            {
                throw new ScriptException("Get is not supported for this member");
            }

            if (creator != null)
            {
                bool ok;
                object retVal = creator.InvokeExecutor(Value, arguments, out ok);
                if (ok)
                {
                    return retVal;
                }
            }

            if (ValueType == ValueType.Literal)
            {
                return Value;
            }

            object tmpValue = Value;
            if (ValueType == ValueType.PropertyOrField)
            {
                if (arguments == null || arguments.Length == 0)
                {
                    if (tmpValue is InvokationHelper)
                    {
                        return ((InvokationHelper)tmpValue).Invoke(null);
                    }

                    return tmpValue;
                }
                if (tmpValue == null)
                {
                    throw new ScriptException("Indexer Failed for NULL - Value");
                }

                if (tmpValue is Type)
                {
                    throw new ScriptException("Indexer call for Types not supported");
                }

                object[] parameters = (from t in arguments select t.GetValue(null)).ToArray();
                if (!(tmpValue is Array))
                {
                    object[] args;
                    PropertyInfo pi = MethodHelper.GetCapableIndexer(tmpValue.GetType(),
                                                                     parameters,
                                                                     out args);
                    if (pi == null)
                    {
                        throw new ScriptException("No capable Indexer found for the provided arguments");
                    }

                    if (creator != null)
                    {
                        creator.SetPreferredExecutor(new LazyIndexer(pi,parameters.Length != args.Length));
                    }

                    return pi.GetValue(tmpValue, args);
                }

                Array arr = (Array)tmpValue;
                return arr.GetValue(parameters.Cast<int>().ToArray());
            }

            if (ValueType == ValueType.Method)
            {
                SequenceValue ta = (SequenceValue) arguments[0];
                SequenceValue a = (SequenceValue) arguments[1];
                object[] parameters = (from t in a.Sequence select t.GetValue(null)).ToArray();
                Type[] typeParameters = Type.EmptyTypes;
                if (ta != null)
                {
                    typeParameters = (from t in ta.Sequence select (Type) t.GetValue(null)).ToArray();
                }

                Type type;
                
                if (tmpValue == null)
                {
                    ValueType = ValueType.PropertyOrField;
                }

                try
                {
                    if (tmpValue is Delegate)
                    {
                        Delegate dlg = (Delegate)tmpValue;
                        return dlg.DynamicInvoke(parameters);
                    }

                    if (tmpValue is InvokationHelper)
                    {
                        InvokationHelper ih = (InvokationHelper)tmpValue;
                        return ih.Invoke(parameters);
                    }

                    if (tmpValue is FunctionLiteral)
                    {
                        FunctionLiteral fl = (FunctionLiteral)tmpValue;
                        return fl.Invoke(parameters);
                    }
                }
                finally
                {
                    ValueType = ValueType.Method;
                }

                if (tmpValue == null)
                {
                    throw new Exception("Method call failed for NULL - Value");
                }

                object target = tmpValue;
                bool isStatic = false;
                if (tmpValue is Type)
                {
                    type = (Type)tmpValue;
                    target = null;
                    isStatic = true;
                }
                else if (tmpValue is ObjectLiteral)
                {
                    type = tmpValue.GetType();
                    ObjectLiteral ol = tmpValue as ObjectLiteral;
                    FunctionLiteral fl = ol[Name] as FunctionLiteral;
                    if (fl != null)
                    {
                        return fl.Invoke(parameters);
                    }
                }
                else
                {
                    type = tmpValue.GetType();
                }

                object[] args;
#if UseDelegates
                MethodInvoker method = MethodHelper.GetCapableMethod(type, typeParameters, Name, Value is Type, parameters, out args);
#else
                bool tmpStatic = isStatic;
                MethodInfo method = MethodHelper.GetCapableMethod(type, typeParameters, Name, ref isStatic, parameters,
                                                                  out args);
                if (!tmpStatic && isStatic)
                {
                    args[0] = target;
                    target = null;
                }
#endif
                if (method == null)
                {
                    throw new ScriptException(string.Format("No capable Method found for {0}", Name));
                }

                var writeBacks = MethodHelper.GetWritebacks(method, args, a.Sequence);
                if (creator != null)
                {
                    creator.SetPreferredExecutor(new LazyMethod(method, tmpStatic, !tmpStatic && isStatic, args.Length != a.Sequence.Length));
                }
#if UseDelegates
                if (target != null)
                {
                    target = target.WrapIfValueType();
                }

                return method(target, args);
                if (target != null)
                {

                    /*object[] newArgs = new object[args.Length + 1];
                    newArgs[0] = target;
                    Array.Copy(args, 0, newArgs, 1, args.Length);*/
                    args[0] = target;
                    if (!(target is System.ValueType))
                    {
                        return method.FastDynamicInvoke(args);
                    }

                    return method.DynamicInvoke(args);
#else
                try
                {
                    return method.Invoke(target, args);
                }
                finally
                {
                    foreach (var wb in writeBacks)
                    {
                        wb.Target.SetValue(args[wb.Index]);
                    }
                }
#endif
#if UseDelegates
                }


                return method.FastDynamicInvoke(args);
#endif
            }

            if (ValueType == ValueType.Constructor)
            {
                if (tmpValue == null || !(tmpValue is Type))
                {
                    throw new ScriptException("Require Type in order to create a new instance");
                }

                ScriptValue[] ta = null;
                if (arguments[0] != null)
                {
                    ta = ((SequenceValue)arguments[0]).Sequence;
                }

                ScriptValue[] a = ((SequenceValue) arguments[1]).Sequence;
                object[] parameters = (from t in a select t.GetValue(null)).ToArray();
                Type[] typeParameters = ta == null
                                            ? Type.EmptyTypes
                                            : (from t in ta select (Type) t.GetValue(null)).ToArray();
                Type type = (Type)tmpValue;
                if (typeParameters.Length != 0)
                {
                    //throw new ScriptException(string.Format("Unexpected usage of generic Type {0}", ((Type)Value).FullName));
                    type = type.MakeGenericType(typeParameters);
                }

                object[] args;
                ConstructorInfo constructor = MethodHelper.GetCapableConstructor(type, parameters, out args);
                if (constructor == null)
                {
                    throw new ScriptException(string.Format("No appropriate Constructor was found for {0}",
                                                            ((Type)tmpValue).FullName));
                }

                if (creator != null)
                {
                    creator.SetPreferredExecutor(new LazyConstructor(constructor, args.Length != a.Length));
                }

                return constructor.Invoke(args);
            }

            throw new ScriptException("Unexpected Value-Type");
        }

        /// <summary>
        /// Sets the Value of this ScriptValue object
        /// </summary>
        /// <param name="value">the Value to assign to this ScriptValue</param>
        /// <param name="arguments">the indexer arguments, if required</param>
        public virtual void SetValue(object value, ScriptValue[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                if (!Writable)
                {
                    throw new ScriptException("Set is not supported for this Member");
                }

                SetValue(value);
            }
            else
            {
                object tmpValue = Value;
                if (tmpValue == null)
                {
                    throw new ScriptException("Indexer Failed for NULL - Value");
                }

                if (tmpValue is Type)
                {
                    throw new ScriptException("Indexer call for Types not supported");
                }

                object[] parameters = (from t in arguments select t.GetValue(null)).ToArray();
                if (!(tmpValue is Array))
                {
                    object[] args;
                    PropertyInfo pi = MethodHelper.GetCapableIndexer(tmpValue.GetType(),
                                                                     parameters,
                                                                     out args);
                    if (pi == null)
                    {
                        throw new ScriptException("No capable Indexer found for the provided arguments");
                    }

                    if (!pi.CanWrite)
                    {
                        throw new ScriptException("Set is not supported for this Indexer");
                    }

                    pi.SetValue(tmpValue, value, args);
                }
                else
                {
                    Array arr = (Array)tmpValue;
                    arr.SetValue(value,parameters.Cast<int>().ToArray());
                }
            }
        }

        /// <summary>
        /// Sets the Value of this ScriptValue object
        /// </summary>
        /// <param name="value">the new Value to assign to this Value</param>
        internal abstract void SetValue(object value);
    }

    /// <summary>
    /// Enum for the Supported Value Types in a Script
    /// </summary>
    public enum ValueType
    {
        /// <summary>
        /// The value is literal and can not be assigned
        /// </summary>
        Literal,

        /// <summary>
        /// The Value is a Property and can be read and written
        /// </summary>
        PropertyOrField,

        /// <summary>
        /// The Value is a Method and can only be read
        /// </summary>
        Method,

        /// <summary>
        /// The Value is a Constructor and can only be read
        /// </summary>
        Constructor
    }
}
