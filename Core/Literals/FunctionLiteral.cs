using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ITVComponents.Scripting.CScript.Core.RuntimeSafety;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Core.Literals
{
    public class FunctionLiteral : DynamicObject
    {
        /// <summary>
        /// The Scope of this function
        /// </summary>
        private FunctionScope scope;

        /// <summary>
        /// The Script visitor that is used to interpret this method
        /// </summary>
        private ScriptVisitor visitor;

        /// <summary>
        /// The method names that are expected by this method
        /// </summary>
        private string[] arguments;

        /// <summary>
        /// The functionbody of this method
        /// </summary>
        private ITVScriptingParser.FunctionBodyContext body;

        /// <summary>
        /// The initialValues that are surrounding this functiondefinition
        /// </summary>
        private Dictionary<string, object> initialValues;

        /// <summary>
        /// Initializes a new instance of the FunctionLiteral class
        /// </summary>
        /// <param name="values">the local values that are surrounding the method at the moment of creation</param>
        /// <param name="parent">the parent scope of this method</param>
        /// <param name="arguments">the argument names that are passed to this method</param>
        /// <param name="body">the method body of this method</param>
        public FunctionLiteral(Dictionary<string, object> values, string[] arguments,
            ITVScriptingParser.FunctionBodyContext body)
        {
            initialValues = values;
            scope = new FunctionScope(values);
            visitor = new ScriptVisitor(scope);
            this.arguments = arguments;
            this.body = body;
        }

        public IScope ParentScope { get { return scope.ParentScope; } set { scope.ParentScope = value; } }

        /// <summary>Stellt die Implementierung für Typkonvertierungsvorgänge bereit.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Operationen anzugeben, die ein Objekt von einem Typ in einen anderen konvertieren.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine sprachspezifische Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zur Konvertierungsoperation bereit.Die binder.Type-Eigenschaft stellt den Typ bereit, in den das Objekt konvertiert werden muss.Für die Anweisung (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), bei der sampleObject eine Instanz der von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleiteten Klasse ist, gibt binder.Type z. B. den <see cref="T:System.String" />-Typ zurück.Die binder.Explicit-Eigenschaft stellt Informationen zur Art der ausgeführten Konvertierung bereit.Für die explizite Konvertierung wird true und für die implizite Konvertierung wird false zurückgegeben.</param>
        /// <param name="result">Das Ergebnis des Typkonvertierungsvorgangs.</param>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (!binder.Type.IsSubclassOf(typeof(Delegate)))
            {
                result = null;
                return false;
            }

            result = CreateDelegate(binder.Type);
            return true;
        }

        /// <summary>Stellt die Implementierung für Vorgänge bereit, die ein Objekt aufrufen.Von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitete Klassen können diese Methode überschreiben, um dynamisches Verhalten für Vorgänge wie das Aufrufen eines Objekts oder Delegaten anzugeben.</summary>
        /// <returns>true, wenn der Vorgang erfolgreich ist, andernfalls false.Wenn die Methode false zurückgibt, wird das Verhalten vom Laufzeitbinder der Sprache bestimmt.(In den meisten Fällen wird eine sprachspezifische Laufzeitausnahme ausgelöst.)</returns>
        /// <param name="binder">Stellt Informationen zum Aufrufvorgang bereit.</param>
        /// <param name="args">Die Argumente, die während des Aufrufvorgangs an das Objekt übergeben werden.Für den sampleObject(100)-Vorgang, in dem sampleObject von der <see cref="T:System.Dynamic.DynamicObject" />-Klasse abgeleitet ist, entspricht <paramref name="args[0]" /> z. B. 100.</param>
        /// <param name="result">Das Ergebnis des Objektaufrufs.</param>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = Invoke(args);
            return true;
        }

        /// <summary>
        /// Invokes the body of this function using the provided arguments
        /// </summary>
        /// <param name="arguments">the arguments whith which to initialize the visitor for this method</param>
        /// <returns>the result of this method</returns>
        public object Invoke(object[] arguments)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            for (int i = 0; i < this.arguments.Length; i++)
            {
                object val = i < arguments.Length ? arguments[i] : null;
                parameters[this.arguments[i]] = val;
            }

            parameters["parameters"] = arguments;
            scope.Clear(parameters);
            return ScriptValueHelper.GetScriptValueResult<object>(visitor.Visit(body), false);
        }

        /// <summary>
        /// Creates a copy with a new scope of this functionLiteral
        /// </summary>
        /// <returns></returns>
        public FunctionLiteral Copy()
        {
            return new FunctionLiteral(initialValues, arguments, body);
        }

        /// <summary>
        /// Creates an eventhandler for the specified event info
        /// </summary>
        /// <param name="delegateType">the event information for the subscribed event</param>
        /// <returns>a delegate that raises a generic event providing all required information used to distribute the event to clients</returns>
        public Delegate CreateDelegate(Type delegateType)
        {
            Funk d = Invoke;
            Type method = delegateType;
            ParameterInfo[] methodParameters = method.GetMethod("Invoke").GetParameters();
            List<string> names = new List<string>();
            int a = 0;
            ParameterExpression[] parameters =
                methodParameters.Select(n => Expression.Parameter(n.ParameterType, string.Format("arg{0}", a++)))
                    .ToArray();
            names.AddRange(parameters.Select(n => n.Name));
            NewArrayExpression array = Expression.NewArrayInit(typeof(object), parameters);
            LambdaExpression lambda =
                Expression.Lambda(
                    Expression.Call(Expression.Constant(d), d.GetType().GetMethod("Invoke"), array), parameters);
            Delegate tmp = lambda.Compile();
            return Delegate.CreateDelegate(method, tmp, "Invoke", false);
        }

        /// <summary>
        /// a temporary delegate type that is used as bridge between an eventhandler and the invoke method
        /// </summary>
        /// <param name="arguments">the arguments to pass to the Invoke method</param>
        /// <returns>the result of this function</returns>
        private delegate object Funk(object[] arguments);
    }
}
