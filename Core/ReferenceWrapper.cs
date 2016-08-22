using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ITVComponents.Scripting.CScript.Core
{
    public class ReferenceWrapper:TypedNull
    {
        public object WrappedValue { get; private set; }

        public ReferenceWrapper Value()
        {
            return new ReferenceWrapper {Type = Type};
        }

        public ReferenceWrapper Value(object value)
        {
            return new ReferenceWrapper {Type = Type, WrappedValue = value};
        }
    }
}
