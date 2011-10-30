using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Epsilon.DebugServices;
using Epsilon.IO;
using Epsilon.Parsers;
using Epsilon.WindowsModTools;
using Epsilon.n7Framework.Epsilon.Slipstreamers;
using System.Threading;

namespace Epsilon.Slipstreamers
{
    public abstract class SlipstreamerBase
    {
        #region Private fields
        volatile bool _paused;
        volatile bool _aborting;
        readonly StringBuilder _pathBuffer;
        readonly List<SlipstreamStep> _slipstreamerSteps;
        #endregion

        #region Protected fields
        protected readonly string _archDir;
        protected readonly string _x64i386ArchDir;
        protected readonly string _driverDir;
        protected readonly string _workDir;
        protected readonly string _x64i386WorkDir;
        protected readonly string _extractDir;
        protected readonly SlipstreamerArgumentsBase _state;

        // Needed to compress edited files
        protected readonly string[] _possCompArchFile 
            = new string[] { "sysoc.inf", "svcpack.inf" };

        // Backend that inherits from this class has liberty
        // to use these variables as need arises.
        protected IniParser _txtsetupSif;
        protected IniParser _dosnetInf;
        protected IniParser _svcpackInf;
        protected IniParser _sysocInf;
        protected IniParser _drvindexInf;
        #endregion

        #region Public accessors
        public string WorkingDirectory
        {
            get { return this._workDir; }
        }

        public WindowsSourceInfo OsInfo
        {
            get { return this._state.SourceInfo; }
        }
        #endregion

        #region Constructor
        protected SlipstreamerBase(SlipstreamerArgumentsBase arguments, string tempName)
        {
            this._pathBuffer = new StringBuilder(FileSystem.MaximumPathLength);
            this._state = arguments;

            this._slipstreamerSteps = new List<SlipstreamStep>(5);

            switch (this._state.SourceInfo.Arch)
            {
                case TargetArchitecture.x86:
                    this._archDir = this.CreatePathString(arguments.WinSource, "i386");
                    this._x64i386ArchDir = String.Empty;
                    break;

                case TargetArchitecture.x64:
                    this._archDir = this.CreatePathString(arguments.WinSource, "amd64");
                    this._x64i386ArchDir = this.CreatePathString(
                        arguments.WinSource, "i386");
                    break;

                default:
                    throw new SourceNotSupportedException(
                        this._state.SourceInfo);
            }

            // Check if source is supported
            if (!this.SourceIsSupported())
            {
                throw new SourceNotSupportedException(this._state.SourceInfo);
            }

            this._workDir = this.CreatePathString(this._archDir, tempName);
            this._x64i386WorkDir = this.CreatePathString(this._workDir, "x64_i386");
            this._extractDir = this.CreatePathString(this._workDir, "rawfiles");
            this._driverDir = this.CreatePathString(this._workDir, "driver");

            // Create folders
            FileSystem.CreateEmptyDirectories(this._workDir, this._x64i386WorkDir, 
                this._extractDir, this._driverDir);
        }
        #endregion

        #region Pause and abort methods
        /// <summary>
        /// Aborts execution at next possible checkpoint.
        /// </summary>
        public void Abort()
        {
            this._aborting = true;
            this._paused = false;
        }

        /// <summary>
        /// Pauses execution at the next possible checkpoint.
        /// </summary>
        public void Pause()
        {
            this._paused = true;
        }

        /// <summary>
        /// Resumes execution
        /// </summary>
        public void Resume()
        {
            this._paused = false;
        }

        protected void CancelOrPauseCheckpoint()
        {
            while (this._paused) Thread.Sleep(30);

            if (this._aborting)
            {
                throw new SlipstreamerAbortedException();
            }
        }
        #endregion

        #region Abstract methods
        protected abstract bool SourceIsSupported();
        #endregion

        #region Protected methods
        /// <summary>
        /// Adds a slipstreamer step object describing the method to call.
        /// Steps will be executed in the order they are added.
        /// </summary>
        /// <param name="step"></param>
        protected void AddSlipstreamStep(SlipstreamStep step)
        {
            this._slipstreamerSteps.Add(step);
        }

