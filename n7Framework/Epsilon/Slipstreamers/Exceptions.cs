using System;
using System.Collections;
using System.Text;
using Epsilon.n7Framework.Epsilon.Slipstreamers;

namespace Epsilon.Slipstreamers
{
    /// <summary>
    /// Base exception class for backend
    /// </summary>
    public class IntegrationException : Exception
    {
        public IntegrationException(string message)
            : this(message, null) { }
        public IntegrationException(string message,
            Exception innerException)
            : base(message, innerException) { }

        private string _cachedFullMessage;
        private int _cachedDataCount;

        public string FullMessage
        {
            get
            {
                if (this._cachedFullMessage == null
                    || this._cachedDataCount != this.Data.Count)
                {
                    StringBuilder messageBuilder
                        = new StringBuilder(base.Message);
                    if (this.Data.Count > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);
                        messageBuilder.Append(Environment.NewLine);

                        foreach (DictionaryEntry entry in this.Data)
                        {
                            string key = entry.Key.ToString();
                            string value
                                = entry.Value.ToString().Replace("\"", "\"\"");

                            messageBuilder.AppendFormat(
                                "{0}: \"{1}\"", key, value);
                            messageBuilder.AppendLine();
                        }
                    }

                    this._cachedFullMessage = messageBuilder.ToString();
                    this._cachedDataCount = this.Data.Count;
                }
                return this._cachedFullMessage;
            }
        }
    }

    public class FileNotFoundException : IntegrationException
    {
        static string s_DefaultMessage = SlipstreamersMsg.errFileNotFound;

        public FileNotFoundException(string fileName, string expectedPath)
            : this(fileName, expectedPath, s_DefaultMessage) { }

        public FileNotFoundException(string fileName,
            string expectedPath, string message)
            : base(message)
        {
            base.Data.Add(SlipstreamersMsg.errFileName, fileName);
            base.Data.Add(SlipstreamersMsg.errExpectedPath, expectedPath);
        }
    }

    /// <summary>
    /// Critical setup file missing
    /// </summary>
    public class ArchSetupFileNotFoundException : FileNotFoundException
    {
        static string s_DefaultMessage
            = SlipstreamersMsg.errCriticalArchFileNotFound;

        public ArchSetupFileNotFoundException(
            string fileName, string expectedPath)
            : base(fileName, expectedPath, s_DefaultMessage) { }
    }

    public class ArchFileNotFoundException : FileNotFoundException
    {
        static string s_DefaultMessage
            = SlipstreamersMsg.errArchFileNotFound;

        public ArchFileNotFoundException(string fileName, string expectedPath)
            : base(fileName, expectedPath, s_DefaultMessage) { }
    }

    /// <summary>
    /// Unsupported Windows source version exception
    /// </summary>
    public class SourceNotSupportedException : IntegrationException
    {
        static string s_DefaultMessage
            = SlipstreamersMsg.errSourceNotSupported;

        public SourceNotSupportedException(WindowsSourceInfo sourceInfo)
            : base(s_DefaultMessage)
        {
            base.Data.Add("Version", sourceInfo.SourceVersion);
            base.Data.Add("Edition", sourceInfo.Edition);
            base.Data.Add("Service Pack", sourceInfo.ServicePack);
            base.Data.Add("Reduced Media Edition",
                sourceInfo.ReducedMediaEdition);
            base.Data.Add("Architecture", sourceInfo.Arch);
        }
    }


    public class DuplicateStepOrdinal : Exception
    {
        public DuplicateStepOrdinal()
            : base("Methods found marked as steps but with same ordinal.") { }
    }

    /// <summary>
    /// Custom exception class for aborting worker thread gracefully
    /// </summary>
    public class SlipstreamerAbortedException : Exception { }
}
