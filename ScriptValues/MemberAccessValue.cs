using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ITVComponents.Scripting.CScript.Core.Literals;
using ITVComponents.Scripting.CScript.Exceptions;
using ITVComponents.Scripting.CScript.Optimization;
using ValueType = ITVComponents.Scripting.CScript.ScriptValues.ValueType;

namespace ITVComponents.Scripting.CScript.ScriptValues
{
    public class MemberAccessValue:ScriptValue
    {
        /// <summary>
        /// the base value on which the defined member should be defined
        /// </summary>
        private ScriptValue baseValue;

        /// <summary>
        /// the name of the member that is represented by this scriptvalue
        /// </summary>
        private string memberName;

        /// <summary>
        /// Initializes a new instance of the MemberAccessValue class
        /// </summary>
        /// <param name="handler">the handler that is used to lock/unlock this value</param>
        public MemberAccessValue(IScriptSymbol creator):base(creator) 
        {
        }

        /// <summary>
        /// Initialiezs a new instance of the MemberAccessValue class
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="memberName"></param>
        public void Initialize(ScriptValue baseValue, string memberName)
        {
            this.baseValue = baseValue;
            this.memberName = memberName;
            ValueType = ValueType.PropertyOrField;
        }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Writable
        /// </summary>
        public override bool Writable
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this ScriptValue is Getable
        /// </summary>
        public override bool Getable
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the baseValue of this MemberAccess Value object
        /// </summary>
        protected object BaseValue { get { return baseValue.GetValue(null); } }

        /// <summary>
        /// Gets the Value of this ScriptValue
        /// </summary>
        protected override object Value
        {
            get
            {
                if (ValueType == ValueType.Method || ValueType == ValueType.Constructor)
                {
                    var bv = baseValue.GetValue(null);
                    var olt = bv as ObjectLiteral;
                    if (ValueType == ValueType.Constructor && olt != null)
                    {
                        return olt[Name];
                    }

                    return bv;
                }

                object targetObject;
                bool isEnum;
                MemberInfo mi = FindMember(out targetObject, out isEnum);
                ObjectLiteral ojl = targetObject as ObjectLiteral;
                if (ojl != null)
                {
                    return ojl[memberName];
                }

                if (isEnum)
                {
                    return Enum.Parse((Type) targetObject, memberName);
                }

                if (mi == null)
                {
                    throw new ScriptException(string.Format("Member {0} is not declared on {1}", memberName,
                                                            targetObject));
                }

                if (mi is PropertyInfo)
                {
                    PropertyInfo pi = (PropertyInfo) mi;
                    if (pi.CanRead)
                    {
                        return pi.GetValue(targetObject, null);
                    }

                    return null;
                }
                
                if (mi is FieldInfo)
                {
                    return ((FieldInfo) mi).GetValue(targetObject);
                }

                throw new ScriptException(string.Format("GetValue is not supported for MemberType {0}", mi.MemberType));
            }
        }

        /// <summary>
        /// Gets the Value Type of this ScriptValue
        /// </summary>
        public override sealed ValueType ValueType { get; set; }

        /// <summary>
        /// The Name of the Target object. This is only required for Methods
        /// </summary>
        protected override string Name
        {
            get { return memberName; }
        }

        /// <summary>
        /// Sets the Value of this ScriptValue object
        /// </summary>
        /// <param name="value">the new Value to assign to this Value</param>
        internal override void SetValue(object value)
        {
            if (ValueType != ValueType.PropertyOrField)
            {
                throw new ScriptException("Unable to set the value of something else than Property or Field");
            }

            object targetObject;
            bool isEnum;
            MemberInfo mi = FindMember(out targetObject, out isEnum);
            ObjectLiteral ojl = targetObject as ObjectLiteral;
            if (ojl != null)
            {
                ojl[memberName] = value;
                return;
            }
            if (mi == null)
            {
                throw new ScriptException(string.Format("Member {0} is not declared on {1}", memberName,
                                                        targetObject));
            }

            PropertyInfo pi;
            FieldInfo fi;
            if (mi is PropertyInfo && (pi=(PropertyInfo)mi).CanWrite)
            {
                pi.SetValue(targetObject, value, null);
            }
            else if (mi is FieldInfo && !(fi=(FieldInfo)mi).IsLiteral)
            {
                fi.SetValue(targetObject, value);
            }
            else if (mi is EventInfo && value is FunctionLiteral)
            {
                FunctionLiteral fl = value as FunctionLiteral;
                EventInfo ev = mi as EventInfo;
                ev.AddEventHandler(targetObject,fl.CreateDelegate(ev.EventHandlerType) );
            }
            else
            {
                throw new ScriptException(string.Format("SetValue is not supported for this Member ({0}", memberName));
            }
        }

        /// <summary>
        /// Finds the member with the given Name
        /// </summary>
        /// <param name="targetObject">the target object from which to read the value of the returned member</param>
        /// <param name="isEnum">indicates whether the base object is an enum type</param>
        /// <returns>a memberinfo that represents the name of this memberAccessValue object</returns>
        private MemberInfo FindMember(out object targetObject, out bool isEnum)
        {
            object baseVal = baseValue.GetValue(null);
            if (baseVal == null)
            {
                throw new ScriptException(string.Format("Unable to access {0} on a NULL - Value", memberName));
            }

            targetObject = baseVal;
            isEnum = false;
            bool isStatic = false;
            if (baseVal is Type)
            {
                targetObject = null;
                isStatic = true;
                if (((Type) baseVal).IsEnum)
                {
                    targetObject = baseVal;
                    isEnum = true;
                    return null;
                }
            }
            else
            {
                baseVal = baseVal.GetType();
            }

            Type t = (Type) baseVal;
            return (from m in t.GetMembers(BindingFlags.Public|(isStatic?BindingFlags.Static : BindingFlags.Instance)) where m.Name == memberName select m).FirstOrDefault();
        }
    }
}
