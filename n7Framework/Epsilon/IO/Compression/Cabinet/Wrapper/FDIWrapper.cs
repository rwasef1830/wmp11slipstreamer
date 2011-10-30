using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace Epsilon.IO.Compression.Cabinet
{
    public class FDIWrapper<T> : IDisposable where T : class
    {
        #region Private members
        FDI.MemAllocDelegate _pfnMalloc;
        FDI.MemFreeDelegate _pfnMfree;
        FDI.FileOpenDelegate _pfnFopen;
        FDI.FileCloseDelegate _pfnFclose;
        FDI.FileReadDelegate _pfnFread;
        FDI.FileWriteDelegate _pfnFwrite;
        FDI.FileSeekDelegate _pfnFseek;
        FDI.NotifyDelegate _pfnNotify;
        SafeMemoryBlock _pErrorStruct;
        IntPtr _fdiContext;
        List<Exception> _exceptions;
        CabIO<T> _ioFunctions;
        OpenDestFileDelegate _openFileFunc;
        CloseDestFileDelegate _closeFileFunc;
        T _userObject;
        bool _disposed;
        #endregion

        #region Private constants
        // Change this to switch to UTC in the cabinet
        const DateTimeKind c_fileTimeInCabKind = DateTimeKind.Local;
        #endregion

        #region Delegate declarations
        /// <summary>
        /// Called when FDI wants to open a handle on the destination
        /// file system to write the file it is going to extract. It is the 
        /// responsibility of the client to open this handle and return it to FDI
        /// and to make sure that this handle can be closed with the close function
        /// passed to the constructor.
        /// </summary>
        public delegate IntPtr OpenDestFileDelegate(string fileName, out bool skipFile, 
            int uncompressedSize, DateTime lastModified, FileAttributes fileAttribs, 
            out bool abortOperation, T userObject);

        /// <summary>
        /// Called when FDI wants to close the handle it opened. It is the 
        /// responsibility of the client to close the handle, set file last modified
        /// time and file attributes. FDI assumes that the file was closed even if
        /// it returns false.
        /// </summary>
        public delegate bool CloseDestFileDelegate(string fileName, IntPtr fileHandle, 
            DateTime lastModified, FileAttributes fileAttribs, bool execute, 
            out bool abortOperation, T userObject);
        #endregion

        #region Event argument declarations
        public abstract class FDIEventArgs : EventArgs
        {
            /// <summary>
            /// Set this to true to abort the current operation
            /// </summary>
            public bool AbortOperation;

            /// <summary>
            /// A reference to the user object
            /// </summary>
            public readonly T UserObject;

            protected FDIEventArgs(T userObject)
            {
                this.UserObject = userObject;
            }
        }

        public class OnCabinetInfoEventArgs : FDIEventArgs
        {
            public readonly string NextCabinetName;
            public readonly string NextDiskName;
            public readonly string NextCabinetPath;
            public readonly int CurrentSetId;
            public readonly int CabinetNumber;

            public OnCabinetInfoEventArgs(string nextCabinetName, string nextDiskName,
                string nextCabinetPath, int currentSetId, int cabinetNumber, 
                T userobject) : base(userobject)
            {
                this.NextCabinetName = nextCabinetName;
                this.NextDiskName = nextDiskName;
                this.NextCabinetPath = nextCabinetPath;
                this.CurrentSetId = currentSetId;
                this.CabinetNumber = cabinetNumber;
            }
        }

        public class OnPartialContinuationEventArgs : FDIEventArgs
        {
            public readonly string FileName;
            public readonly string CabNameHasFirstSegment;
            public readonly string DiskNameHasFirstSegment;

            public OnPartialContinuationEventArgs(string filename,
                string nameOfCabHasFirstSegment, string nameOfDiskHasFirstSegment,
                T userObject) : base(userObject)
            {
                this.FileName = filename;
                this.CabNameHasFirstSegment = nameOfCabHasFirstSegment;
                this.DiskNameHasFirstSegment = nameOfDiskHasFirstSegment;
            }
        }

        public class OnBeforeExtractNextSegmentEventArgs : FDIEventArgs
        {
            public readonly string NextCabinetName;
            public readonly string NextDiskName;
            /// <summary>
            /// You can prompt for user to insert new media or locate
            /// next cabinet and change this path. (max: 256 characters)
            /// </summary>
            public string NextCabinetPath;

            /// <summary>
            /// The error condition returned by FDI. If you return to FDI
            /// with a wrong cabinet, FDI will call this again with
            /// this set to WrongCabinet or with another error if the 
            /// next cabinet is corrupt or damaged in anyway until you set the 
            /// AbortOperation member to true to abort FDI.
            /// </summary>
            public readonly FDI.ErrorCode Error;

            public OnBeforeExtractNextSegmentEventArgs(string nextCabName, 
                string nextDiskName, string nextCabPath, FDI.ErrorCode error, 
                T userObject) : base(userObject)
            {
                this.NextCabinetName = nextCabName;
                this.NextDiskName = nextDiskName;
                this.NextCabinetPath = nextCabPath;
                this.Error = error;
            }
        }
        #endregion

        #region Public events
        /// <summary>
        /// Raised when FDI wants to give the client information about 
        /// the cabinet it is going to start processing.
        /// </summary>
        public event EventHandler<OnCabinetInfoEventArgs> OnCabinetInfo;

        /// <summary>
        /// Raised after <see cref="OnBeforeExtractFile" /> when FDI is going to
        /// cross to a new cabinet to continue reading the next segment of
        /// compressed data while decompressing the file it started.
        /// 
        /// This event should be used to make sure the next cabinet file
        /// exists and is readable and isn't corrupt. It can also be used
        /// too issue a "Insert Media" prompt. If not handled, FDI
        /// will fail if it cannot read the next cabinet for any reason.
        /// </summary>
        public event EventHandler<OnBeforeExtractNextSegmentEventArgs> 
            OnBeforeExtractNextSegment;

        /// <summary>
        /// Raised when FDI encounters files at beginning of the source
        /// cabinet that are discovered to be a continuation of the file
        /// from a previous cabinet file.
        /// </summary>
        public event EventHandler<OnPartialContinuationEventArgs> OnPartialContinuation;
        #endregion

        #region Constructors
        public FDIWrapper(OpenDestFileDelegate openFileFunc,
            CloseDestFileDelegate closeFileFunc) 
            : this(null, null, openFileFunc, closeFileFunc) { }

        public FDIWrapper(T userObject, OpenDestFileDelegate openFileFunc,
            CloseDestFileDelegate closeFileFunc) 
            : this(userObject, null, openFileFunc, closeFileFunc) { }

        public FDIWrapper(CabIO<T> ioFunctions, OpenDestFileDelegate openFileFunc,
            CloseDestFileDelegate closeFileFunc) 
            : this(null, ioFunctions, openFileFunc, closeFileFunc) { }

        public FDIWrapper(T userObject, CabIO<T> ioFunctions, 
            OpenDestFileDelegate openFileFunc, CloseDestFileDelegate closeFileFunc)
        {
            // Null checks
            if (openFileFunc == null)
                throw new ArgumentNullException("openFileFunc");
            if (closeFileFunc == null)
                throw new ArgumentNullException("closeFileFunc");

            this._userObject = userObject;
            this._exceptions = new List<Exception>(5);

            this._openFileFunc = openFileFunc;
            this._closeFileFunc = closeFileFunc;

            if (ioFunctions == null) this._ioFunctions = new CabIO<T>();
            else this._ioFunctions = ioFunctions;

            // Attach exception handler
            this._ioFunctions.OnError += this._exceptions.Add;

            // Initialize the memory block that will hold the error info
            this._pErrorStruct = new SafeMemoryBlock(typeof(FDI.Error));

            // General callbacks
            this._pfnMalloc = new FDI.MemAllocDelegate(this._ioFunctions.MemAlloc);
            this._pfnMfree = new FDI.MemFreeDelegate(this._ioFunctions.MemFree);
            this._pfnFopen = new FDI.FileOpenDelegate(this._ioFunctions.FileOpen);
            this._pfnFclose = new FDI.FileCloseDelegate(this._ioFunctions.FileClose);
            this._pfnFread = new FDI.FileReadDelegate(this._ioFunctions.FileRead);
            this._pfnFwrite = new FDI.FileWriteDelegate(this._ioFunctions.FileWrite);
            this._pfnFseek = new FDI.FileSeekDelegate(this._ioFunctions.FileSeek);

            // Notification callbacks
            this._pfnNotify = new FDI.NotifyDelegate(this.Notify);

            // Create the context
            this._fdiContext = FDI.CreateContext(this._pfnMalloc, this._pfnMfree,
                this._pfnFopen, this._pfnFread, this._pfnFwrite, this._pfnFclose,
                this._pfnFseek, this._pErrorStruct);

            if (!Helpers.IsValidHandle(this._fdiContext)) this.ThrowFdiException();
        }

        public bool IsCabinet(string cabinetFilePath, FDI.CabinetInfo ccabInfo)
        {
            this.EnsureContextIsAlive();
            IntPtr fileHandle = IntPtr.Zero;
            try
            {
                fileHandle = this._ioFunctions.FileOpen(
                    cabinetFilePath, (int)(FCntl.COpenModes.ReadOnly
                    | FCntl.COpenModes.BinaryMode),
                    (int)FCntl.CShareModes.ShareRead);

                // Make sure that FDI.IsCabinet returns without any exceptions having 
                // being generated and caught in the managed callbacks. Exceptions in
                // managed callbacks indicate a genuine error. (not just that 
                // FDI discovered a non-cabinet file or corrupt file).
                int exceptionsCount = this._exceptions.Count;
                bool result = FDI.IsCabinet(this._fdiContext, fileHandle, ccabInfo);
                if (this._exceptions.Count != exceptionsCount) this.ThrowFdiException();

                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Helpers.IsValidHandle(fileHandle)) 
                    this._ioFunctions.FileClose(fileHandle);
            }
        }

        public void Extract(string cabinetFilePath)
        {
            this.EnsureContextIsAlive();

            string dirName = Path.GetDirectoryName(cabinetFilePath);
            string fileName = Path.GetFileName(cabinetFilePath);

            bool succeeded = FDI.Copy(this._fdiContext, fileName, 
                dirName, this._pfnNotify);
            if (!succeeded)
            {
                this.ThrowFdiException();
            }
        }

        public void Close()
        {
            this.Dispose();
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources that need to have Dispose called on them
                this._pErrorStruct.Dispose();
            }

            // Free unmanaged resources
            if (Helpers.IsValidHandle(this._fdiContext))
            {
                FDI.DestroyContext(this._fdiContext);
                this._fdiContext = IntPtr.Zero;
            }

            // Mark as disposed
            this._disposed = true;
        }
        #endregion

        #region Finalizer as a backup for Dispose()
        ~FDIWrapper()
        {
            Dispose(false);
        }
        #endregion

        #region FDI callback methods
        IntPtr Notify(FDI.NotificationType notifyType, FDI.Notification notifyData)
        {
            try
            {
                switch (notifyType)
                {
                    case FDI.NotificationType.CabinetInfo:
                        if (this.OnCabinetInfo != null)
                        {
                            OnCabinetInfoEventArgs onCabInfoEventArgs
                                = new OnCabinetInfoEventArgs(notifyData.String1, notifyData.String2,
                                notifyData.String3, notifyData.SetId, notifyData.CabinetNumber,
                                this._userObject);
                            this.OnCabinetInfo(this, onCabInfoEventArgs);

                            if (onCabInfoEventArgs.AbortOperation) return (IntPtr)(-1);
                            else return IntPtr.Zero;
                        }
                        // Tell FDI to succeed and continue
                        else return IntPtr.Zero;

                    case FDI.NotificationType.CopyFile:
                        bool skipFile = false;
                        bool abortOperation1 = false;
                        IntPtr hFile = this._openFileFunc(notifyData.String1, 
                            out skipFile, notifyData.cb, FCntl.DateTimeFromDosDateTime(
                            notifyData.Date, notifyData.Time, c_fileTimeInCabKind), 
                            FCntl.FileAttributesFromFAttrs(notifyData.Attributes),
                            out abortOperation1, this._userObject);

                        if (abortOperation1) return (IntPtr)(-1);
                        else if (skipFile) return IntPtr.Zero;
                        else if (!Helpers.IsValidHandle(hFile))
                            throw new ArgumentOutOfRangeException("hFile",
                                "File handle returned from delegate is invalid.");
                        else return hFile;

                    case FDI.NotificationType.CloseFileInfo:
                        bool abortOperation2;
                        bool succeeded = this._closeFileFunc(notifyData.String1,
                            notifyData.FileHandle, FCntl.DateTimeFromDosDateTime(notifyData.Date,
                            notifyData.Time, c_fileTimeInCabKind),
                            FCntl.FileAttributesFromFAttrs(notifyData.Attributes),
                            notifyData.cb == 1, out abortOperation2, this._userObject);
                        if (abortOperation2 || !succeeded) return (IntPtr)(-1);
                        else return (IntPtr)(1);

                    case FDI.NotificationType.PartialFile:
                        if (this.OnPartialContinuation != null)
                        {
                            OnPartialContinuationEventArgs
                                onPartialFileArgs
                                = new OnPartialContinuationEventArgs(
                                notifyData.String1, notifyData.String2, notifyData.String3,
                                this._userObject);

                            this.OnPartialContinuation(this, onPartialFileArgs);

                            if (onPartialFileArgs.AbortOperation) return (IntPtr)(-1);
                            else return IntPtr.Zero;
                        }
                        else return IntPtr.Zero;

                    case FDI.NotificationType.NextCabinet:
                        if (this.OnBeforeExtractNextSegment != null)
                        {
                            OnBeforeExtractNextSegmentEventArgs onBeforeSpanned
                                = new OnBeforeExtractNextSegmentEventArgs(notifyData.String1,
                                notifyData.String2, notifyData.String3, notifyData.ErrorType,
                                this._userObject);

                            this.OnBeforeExtractNextSegment(this, onBeforeSpanned);

                            if (onBeforeSpanned.AbortOperation) return (IntPtr)(-1);
                            else return IntPtr.Zero;
                        }
                        else return IntPtr.Zero;

                    // This callback type is not needed in general, so I just return 0;
                    case FDI.NotificationType.Enumerate:
                        goto default;

                    default:
                        return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                // Log this exception
                this._exceptions.Add(ex);
                return (IntPtr)(-1);
            }
        }
        #endregion

        #region Helper methods
        void EnsureContextIsAlive()
        {
            if (this._disposed)
                throw new ObjectDisposedException(this.ToString());
        }

        void ThrowFdiException()
        {
            throw new FdiException(this._pErrorStruct, this._exceptions);
        }
        #endregion
    }
}