        /// <summary>
        /// Applies all the changes accumulated in the work folder to the
        /// actual source. An exception here will corrupt the installation source.
        /// </summary>
        protected void MergeFolders()
        {
            this.OnBeginCriticalOperation();
            FileSystem.MoveFiles(this._workDir, this._archDir, false);
            if (this._state.SourceInfo.Arch == TargetArchitecture.x64)
            {
                FileSystem.MoveFiles(this._x64i386WorkDir, this._x64i386ArchDir, false);
            }

            if (!this._state.IgnoreCats)
            {
                string svcpackArchPath = this.CreatePathString(this._archDir, "SVCPACK");
                string svcpackWorkPath = this.CreatePathString(this._workDir, "SVCPACK");

                if (!Directory.Exists(svcpackArchPath))
                {
                    Directory.CreateDirectory(svcpackArchPath);
                }

                foreach (string svcpackWorkCatPath in
                    Directory.GetFiles(svcpackWorkPath, "*",
                    SearchOption.TopDirectoryOnly))
                {
                    string svcpackWorkCatFileName
                        = Path.GetFileName(svcpackWorkCatPath);
                    string svcpackArchCatPath
                        = this.CreatePathString(
                        this._archDir, "SVCPACK", svcpackWorkCatFileName);
                    FileSystem.MoveFileOverwrite(svcpackWorkCatPath, svcpackArchCatPath);
                }
            }
            this.OnEndCriticalOperation();
        }

        /// <summary>
        /// Cleans up the source by deleting the temporary work subfolder.
        /// </summary>
        protected void Cleanup()
        {
            FileSystem.DeleteFolder(this._workDir);
        }

        protected string CreatePathString(params string[] components)
        {
            return FileSystem.CreatePathString(this._pathBuffer, components);
        }

        protected bool FileExistsInArch(string fileName)
        {
            return this.FileExistsInArch(fileName, true);
        }

        protected bool FileExistsInArch(string fileName, bool checkCompressed)
        {
            return this.FilesExistInArch(
                new string[] { fileName }, 0, checkCompressed);
        }

        protected bool FilesExistInArch(string[] fileNames, int startAt)
        {
            return this.FilesExistInArch(fileNames, startAt, true);
        }

