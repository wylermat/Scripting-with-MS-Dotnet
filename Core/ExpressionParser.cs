using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using ITVComponents.Scripting.CScript.Buffering;
using ITVComponents.Scripting.CScript.Exceptions;
using ITVComponents.Scripting.CScript.ScriptValues;

namespace ITVComponents.Scripting.CScript.Core
{
    public static class ExpressionParser
    {
        /// <summary>
        /// Holds all previously parsed expressions
        /// </summary>
        private static Dictionary<string, ITVScriptingParser.SingleExpressionContext> parsedExpressions = new Dictionary<string, ITVScriptingParser.SingleExpressionContext>();

        /// <summary>
        /// Holds all blocks that were evaluated before
        /// </summary>
        private static Dictionary<string, ITVScriptingParser.ProgramContext> parsedPrograms = new Dictionary<string, ITVScriptingParser.ProgramContext>();

        /// <summary>
        /// the error listener that is used to provide errors on expressions
        /// </summary>
        private static ErrorListener listener = new ErrorListener();

        /// <summary>
        /// Parses one single Expression
        /// </summary>
        /// <param name="expression">the target expression to execute</param>
        /// <param name="variables">the initial variables that are provided to the expression</param>
        /// <returns>the evaluation result</returns>
        public static object Parse(string expression, IDictionary<string, object> variables)
        {
            ScriptVisitor visitor;
            using (var session = InterpreterBuffer.GetReplInstance(variables, out visitor))
            {
                return Parse(expression, session);
            }
        }

        /// <summary>
        /// Parses one single Expression-Block
        /// </summary>
        /// <param name="expression">the target expression to execute</param>
        /// <param name="variables">the initial variables that are provided to the expression</param>
        /// <returns>the evaluation result</returns>
        public static object ParseBlock(string expression, IDictionary<string, object> variables)
        {
            ScriptVisitor visitor;
            using (var session = InterpreterBuffer.GetReplInstance(variables, out visitor))
            {
                return ParseBlock(expression, session);
            }
        }

        /// <summary>
        /// Parses a Block inside an open ReplSession
        /// </summary>
        /// <param name="expression">the Expression-Block that must be executed</param>
        /// <param name="replSession">the current repl-session</param>
        /// <returns>the result of the Execution-block</returns>
        public static object ParseBlock(string expression, IDisposable replSession)
        {
            ITVScriptingBaseVisitor<ScriptValue> visitor = InterpreterBuffer.GetInterpreter(replSession);
            ITVScriptingParser.ProgramContext executor = GetProgramTree(expression);
            ScriptValue retVal = visitor.VisitProgram(executor);
            return ScriptValueHelper.GetScriptValueResult<object>(retVal,false);
        }

        /// <summary>
        /// Parses an expression for a repl-session
        /// </summary>
        /// <param name="expression">the expression to parse</param>
        /// <param name="replSession">the repl-session that is currently running</param>
        /// <returns>the result of the provided expression</returns>
        public static object Parse(string expression, IDisposable replSession)
        {
            ITVScriptingBaseVisitor<ScriptValue> visitor = InterpreterBuffer.GetInterpreter(replSession);
            ITVScriptingParser.SingleExpressionContext executor = GetExpressionTree(expression);
            ScriptValue retVal =  visitor.Visit(executor);
            return ScriptValueHelper.GetScriptValueResult<object>(retVal,true);
        }

        /// <summary>
        /// Begins a repl - session
        /// </summary>
        /// <param name="baseValues">the base values that are used for the current session</param>
        /// <returns>a value that can be used to end this repl - session</returns>
        public static IDisposable BeginRepl(IDictionary<string, object> baseValues)
        {
            ScriptVisitor visitor;
            return InterpreterBuffer.GetReplInstance(baseValues, out visitor);
        }

        /// <summary>
        /// Gets the ExpressionTree for a specific Expression
        /// </summary>
        /// <param name="expression">the expression for which to get the tree</param>
        /// <returns>a ScriptParser containing the entire ExpressionTree for the provided expression</returns>
        private static ITVScriptingParser.SingleExpressionContext GetExpressionTree(string expression)
        {
            lock (parsedExpressions)
            {
                if (!parsedExpressions.ContainsKey(expression))
                {
                    var lex = new ITVScriptingLexer(new AntlrInputStream(expression));
                    var parser = new ITVScriptingParser(new CommonTokenStream(lex));
                    parser.AddErrorListener(listener);
                    var singleExpression = parser.singleExpression();
                    if (parser.NumberOfSyntaxErrors == 0)
                    {
                        parsedExpressions.Add(expression, singleExpression);
                    }
                    else
                    {
                        throw new ScriptException(listener.GetAllErrors());
                    }
                }

                return parsedExpressions[expression];
            }
        }

        /// <summary>
        /// Gets the ExpressionTree for a specific Expression
        /// </summary>
        /// <param name="expression">the expression for which to get the tree</param>
        /// <returns>a ScriptParser containing the entire ExpressionTree for the provided expression</returns>
        private static ITVScriptingParser.ProgramContext GetProgramTree(string expression)
        {
            lock (parsedPrograms)
            {
                if (!parsedPrograms.ContainsKey(expression))
                {
                    var lex = new ITVScriptingLexer(new AntlrInputStream(expression));
                    var parser = new ITVScriptingParser(new CommonTokenStream(lex));
                    parser.AddErrorListener(listener);
                    var singleExpression = parser.program();
                    if (parser.NumberOfSyntaxErrors == 0)
                    {
                        parsedPrograms.Add(expression, singleExpression);
                    }
                    else
                    {
                        throw new ScriptException(listener.GetAllErrors());
                    }
                }

                return parsedPrograms[expression];
            }
        }
    }
}
