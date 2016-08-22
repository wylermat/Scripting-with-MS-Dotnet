using System.Text;
using Antlr4.Runtime;

namespace ITVComponents.Scripting.CScript
{
    internal class ErrorListener : IAntlrErrorListener<IToken>
    {
        private StringBuilder bld = new StringBuilder();

        public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg,
                                RecognitionException e)
        {
            string ems = "";
            if (e != null)
            {
                ems = e.Message;
            }
            bld.AppendFormat("Error at {0}/{1}: {2} ({3})\r\n", line, charPositionInLine, offendingSymbol.Text, ems);
        }

        /// <summary>
        /// Gets all error messages that were generated while parsing the provided expression
        /// </summary>
        /// <returns>all collected error messages</returns>
        public string GetAllErrors()
        {
            try
            {
                return bld.ToString();
            }
            finally
            {
                bld.Clear();
            }
        }
    }
}