        protected bool FilesExistInArch(string[] fileNames, 
            int startAt, bool checkCompressed)
        {
            for (int i = startAt; i < fileNames.Length; i++)
            {
                if (!FileExistsInSourceFolder(this._pathBuffer, fileNames[i], 
                    this._archDir, checkCompressed))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Copy or expand file from the arch directory to the destination else
        /// throw FileNotFoundException if file and compressed cab both not exist.
        /// </summary>
        /// <param name="fileName">filename to copy or expand if compressed 
        /// (must specify uncompressed file name) without path</param>
        /// <param name="destFolder">destination folder</param>
        /// <param name="ignoreIfNotExist">Don't throw exception if filename 
        /// doesn't exist</param>
        /// <returns>true if file was copied, false if it doesn't exist and 
        /// ignoreIfNotExist is true</returns>
        protected bool CopyOrExpandFromArch(string fileName, string destFolder,
            bool ignoreIfNotExist)
        {
            string archFileName = this.CreatePathString(this._archDir, fileName);
            string compressedArchFileName = GetCompressedFileName(archFileName);
            string destinationFileName = this.CreatePathString(destFolder, fileName);

            if (File.Exists(archFileName))
            {
                File.Copy(archFileName, destinationFileName);
                return true;
            }
            else if (File.Exists(compressedArchFileName))
            {
                Archival.NativeCabinetExtract(compressedArchFileName, destFolder);
                return true;
            }
            else if (ignoreIfNotExist)
            {
                return false;
            }
            else
            {
                throw new ArchFileNotFoundException(fileName, archFileName);
            }
        }

        /// <summary>
        /// Extract and parse common architecture files into IniParser instances.
        /// </summary>
        protected void ExtractAndParseArchFiles()
        {
            string svcpack = "svcpack.inf";
            string sysoc = "sysoc.inf";
            string dosnet = "dosnet.inf";
            string txtsetup = "txtsetup.sif";

            string[] maybeCompressedFiles = new string[] { sysoc };
            string[] notCompressedFiles = new string[] { dosnet, txtsetup };

            foreach (string fileName in notCompressedFiles)
            {
                this.CancelOrPauseCheckpoint();
                string srcPath = this.CreatePathString(this._archDir, fileName);
                if (File.Exists(srcPath))
                {
                    string dstPath = this.CreatePathString(this._workDir, fileName);
                    FileSystem.CopyFile(srcPath, dstPath);
                }
                else
                {
                    throw new ArchSetupFileNotFoundException(fileName, srcPath);
                }
            }

            foreach (string fileName in maybeCompressedFiles)
            {
                this.CancelOrPauseCheckpoint();
                this.CopyOrExpandFromArch(fileName, this._workDir, false);
            }

            if (!this.CopyOrExpandFromArch(svcpack, this._workDir, true))
            {
                File.WriteAllText(this.CreatePathString(this._workDir, svcpack),
                    "[Version]\r\nSignature=\"$Windows NT$\"\r\n\r\n[SetupData]\r\n"
                    + "CatalogSubDir=\"i386\\hotfixes\"\r\n\r\n[ProductCatalogsToInstall]\r\n\r\n"
                    + "[SetupHotfixesToRun]", Encoding.ASCII);
            }

            this.CancelOrPauseCheckpoint();

            // Parse setup files
            this._txtsetupSif = new TxtsetupParser(
                this.CreatePathString(this._workDir, txtsetup));

            this._dosnetInf = new IniParser(
                this.CreatePathString(this._workDir, dosnet), true);
            this._svcpackInf = new IniParser(
                this.CreatePathString(this._workDir, svcpack), true);
            this._sysocInf = new IniParser(
                this.CreatePathString(this._workDir, sysoc), true);
        }

        protected virtual void SaveArchFiles()
        {
            FileSystem.UnsetReadonly(this._txtsetupSif.IniFileInfo);
            FileSystem.UnsetReadonly(this._dosnetInf.IniFileInfo);
            FileSystem.UnsetReadonly(this._sysocInf.IniFileInfo);
            FileSystem.UnsetReadonly(this._svcpackInf.IniFileInfo);

            this._txtsetupSif.SaveIni();
            this._dosnetInf.SaveIni();
            this._sysocInf.SaveIni();
            this._svcpackInf.SaveIni();
        }
        #endregion

        #region Public methods
        public void Slipstream()
        {
            // Extra 3 for: Initial init-went-well increment, merge and cleanup.
            ProgressTracker progressTracker 
                = new ProgressTracker(this._slipstreamerSteps.Count + 3);
            this.OnIncrementGlobalProgress(progressTracker);

            try
            {
                this._state.Status = SlipstreamerStatus.Working;

                for (int i = 0; i < this._slipstreamerSteps.Count; i++)
                {
                    SlipstreamStep step = this._slipstreamerSteps[i];
                    this.OnAnnounce(step.Status);
                    step.ExecuteStep();
                    this.CancelOrPauseCheckpoint();
                    this.OnIncrementGlobalProgress(progressTracker);
                }

                this.OnAnnounce(SlipstreamersMsg.statMergingFolders);
                if (this.OnCheckpoint("Merge to source ?")) this.MergeFolders();
                this.OnIncrementGlobalProgress(progressTracker);

                this.OnAnnounce(SlipstreamersMsg.statCleaningUp);
                if (this.OnCheckpoint("Delete work folder ?")) this.Cleanup();
                this.OnIncrementGlobalProgress(progressTracker);

                this._state.Status = SlipstreamerStatus.Success;
            }
            catch (SlipstreamerAbortedException)
            {
                this._state.Status = SlipstreamerStatus.Cancelled;
            }
            catch (IntegrationException)
            {
                Debug.Assert(this._state.Status != SlipstreamerStatus.Critical,
                    "IntegrationException in critical operation.",
                    "Must not throw exceptions during critical operation.");
                if (this._state.Status == SlipstreamerStatus.Critical)
                {
                    this._state.Status = SlipstreamerStatus.CriticalError;
                }
                else
                {
                    this._state.Status = SlipstreamerStatus.Error;
                }
                throw;
            }
            catch (Exception)
            {
                if (this._state.Status == SlipstreamerStatus.Critical)
                {
                    this._state.Status = SlipstreamerStatus.CriticalError;
                }
                else
                {
                    this._state.Status = SlipstreamerStatus.UnhandledException;
                }
                throw;
            }
            finally
            {
                // Try a last ditch cleanup and write exceptions to debug
                try { if (Directory.Exists(this._workDir)) this.Cleanup(); }
                catch (Exception ex) { HelperConsole.ErrorWriteLine(ex.ToString()); }
                finally { }
            }
        }
        #endregion

        #region Static methods
        protected static string GetCompressedFileName(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return Path.ChangeExtension(filePath, 
                extension.Substring(0, extension.Length - 1) + "_");
        }

        public static bool FileExistsInSourceFolder(StringBuilder buffer, 
            string fileName, string sourceFolder, bool checkCompressed)
        {
            string archPath = FileSystem.CreatePathString(buffer, 
                new string[] { sourceFolder, fileName });
            string archPathCompressed = GetCompressedFileName(archPath);
            bool found = File.Exists(archPath);
            if (checkCompressed)
            {
                found |= File.Exists(archPathCompressed);
            }
            return found;
        }

        protected static bool VersionIsWithin(Version upperBound, 
            Version lowerBound, Version version)
        {
            return (version.CompareTo(lowerBound) >= 0)
                && (version.CompareTo(upperBound) <= 0);
        }

        /// <summary>
        /// Checks if a file is newer than the other. For EXEs and DLLs that have
        /// the FileVersionInfo structure, their version is used, otherwise, thier last
        /// modified date is used.
        /// </summary>
        /// <param name="fileToCompareAgainst">File to compare against</param>
        /// <param name="fileToCompareTo">File to compare to the first</param>
        /// <returns>FileVersionComparison result</returns>
        protected static FileVersionComparison CompareVersions(
            string fileToCompareAgainst,
            string fileToCompareTo)
        {
            FileVersionInfo version1
                = FileVersionInfo.GetVersionInfo(fileToCompareAgainst);
            if (!File.Exists(fileToCompareTo))
                return FileVersionComparison.NotFound;
            FileVersionInfo version2
                = FileVersionInfo.GetVersionInfo(fileToCompareTo);
            int? result = null;
            if (version1.FileVersion == null || version2.FileVersion == null)
            {
                DateTime version1DateTime
                    = File.GetLastWriteTimeUtc(fileToCompareAgainst);
                DateTime version2DateTime
                    = File.GetLastWriteTimeUtc(fileToCompareTo);
                result = version1DateTime.CompareTo(version2DateTime);
            }
            else
            {
                Version vFirst
                    = new Version(version1.FileMajorPart, version1.FileMinorPart,
                    version1.FileBuildPart, version1.FilePrivatePart);
                Version vSecond
                    = new Version(version2.FileMajorPart, version2.FileMinorPart,
                    version2.FileBuildPart, version2.FilePrivatePart);
                result = vFirst.CompareTo(vSecond);
            }
            if (result.HasValue)
            {
                if (result == 0)
                    return FileVersionComparison.Same;
                else if (result < 0)
                    return FileVersionComparison.Older;
                else if (result > 0)
                    return FileVersionComparison.Newer;
            }
            return FileVersionComparison.Error;
        }

        protected static bool ByteArraysAreEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;
            else
            {
                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                        return false;
                }
                return true;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised to display an informational message.
        /// </summary>
        public event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// If value == -1, progress bar should be hidden and reset, else show
        /// the value and make it visible.
        /// </summary>
        public event ProgressEventDelegate UpdateGlobalProgress;

        /// <summary>
        /// If value == -1, progress bar should be hidden and reset, else show
        /// the value and make it visible.
        /// </summary>
        public event ProgressEventDelegate UpdateCurrentProgress;

        /// <summary>
        /// Called just before starting an operation from which there
        /// can be no graceful cancel or shutdown without corruption of data.
        /// </summary>
        public event Action<SlipstreamerBase> BeginCriticalOperation;

        /// <summary>
        /// Called just after a critical operation has completed successfully.
        /// Aborting the operation can be done now.
        /// </summary>
        public event Action<SlipstreamerBase> ExitCriticalOperation;

        /// <summary>
        /// Called to announce to the caller the start of a new operation.
        /// Can be used to update a label or write to the console.
        /// </summary>
        public event Action<string> AnnounceOperation;

        /// <summary>
        /// Called to check if it is possible to continue with slipstream.
        /// Used mainly for debugging purposes.
        /// </summary>
        public event Predicate<string> Checkpoint;

        public delegate void ProgressEventDelegate(int value, int max);

        public class MessageEventArgs : EventArgs
        {
            public readonly string Message;
            public readonly string MessageTitle;
            public readonly MessageEventType MessageType;

            public MessageEventArgs(string message, string messageTitle,
                MessageEventType messageType)
            {
                this.Message = message;
                this.MessageTitle = messageTitle;
                this.MessageType = messageType;
            }
        }

        /// <summary>
        /// Message event types, can be directly casted to 
        /// their MessageBoxIcon equivalent
        /// </summary>
        public enum MessageEventType
        {
            Error = 16,
            Warning = 48,
            Information = 64,
        }

        protected void OnMessage(string message, string messageTitle,
            MessageEventType messageType)
        {
            if (this.Message != null)
            {
                MessageEventArgs args = new MessageEventArgs(
                    message, messageTitle, messageType);
                this.Message(this, args);
            }
        }

        protected void OnAnnounce(string opMessage)
        {
            HelperConsole.InfoWriteLine(opMessage, "Announcement");
            if (this.AnnounceOperation != null)
            {
                this.AnnounceOperation(opMessage);
            }
        }

        protected bool OnCheckpoint(string message)
        {
            if (this.Checkpoint != null)
            {
                return this.Checkpoint(message);
            }
            else
            {
                return true;
            }
        }

        #region Global progress controller methods
        void OnIncrementGlobalProgress(ProgressTracker progressTracker)
        {
            progressTracker.Increment();
            this.OnUpdateGlobalProgress(progressTracker.Value, progressTracker.Maximum);
        }

        void OnUpdateGlobalProgress(int val, int max)
        {
            if (this.UpdateGlobalProgress != null)
            {
                this.UpdateGlobalProgress(val, max);
            }
        }

        void OnHideGlobalProgress()
        {
            this.OnUpdateGlobalProgress(-1, 0);
        }
        #endregion

        #region Current step progress controller methods
        protected void OnUpdateCurrentProgress(int val, int max)
        {
            if (this.UpdateCurrentProgress != null)
            {
                this.UpdateCurrentProgress(val, max);
            }
        }

        protected void OnIncrementCurrentProgress(ProgressTracker progressTracker)
        {
            progressTracker.Increment();
            this.OnUpdateCurrentProgress(progressTracker.Value, 
                progressTracker.Maximum);
        }

        protected void OnResetCurrentProgress()
        {
            this.OnUpdateCurrentProgress(0, 1);
        }

        protected void OnHideCurrentProgress()
        {
            this.OnUpdateCurrentProgress(-1, 0);
        }
        #endregion

        #region Critical operation marker controller methods
        protected void OnBeginCriticalOperation()
        {
            this._state.Status = SlipstreamerStatus.Critical;

            if (this.BeginCriticalOperation != null)
            {
                this.BeginCriticalOperation(this);
            }
        }

        protected void OnEndCriticalOperation()
        {
            this._state.Status = SlipstreamerStatus.Working;

            if (this.ExitCriticalOperation != null)
            {
                this.ExitCriticalOperation(this);
            }
        }
        #endregion
        #endregion

        #region Progress Tracker
        protected class ProgressTracker
        {
            int _maximum;
            int _value;
            
            public int Maximum
            {
                get { return this._maximum; }
            }

            public int Value
            {
                get { return this._value; }
            }

            public ProgressTracker(int max)
            {
                this._maximum = max;
            }

            public void Increment()
            {
                this._value++;
            }
        }
        #endregion
    }

    public class SlipstreamStep
    {
        internal readonly SlipstreamStepDelegate ExecuteStep;
        internal readonly string Status;

        public SlipstreamStep(SlipstreamStepDelegate stepMethod)
            : this(stepMethod, String.Empty) { }

        public SlipstreamStep(SlipstreamStepDelegate stepMethod, string status)
        {
            this.ExecuteStep = stepMethod;
            this.Status = status;
        }
    }

    public delegate void SlipstreamStepDelegate();

    /// <summary>
    /// Enumeration for the file version comparison results
    /// </summary>
    public enum FileVersionComparison
    {
        /// <summary>
        /// The files have identical dates or versions
        /// </summary>
        Same,
        /// <summary>
        /// The first file is newer than the second
        /// </summary>
        Newer,
        /// <summary>
        /// The first file is older than the second
        /// </summary>
        Older,
        /// <summary>
        /// The second file was not found
        /// </summary>
        NotFound,
        /// <summary>
        /// An unknown error occurred
        /// </summary>
        Error
    }
}
