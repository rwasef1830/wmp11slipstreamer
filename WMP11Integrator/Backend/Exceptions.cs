using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Epsilon.WindowsModTools;

namespace WMP11Slipstreamer
{
    partial class Backend
    {
        public class Exceptions
        {
            /// <summary>
            /// Base exception class for backend
            /// </summary>
            public class IntegrationException : Exception
            {
                public IntegrationException(string message)
                    : this(message, null) { }
                public IntegrationException(string message,
                    Exception innerException) : base(message, innerException) { }

                public override string Message
                {
                    get
                    {
                        StringBuilder messageBuilder = new StringBuilder(base.Message);
                        if (this.Data.Count > 0)
                        {
                            messageBuilder.Append(Environment.NewLine);
                            messageBuilder.Append(Environment.NewLine);

                            foreach (DictionaryEntry entry in this.Data)
                            {
                                string key = entry.Key.ToString();
                                string value = entry.Value.ToString().Replace("\"", "\"\"");

                                messageBuilder.AppendFormat("{0}: \"{1}\"", key, value);
                                messageBuilder.AppendLine();
                            }
                        }
                        return messageBuilder.ToString();
                    }
                }
            }

            /// <summary>
            /// Unsupported Windows source version exception
            /// </summary>
            public class SourceNotSupportedException : IntegrationException
            {
                public SourceNotSupportedException(WindowsSourceInfo sourceInfo)
                    : base("Cannot integrate WMP11 on a windows source of this type.")
                {
                    base.Data.Add("Version", sourceInfo.SourceVersion);
                    base.Data.Add("Edition", sourceInfo.Edition);
                    base.Data.Add("Service Pack", sourceInfo.ServicePack);
                    base.Data.Add("Reduced Media Edition", 
                        sourceInfo.ReducedMediaEdition);
                    base.Data.Add("Architecture", sourceInfo.Arch);
                }
            }

            /// <summary>
            /// Custom exception class for aborting worker thread gracefully
            /// </summary>
            public class BackendAbortedException : Exception { }
        }
    }
}
