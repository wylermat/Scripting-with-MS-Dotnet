using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Optimization
{
    public class ValueBuffer
    {
        public ValueBuffer()
        {
        }

        public T GetValue<T>() where T : ScriptValue , new()
        {
            return new T();
        }
    }
}
