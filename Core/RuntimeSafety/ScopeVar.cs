using System;
#if !Community
using ITVComponents.ExtendedFormatting;
#endif

namespace ITVComponents.Scripting.CScript.Core.RuntimeSafety
{
    internal class ScopeVar
    {
        private Action<int> leaveLayer;
        private Action clear;
        private object value;
#if !Community
        private SmartProperty smartValue;
        private bool isSmart;
#endif

        public ScopeVar()
        {
            Layer = 0;
            Revision = -1;
        }

        public int Layer { get; set; }

        public int Revision { get; set; } 

        public object Value
        {
            get
            {
#if !Community
                return !isSmart?value:smartValue.Value;
#else
                return value;
#endif
            }
            set
            {
#if !Community
                if (!isSmart)
                {
#endif
                    this.value = value;
#if !Community
                    smartValue = value as SmartProperty;
                }
                else
                {
                    smartValue.Value = value;
                }
                isSmart = smartValue != null;
#endif
            }
        }
    }
}
