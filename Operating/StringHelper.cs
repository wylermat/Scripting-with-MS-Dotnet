using System;
using System.Text.RegularExpressions;
using ITVComponents.Scripting.CScript.Exceptions;

namespace ITVComponents.Scripting.CScript.Operating
{
    public static class StringHelper
    {
        public static string Parse(string rawString)
        {
            if (rawString.StartsWith("@\""))
            {
                return rawString.Substring(2, rawString.Length - 3).Replace("\"\"", "\"");
            }

            if (!rawString.StartsWith("\""))
            {
                throw new ScriptException("Invalid String");
            }

            string retVal = "";
            bool escaping = false;
            for (int i = 1; i < rawString.Length - 1; i++)
            {
                switch (rawString[i])
                {
                    case '\\':
                        {
                            escaping = !escaping;
                            if (!escaping)
                            {
                                retVal += "\\";
                            }

                            break;
                        }
                    case '"':
                        {
                            if (escaping)
                            {
                                retVal += "\"";
                                escaping = false;
                            }
                            else
                            {
                                throw new Exception("Unexpected quote in string");
                            }

                            break;
                        }
                    default:
                        {
                            if (!escaping)
                            {
                                retVal += rawString[i];
                            }
                            else
                            {
                                retVal += Escape(rawString, ref i);
                                escaping = false;
                            }

                            break;
                        }
                }
            }

            return retVal;
        }

        private static string Escape(string rawVal, ref int id)
        {
            string retVal = "";
            switch (rawVal[id])
            {
                case 'a':
                case 'A':
                    {
                        retVal = "\a";
                        break;
                    }
                case 'b':
                case 'B':
                    {
                        retVal = "\b";
                        break;
                    }
                case 'f':
                case 'F':
                    {
                        retVal = "\f";
                        break;
                    }
                case 'n':
                case 'N':
                    {
                        retVal = "\n";
                        break;
                    }
                case 'r':
                case 'R':
                    {
                        retVal = "\r";
                        break;
                    }
                case 't':
                case 'T':
                    {
                        retVal = "\t";
                        break;
                    }
                case 'u':
                    {
                        if (rawVal[id + 1] != ':')
                        {
                            throw new Exception("invalid escape sequence");
                        }

                        id++;
                        int val = Convert.ToInt32(rawVal.Substring(id + 1, 4), 16);
                        retVal = ((char) val).ToString();
                        id += 4;
                        break;
                    }
                case 'x':
                    {
                        if (rawVal[id + 1] != ':')
                        {
                            throw new Exception("invalid escape sequence");
                        }

                        id++;
                        string ss = rawVal.Substring(id + 1, 4);
                        Match m = Regex.Match(ss, "[0-9a-fA-F]*",
                                              RegexOptions.Compiled | RegexOptions.CultureInvariant |
                                              RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
                                              RegexOptions.Multiline);
                        if (!m.Success)
                        {
                            throw new Exception("Invalid escape sequence");
                        }

                        int val = Convert.ToInt32(m.Value, 16);
                        retVal = ((char) val).ToString();
                        id += m.Length;
                        break;
                    }
                default:
                    {
                        string ss = rawVal.Substring(id, 3);
                        Match m = Regex.Match(ss, "[0-7]*",
                                              RegexOptions.Compiled | RegexOptions.CultureInvariant |
                                              RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
                                              RegexOptions.Multiline);
                        int val = Convert.ToInt32(m.Value, 8);
                        retVal = ((char) val).ToString();
                        id += (m.Length - 1);
                        break;
                    }
            }

            return retVal;
        }
    }
}
