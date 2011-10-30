using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace Epsilon.Parsers
{
    public abstract class ParserBase
    {
        public virtual string SubstituteVariables(
            IDictionary<string, string> varDict, string line)
        {
            if (String.IsNullOrEmpty(line)) return line;
            else
            {
                int firstIndexOfPercent = -1;
                int lastIndexOfPercent = -1;
                do
                {
                    firstIndexOfPercent = line.IndexOf('%', lastIndexOfPercent + 1);
                    lastIndexOfPercent = line.IndexOf('%', firstIndexOfPercent + 1);
                    if (lastIndexOfPercent < 0) break;

                    if (lastIndexOfPercent - firstIndexOfPercent == 1) continue;
                    else
                    {
                        string variableName = line.Substring(firstIndexOfPercent + 1,
                            lastIndexOfPercent - firstIndexOfPercent - 1);
                        if (varDict.ContainsKey(variableName))
                        {
                            line = line.Replace(String.Concat("%", variableName, "%"),
                                varDict[variableName]);
                        }
                    }
                }
                while (lastIndexOfPercent < line.Length);

                return line;
            }
        }

        protected void ThrowNullCharEncountered(string line)
        {
            int indexOfNull = line.IndexOf('\0');
            Debug.Assert(indexOfNull > 0, 
                "No null characters in the line. Going to throw by mistake.");
            ParserException exception = new ParserException(
                "Unexpected null character in input.");
            exception.Data.Add("Input upto null", String.Format("({0})",
                line.Substring(0, indexOfNull)));
            throw exception;
        }

        public class ParserException : Exception
        {
            protected const string c_DefaultMessage = "An error occurred while parsing input.";

            public ParserException() : this(c_DefaultMessage, null) { }
            public ParserException(string message) : this(message, null) { }
            public ParserException(Exception innerException)
                : this(c_DefaultMessage, innerException) { }
            public ParserException(string message, Exception innerException)
                : base(message, innerException) { }

            public string FullMessage
            {
                get
                {
                    StringBuilder messageBuilder = new StringBuilder(base.Message);
                    if (this.Data.Count > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);

                        foreach (DictionaryEntry entry in this.Data)
                        {
                            messageBuilder.AppendFormat("{0}: {1}", entry.Key, entry.Value);
                            messageBuilder.AppendLine();
                        }
                    }
                    return messageBuilder.ToString();
                }
            }
        }
    }
}
