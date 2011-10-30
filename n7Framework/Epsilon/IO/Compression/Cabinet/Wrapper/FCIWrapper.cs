using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using Epsilon.Win32.API;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Epsilon.IO.Compression.Cabinet
{
    public class FCIWrapper<T> : IDisposable where T : class
    {
        #region Public members
        public ReadOnlyCollection<Exception> Exceptions
        {
            get { return _exceptions.AsReadOnly(); }
        }
        #endregion

        #region Private members
        FCI.MemAllocDelegate _pfnMalloc;
        FCI.MemFreeDelegate _pfnMfree;
        FCI.FilePlacedDelegate _pfnFplaced;
        FCI.FileDeleteDelegate _pfnFdelete;
        FCI.FileOpenDelegate _pfnFopen;
        FCI.FileCloseDelegate _pfnFclose;
        FCI.FileReadDelegate _pfnFread;
        FCI.FileWriteDelegate _pfnFwrite;
        FCI.FileSeekDelegate _pfnFseek;
        FCI.GetTempFileDelegate _pfnGetTemp;
        FCI.GetOpenInfoDelegate _pfnOpenInfo;
        FCI.GetNextCabinetDelegate _pfnNextCab;
        FCI.StatusDelegate _pfnStatus;
        SafeMemoryBlock _pErrorStruct;
        FCI.CurrentCabinet _currentCabinet;
        IntPtr _fciContext;
        List<Exception> _exceptions;
        CabIO<T> _ioFunctions;
        T _userObject;
        bool _disposed;
        #endregion

        #region Private constants
        const int c_cabinetMaxPath = 260;
        const int c_maxHeaderReserve = 60000;
        const int c_maxFolderReserve = 255;
        const int c_maxDataReserve = 255;
        const int c_defaultFolderThresh = 900 * 1024;
        const int c_defaultMaxCabSize = int.MaxValue;
        #endregion

        #region Event argument declarations
        public abstract class FCIEventArgs : EventArgs
        {
            /// <summary>
            /// Set this to true to abort the current operation
            /// </summary>
            public bool AbortOperation;

            /// <summary>
            /// A reference to the user object
            /// </summary>
            public readonly T UserObject;

            protected FCIEventArgs(T userObject)
            {
                this.UserObject = userObject;
            }
        }

        public class FilePlacedEventArgs : FCIEventArgs
        {
            /// <summary>
            /// Do not modify any members of this structure
            /// </summary>
            public readonly FCI.CurrentCabinet CurrentCabinet;
            public readonly string FileName; 
            public readonly int FileSize; 
            public readonly bool IsContinuation;

            public FilePlacedEventArgs(FCI.CurrentCabinet currentCabinet,
                string fileName, int fileSize, bool isContinuation, 
                T userObject) : base(userObject)
            {
                this.CurrentCabinet = currentCabinet;
                this.FileName = fileName;
                this.FileSize = fileSize;
                this.IsContinuation = isContinuation;
            }
        }

        public class GetNextCabinetEventArgs : FCIEventArgs
        {
            /// <summary>
            /// Use the CabinetId member of this which has already been 
            /// incremented by FCI and change CabinetName. You can also change 
            /// the DiskName if you want.
            /// </summary>
            public readonly FCI.CurrentCabinet CurrentCabinet;
            public readonly int PreviousCabinetSize;

            public GetNextCabinetEventArgs(FCI.CurrentCabinet currentCabinet,
                int previousCabinetSize, T userObject) : base(userObject)
            {
                this.CurrentCabinet = currentCabinet;
                this.PreviousCabinetSize = previousCabinetSize;
            }
        }

        public class StatusCompressBlockEventArgs : FCIEventArgs
        {
            public readonly int SizeOfCompressedBlock;
            public readonly int SizeOfUncompressedBlock;

            public StatusCompressBlockEventArgs(int sizeOfCompressedBlock,
                int sizeOfUncompressedBlock, T userObject) : base(userObject)
            {
                this.SizeOfCompressedBlock = sizeOfCompressedBlock;
                this.SizeOfUncompressedBlock = sizeOfUncompressedBlock;
            }
        }

        public class StatusAddingFolderEventArgs : FCIEventArgs
        {
            public int FolderSizeCopiedToCabinet;
            public int TotalFolderSize;

            public StatusAddingFolderEventArgs(int folderSizeCopiedToCabinet,
                int totalFolderSize, T userObject) : base(userObject)
            {
                this.FolderSizeCopiedToCabinet = folderSizeCopiedToCabinet;
                this.TotalFolderSize = totalFolderSize;
            }
        }
        #endregion

        #region Public events
        public event EventHandler<FilePlacedEventArgs> OnFilePlaced;
        /// <summary>
        /// This must be handled by the client for split cabinets to 
        /// change the cabinet name, otherwise each new cabinet part will 
        /// overwrite the same file.
        /// </summary>
        public event EventHandler<GetNextCabinetEventArgs> OnGetNextCabinet;
        public event EventHandler<StatusCompressBlockEventArgs> OnCompressBlock;
        public event EventHandler<StatusAddingFolderEventArgs> OnAddFolder;
        #endregion

        #region Constructors
        public FCIWrapper(FCI.CurrentCabinet currentCab) : this(currentCab, null) { }

        public FCIWrapper(FCI.CurrentCabinet currentCab, T userObject) 
            : this(currentCab, userObject, null) { }

        public FCIWrapper(FCI.CurrentCabinet currentCab, 
            T userObject , CabIO<T> ioFunctions)
        {
            this._userObject = userObject;
            this._exceptions = new List<Exception>(5);

            if (this._ioFunctions == null) this._ioFunctions = new CabIO<T>();
            else this._ioFunctions = ioFunctions;
            
            // Attach the error handler
            this._ioFunctions.OnError += this._exceptions.Add;

            // We cannot just normally marshal FCI.Error back and forth
            // because FCI keeps a pointer to it at its side and .NET only
            // automatically marshals stuff that is used in the same call. 
            //
            // Afterwards, FCI updates its copy of the structure
            // but that copy is never passed back to the CLR.
            //
            // Solution: Do it the C++ way, allocate memory from unmanaged
            // heap the size of FCI.Error and pass that to FCI and when
            // an error occurs, marshal it manually to a managed struct and 
            // read its contents. I will use a wrapper to be sure of no leaks.
            this._pErrorStruct = new SafeMemoryBlock(typeof(FCI.Error));

            if (currentCab == null)
            {
                throw new ArgumentNullException("currentCab");
            }

            if (currentCab.MaximumCabinetSize == 0)
                currentCab.MaximumCabinetSize = c_defaultMaxCabSize;

            if (currentCab.FolderThreshold == 0)
                currentCab.FolderThreshold = c_defaultFolderThresh;
            
            if (currentCab.ReservedInHeader > c_maxHeaderReserve)
            {
                throw new ArgumentException(String.Format(
                    "Header reserved size cannot be more than {0}.",
                    c_maxHeaderReserve), "currentCab");
            }
            else if (currentCab.ReservedInFolder > c_maxFolderReserve)
            {
                throw new ArgumentException(String.Format(
                    "Folder reserved size cannot be more than {0}.",
                    c_maxFolderReserve, "currentCab"));
            }
            else if (currentCab.ReservedInData > c_maxDataReserve)
            {
                throw new ArgumentException(String.Format(
                    "Data reserved size cannot be more than {0}.",
                    c_maxDataReserve, "currentCab"));
            }
            else
            {
                this._currentCabinet = currentCab;
            }

            if (!Directory.Exists(this._currentCabinet.CabinetPath))
                Directory.CreateDirectory(this._currentCabinet.CabinetPath);

            // cabinet.dll can only deal with 256 chars in path, so we shortify it.
            StringBuilder shortCabPath = new StringBuilder(c_cabinetMaxPath);
            Epsilon.Win32.API.IO.GetShortPathName(
                this._currentCabinet.CabinetPath, shortCabPath,
                (uint)shortCabPath.Capacity);
            // Fallback in case GetShortPathName fails for some reason
            if (shortCabPath.Length == 0)
            {
                Debug.Assert(shortCabPath.Length > 0, "GetShortPathName failed!", 
                    "Falling back to original cabinet path");
                shortCabPath.Append(this._currentCabinet.CabinetPath);
            }
            if (shortCabPath[shortCabPath.Length - 1] != Path.DirectorySeparatorChar)
                shortCabPath.Append(Path.DirectorySeparatorChar);
            this._currentCabinet.CabinetPath = shortCabPath.ToString();

            // Initialise delegate instance members to the built-in functions.
            // They need to be members of the class because the GC cannot tell 
            // if the function pointers are still held by the unmanaged code or not, 
            // so it will race with FCI and will GC them, causing an access 
            // violation in cabinet.dll when it tries to call them.
            // - Special FCI callbacks
            this._pfnFplaced = new FCI.FilePlacedDelegate(FilePlaced);
            this._pfnNextCab = new FCI.GetNextCabinetDelegate(GetNextCabinet);
            this._pfnOpenInfo = new FCI.GetOpenInfoDelegate(GetOpenInfo);
            this._pfnStatus = new FCI.StatusDelegate(StatusNotify);

            // - General callbacks (memory, IO)
            this._pfnMalloc = new FCI.MemAllocDelegate(this._ioFunctions.MemAlloc);
            this._pfnMfree = new FCI.MemFreeDelegate(this._ioFunctions.MemFree);
            this._pfnFopen = new FCI.FileOpenDelegate(this._ioFunctions.FileOpen);
            this._pfnFclose = new FCI.FileCloseDelegate(this._ioFunctions.FileClose);
            this._pfnFread = new FCI.FileReadDelegate(this._ioFunctions.FileRead);
            this._pfnFwrite = new FCI.FileWriteDelegate(this._ioFunctions.FileWrite);
            this._pfnFseek = new FCI.FileSeekDelegate(this._ioFunctions.FileSeek);
            this._pfnFdelete = new FCI.FileDeleteDelegate(
                this._ioFunctions.FileDelete);
            this._pfnGetTemp = new FCI.GetTempFileDelegate(
                this._ioFunctions.GetTempFile);

            // Finally call FCI.CreateContext
            this._fciContext = FCI.CreateContext(this._pErrorStruct, this._pfnFplaced,
                this._pfnMalloc, this._pfnMfree, this._pfnFopen, this._pfnFread,
                this._pfnFwrite, this._pfnFclose, this._pfnFseek, this._pfnFdelete,
                this._pfnGetTemp, this._currentCabinet);

            if (!Helpers.IsValidHandle(this._fciContext)) ThrowFciException();
        }
        #endregion

        #region Public methods
        public void AddFile(string filename, string filenameInCab,
            bool setExecOnDecompressFlag, FCI.CompressionLevel compressionLevel)
        {
            this.EnsureContextIsAlive();

            bool succeeded = FCI.AddFile(this._fciContext, filename, filenameInCab,
                setExecOnDecompressFlag, this._pfnNextCab, this._pfnStatus,
                this._pfnOpenInfo, compressionLevel);
            if (!succeeded) ThrowFciException();
        }

        public void FlushFolder()
        {
            this.EnsureContextIsAlive();

            bool succeeded = FCI.FlushFolder(this._fciContext, this._pfnNextCab,
                this._pfnStatus);
            if (!succeeded) ThrowFciException();
        }

        public void FlushCabinet()
        {
            this.EnsureContextIsAlive();

            bool succeeded = FCI.FlushCabinet(this._fciContext, false, this._pfnNextCab,
                this._pfnStatus);
            if (!succeeded) ThrowFciException();
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
            if (Helpers.IsValidHandle(this._fciContext))
            {
                if (!FCI.DestroyContext(this._fciContext))
                {
                    Debug.WriteLine(String.Format("FCI context {0} failed to close.",
                        this._fciContext));
                }
                this._fciContext = IntPtr.Zero;
            }

            // Mark as disposed
            this._disposed = true;
        }
        #endregion

        #region Finalizer
        ~FCIWrapper()
        {
            Dispose(false);
        }
        #endregion

        #region FCI callback methods
        int FilePlaced(FCI.CurrentCabinet ccab, string filename, int cbFile, 
            bool fContinuation, IntPtr pObject)
        {
            try
            {
                FilePlacedEventArgs eventArgs;
                if (OnFilePlaced != null)
                {
                    eventArgs = new FilePlacedEventArgs(ccab,
                        filename, cbFile, fContinuation, this._userObject);
                    OnFilePlaced(this, eventArgs);
                    if (eventArgs.AbortOperation) return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                this._exceptions.Add(ex);
                return -1;
            }
        }

        bool GetNextCabinet(FCI.CurrentCabinet ccab, int cbPrevCab, IntPtr pObject)
        {
            try
            {
                if (OnGetNextCabinet != null)
                {
                    GetNextCabinetEventArgs eventArgs = new GetNextCabinetEventArgs(
                        ccab, cbPrevCab, this._userObject);
                    OnGetNextCabinet(this, eventArgs);
                    return !eventArgs.AbortOperation;
                }
                return true;
            }
            catch (Exception ex)
            {
                this._exceptions.Add(ex);
                return false;
            }
        }

        IntPtr GetOpenInfo(string fileName, ref ushort rDate, ref ushort rTime,
            ref ushort attribs, ref int err, IntPtr pObject)
        {
            IntPtr hFile = this._ioFunctions.FileOpen(fileName, (int)FCntl.COpenModes.ReadOnly,
                (int)FCntl.CShareModes.ShareRead);
            try
            {
				if (!Helpers.IsValidHandle(hFile))
                {
                    throw new Win32Exception();
                }
                else
                {
                    // Mask out all attributes except for ReadOnly, Hidden, 
                    // System, Archive to be suitable for FCI.
                    attribs = (ushort)((int)File.GetAttributes(fileName) 
                        & (int)(FileAttributes.ReadOnly 
                        | FileAttributes.System | FileAttributes.Hidden | 
                        FileAttributes.Archive));

                    // Currently we are using local times in the CAB file
                    // for compatibility, I would like to use UTC but we need
                    // to maintain compatibility with Windows Setup.
                    FileTimeRef lastWriteTimeUTC = new FileTimeRef();
                    FileTimeRef lastWriteTimeLocal = new FileTimeRef();

                    if (!Win32.API.IO.Unsafe.GetFileTime(hFile, null, null, 
                        lastWriteTimeUTC))
                    {
                        throw new Win32Exception();
                    }

                    if (!Win32.API.IO.FileTimeToLocalFileTime(
                        lastWriteTimeUTC, lastWriteTimeLocal))
                    {
                        throw new Win32Exception();
                    }

                    if (!Win32.API.IO.FileTimeToDosDateTime(lastWriteTimeLocal,
                        ref rDate, ref rTime))
                    {
                        throw new Win32Exception();
                    }

                    // Return the handle to FCI
                    return hFile;
                }
            }
            catch (Exception ex)
            {
                if (Helpers.IsValidHandle(hFile)) Win32.API.IO.Unsafe.CloseHandle(hFile);
                err = Marshal.GetLastWin32Error();
                this._exceptions.Add(ex);
                return (IntPtr)(-1);
            }
        }

        int StatusNotify(FCI.Status typeStatus, int cb1, int cb2, IntPtr userData)
        {
            try
            {
                bool abortOperation = false;
                switch (typeStatus)
                {
                    case FCI.Status.File:
                        if (OnCompressBlock != null)
                        {
                            StatusCompressBlockEventArgs fileEventArgs
                                = new StatusCompressBlockEventArgs(cb1, cb2,
                                this._userObject);
                            OnCompressBlock(this, fileEventArgs);
                            abortOperation = fileEventArgs.AbortOperation;
                        }
                        break;

                    case FCI.Status.Folder:
                        if (OnAddFolder != null)
                        {
                            StatusAddingFolderEventArgs folderEventArgs
                                = new StatusAddingFolderEventArgs(cb1, cb2,
                                this._userObject);
                            OnAddFolder(this, folderEventArgs);
                            abortOperation = folderEventArgs.AbortOperation;
                        }
                        break;

                    case FCI.Status.Cabinet:
                        // No event implemented for this, it looks more like a hack.
                        // Easier to split cabinet using only the media size in CurrentCabinet.
                        return cb2;
                }

                if (!abortOperation)
                {
                    // Return anything other than -1 to continue
                    return 1;
                }
                else
                {
                    // Return -1 to tell FCI to abort
                    return -1;
                }
            }
            catch (Exception ex)
            {
                this._exceptions.Add(ex);
                // Return -1 to tell FCI to abort
                return -1;
            }
        }
        #endregion

        #region Helper methods
        void EnsureContextIsAlive()
        {
            if (this._disposed)
                throw new ObjectDisposedException(this.ToString());
        }

        void ThrowFciException()
        {
            throw new FciException(this._pErrorStruct, this._exceptions);
        }
        #endregion
    }
}
