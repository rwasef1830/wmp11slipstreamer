using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Epsilon.WindowsModTools;

namespace Epsilon.Slipstreamers
{
    public abstract class SlipstreamerArgumentsBase
    {
        protected readonly string _winSrc;
        protected readonly WindowsSourceInfo _winSrcInfo;
        protected readonly bool _ignoreCats;
        SlipstreamerStatus _status;
        object _syncLock = new object();

        public string WinSource
        {
            get { return this._winSrc; }
        }

        public WindowsSourceInfo SourceInfo
        {
            get { return this._winSrcInfo; }
        }

        public bool IgnoreCats
        {
            get { return this._ignoreCats; }
        }

        public SlipstreamerStatus Status
        {
            get { lock (this._syncLock) { return this._status; } }
            internal set { lock (this._syncLock) { this._status = value; } }
        }

        protected SlipstreamerArgumentsBase(
            string winSrc, WindowsSourceInfo srcInfo, bool ignoreCats)
        {
            this._winSrc = Path.GetFullPath(winSrc);
            this._winSrcInfo = srcInfo;
            this._ignoreCats = ignoreCats;

            if (Path.GetDirectoryName(this._winSrc) == null)
                this._winSrc = this._winSrc.TrimEnd(Path.DirectorySeparatorChar);

            this._status = SlipstreamerStatus.NotStarted;
        }

        /// <summary>
        /// Parses hotfix line
        /// Format is: Dir|FileName1|FileName2|...[FileName(n)]
        /// All files must exist in Dir. Files are not checked 
        /// for existence or validity in this method.
        /// </summary>
        protected void ParseHotfixLine(string hotfixLine, out string hotfixFolder,
            out ReadOnlyCollection<string> hotfixFiles)
        {
            hotfixFolder = String.Empty;
            hotfixFiles = null;

            if (!String.IsNullOrEmpty(hotfixLine))
            {
                string[] hotfixComponents = hotfixLine.Split(new char[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (hotfixComponents.Length > 1)
                {
                    hotfixFolder = hotfixComponents[0];
                    List<string> hotfixList
                        = new List<string>(hotfixComponents.Length - 1);

                    for (int i = 1; i < hotfixComponents.Length; i++)
                    {
                        hotfixList.Add(hotfixComponents[i]);
                    }

                    hotfixFiles = hotfixList.AsReadOnly();
                }
            }
            else
            {
                // The overhead should be minimal and remove the hassle of null checks
                hotfixFolder = String.Empty;
                hotfixFiles = new List<string>(0).AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Enumeration of slipstream execute states.
    /// </summary>
    public enum SlipstreamerStatus
    {
        /// <summary>
        /// The operation completed successfully
        /// </summary>
        Success = 0,
        /// <summary>
        /// The operation was gracefully cancelled at user request
        /// </summary>
        Cancelled = 1,
        /// <summary>
        /// A condition has forced the operation to terminate (normal error)
        /// </summary>
        Error = 2,
        /// <summary>
        /// An unexpected exception was thrown
        /// </summary>
        UnhandledException = 3,
        /// <summary>
        /// Operation has not started yet
        /// </summary>
        NotStarted = 4,
        /// <summary>
        /// Operation in progress
        /// </summary>
        Working = 5,
        /// <summary>
        /// Operation in critical state and cannot
        /// be cancelled without data loss/corruption
        /// </summary>
        Critical = 6,
        /// <summary>
        /// Unhandled exception happened during critical
        /// state, data loss/corruption has probably occurred
        /// </summary>
        CriticalError = 7
    }
}
