using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using ImpromptuInterface;
using ITVComponents.Scripting.CScript.Core.Invokation;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;

namespace ITVComponents.Scripting.CScript.Core.Literals
{
    public class ObjectLiteral : DynamicObject, IScope
    {
        private IScope parent;
        private Dictionary<string, object> values;

        public ObjectLiteral(Dictionary<string, object> values, IScope parent)
        {
            this.values = values;
            this.parent = parent;
        }

        object IScope.this[string memberName, bool rootOnly] { get { return this[memberName]; } }

        /// <summary>
        /// Gets or sets the value with the specified name of this object
        /// </summary>
        /// <param name="name">the name of which the name is being set on this object</param>
        /// <returns>the value that is bound to the given name</returns>
        public object this[string name]
        {
            get
            {
                if (name == "this")
                {
                    return this;
                }

                if (values.ContainsKey(name))
                {
                    return values[name];
                }

                return null;
            }
            set
            {
                if (name == "this")
                {
                    throw new InvalidOperationException("Unable to set the value of this!");
                }
                values[name] = value;
                var literal = value as ObjectLiteral;
                if (literal != null)
                {
                    literal.parent = this;
                }
            }
        }

        public bool ContainsKey(string key, bool rootOnly)
        {
            bool retVal = key == "this";
            if (!retVal)
            {
                retVal = values.ContainsKey(key);
            }

            if (!retVal)
            {
                retVal = parent.ContainsKey(key, true);
            }

            return retVal;
        }

        public Dictionary<string, object> CopyInitial()
        {
            return new Dictionary<string, object>(values);
        }

        public Dictionary<string, object> Snapshot()
        {
            return new Dictionary<string, object>(values);
        }

        public void OpenInnerScope()
        {
        }

        public void CollapseScope()
        {
        }

        public void Clear(IDictionary<string, object> rootVariables)
        {
        }


        /// <summary>Gibt die Enumeration aller dynamischen Membernamen zurück. </summary>
        /// <returns>Eine Sequenz, die dynamische Membernamen enthält.</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return values.Keys;
        }

        /// <summary>
        /// Casts this object to an instance of the demanded type as long as it is an interface
        /// </summary>
        ///// <param name="targetType">the target interface type</param>
        /// <returns>the representation of the given interface wrapping this instance</returns>
        public object Cast(Type targetType)
        {
            if (targetType.IsInterface)
            {
                return Impromptu.DynamicActLike(this, targetType);
            }

            return null;
        }

        /// <summary>
        /// Casts this object to an instance of the demanded type as long as it is an interface
        /// </summary>
        /// <typeparam name="T">the target interface type</typeparam>
        /// <returns>the representation of the given interface wrapping this instance</returns>
        public T Cast<T>() where T : class
        {
            if (typeof(T).IsInterface)
            {
                return this.ActLike<T>();
            }

            return default(T);
        }

        /// <summary>Stellt die Implementierung für Vorgänge bereit, die Memberwerte abrufen.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Vorgänge wie das Abrufen eines Werts für eine Eigenschaft anzugeben.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zum Objekt bereit, das den dynamischen Vorgang aufgerufen hat.Die binder.Name-Eigenschaft gibt den Namen des Members an, für den der dynamische Vorgang ausgeführt wird.Für die Console.WriteLine(sampleObject.SampleProperty)-Anweisung, in der sampleObject eine von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Instanz der Klasse ist, gibt binder.Name z. B. "SampleProperty" zurück.Die binder.IgnoreCase-Eigenschaft gibt an, ob der Membername die Groß-/Kleinschreibung berücksichtigt.</param>
        /// <param name="result">Das Ergebnis des get-Vorgangs.Wenn die Methode z. B. für eine Eigenschaft aufgerufen wird, können Sie <paramref name="result" /> den Eigenschaftswert zuweisen.</param>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool retVal = values.ContainsKey(binder.Name);
            result = retVal ? values[binder.Name] : null;
            return retVal;
        }

        /// <summary>Stellt die Implementierung für Typkonvertierungsvorgänge bereit.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Operationen anzugeben, die ein Objekt von einem Typ in einen anderen konvertieren.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine sprachspezifische Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zur Konvertierungsoperation bereit.Die binder.Type-Eigenschaft stellt den Typ bereit, in den das Objekt konvertiert werden muss.Für die Anweisung (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), bei der sampleObject eine Instanz der von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleiteten Klasse ist, gibt binder.Type z. B. den <see cref="T:System.String" />-Typ zurück.Die binder.Explicit-Eigenschaft stellt Informationen zur Art der ausgeführten Konvertierung bereit.Für die explizite Konvertierung wird true und für die implizite Konvertierung wird false zurückgegeben.</param>
        /// <param name="result">Das Ergebnis des Typkonvertierungsvorgangs.</param>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type.IsInterface)
            {
                result = Impromptu.DynamicActLike(this, binder.Type);
                return true;
            }

            result = this;
            return false;
        }

        /// <summary>Stellt die Implementierung für Vorgänge bereit, die Memberwerte festlegen.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Vorgänge wie das Festlegen eines Werts für eine Eigenschaft anzugeben.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine sprachspezifische Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zum Objekt bereit, das den dynamischen Vorgang aufgerufen hat.Die binder.Name-Eigenschaft gibt den Namen des Members an, dem der Wert zugewiesen wird.Für die Anweisung sampleObject.SampleProperty = "Test", in der sampleObject eine von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Instanz der Klasse ist, gibt binder.Name z. B. "SampleProperty" zurück.Die binder.IgnoreCase-Eigenschaft gibt an, ob der Membername die Groß-/Kleinschreibung berücksichtigt.</param>
        /// <param name="value">Der Wert, der auf den Member festgelegt werden soll.Für die sampleObject.SampleProperty = "Test"-Anweisung, in der sampleObject eine von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Instanz der Klasse ist, ist <paramref name="value" /> z. B. "Test".</param>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            values[binder.Name] = value;
            return true;
        }

        /// <summary>Stellt die Implementierung für Vorgänge bereit, die einen Member aufrufen.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Vorgänge wie das Aufrufen einer Methode anzugeben.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine sprachspezifische Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zum dynamischen Vorgang bereit.Die binder.Name-Eigenschaft gibt den Namen des Members an, für den der dynamische Vorgang ausgeführt wird.Für die Anweisung sampleObject.SampleMethod(100), in der sampleObject eine von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Instanz der Klasse ist, gibt binder.Name z. B. "SampleMethod" zurück.Die binder.IgnoreCase-Eigenschaft gibt an, ob der Membername die Groß-/Kleinschreibung berücksichtigt.</param>
        /// <param name="args">Die Argumente, die während des Aufrufvorgangs an den Objektmember übergeben werden.Für die Anweisung sampleObject.SampleMethod(100), in der sampleObject von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitet ist, entspricht <paramref name="args[0]" /> z. B. 100.</param>
        /// <param name="result">Das Ergebnis des Memberaufrufs.</param>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (this[binder.Name] is FunctionLiteral)
            {
                result = ((FunctionLiteral) this[binder.Name]).Invoke(args);
                return true;
            }

            if (this[binder.Name] is InvokationHelper)
            {
                result = ((InvokationHelper) this[binder.Name]).Invoke(args);
                return true;
            }

            if (this[binder.Name] is Delegate)
            {
                result = ((Delegate) this[binder.Name]).DynamicInvoke(args);
                return true;
            }

            result = null;
            return false;
        }
    }
}
