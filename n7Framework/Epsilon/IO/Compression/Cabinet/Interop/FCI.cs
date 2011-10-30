using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Epsilon.IO.Compression.Cabinet
{
    /*
     * Notes: FCI.H bundled in Cabinet SDK has many errors that create hard-to-find bugs.
     * 
     * Here is a list of inconsistencies:
     * 1. CCAB (current cabinet) Max size is supposed to be a signed Int32.
     * 2. CCAB (current cabinet) Max folder threshold is supposed to be a signed Int32.
     * 3. FileRead and FileWrite function pointers should return a signed Int32.
     * (to be able to return -1 on error as CRT does).
     * 4. From WineHQ: Reserved header size must be <= 6000
     * 
     * All UINTs were clamped to LONGs since FCI can only handle < int.MaxValue
     */

    public static class FCI
    {
        /// <summary>
        /// Compression level flags
        /// </summary>
        public enum CompressionLevel : short
        {
            /// <summary>
            /// No compression at all (fastest)
            /// </summary>
            None = 0,
            /// <summary>
            /// Normal compression (balance between speed and ratio)
            /// </summary>
            MsZip = 1,
            /// <summary>
            /// Better compression (slower)
            /// </summary>
            Lzx15 = 3843,
            /// <summary>
            /// Best compression possible (slowest and most memory intensive)
            /// </summary>
            Lzx21 = 5379
        }

        /// <summary>
        /// Status values passed to the status callback.
        /// </summary>
        public enum Status : int
        {
            /// <summary>
            /// File added to Folder
            /// </summary>
            File = 0,
            /// <summary>
            /// File added to Cabinet
            /// </summary>
            Folder = 1,
            /// <summary>
            /// Cabinet completed
            /// </summary>
            Cabinet = 2
        }

        /// <summary>
        /// Built in error codes for FCI
        /// </summary>
        public enum ErrorCode : int
        {
            /// <summary>
            /// No error
            /// </summary>
            None,
            /// <summary>
            /// Failure opening file to be stored in cabinet
            /// </summary>
            OpenSource,
            /// <summary>
            /// Failure reading file to be stored in cabinet
            /// </summary>
            ReadSource,
            /// <summary>
            /// Out of memory in FCI
            /// </summary>
            MemoryAllocation,
            /// <summary>
            /// Could not create a temporary file
            /// </summary>
            CreateTempFile,
            /// <summary>
            /// Unknown compression type
            /// </summary>
            BadCompressionType,
            /// <summary>
            /// Could not create cabinet file
            /// </summary>
            CreateCabinetFile,
            /// <summary>
            /// Aborted by user
            /// </summary>
            UserAbort,
            /// <summary>
            /// Compression failed
            /// </summary>
            MciFail
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Error
        {
            public ErrorCode ErrorCode;
            public int CErrorCode;
            public bool HasError;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class CurrentCabinet
        {
            /// <summary>
            /// Size available for cabinet on this media
            /// </summary>
            public int MaximumCabinetSize;

            /// <summary>
            /// Size threshold that will cause a folder flush
            /// </summary>
            public int FolderThreshold;

            /// <summary>
            /// Size reserved in cabinet header [max: 60 * 1024]
            /// </summary>
            public int ReservedInHeader;

            /// <summary>
            /// Size reserved in cabinet folder
            /// </summary>
            public int ReservedInFolder;

            /// <summary>
            /// Size reserved in cabinet data
            /// </summary>
            public int ReservedInData;

            /// <summary>
            /// Cabinet sequence number
            /// </summary>
            public int CabinetId;

            /// <summary>
            /// Disk number
            /// </summary>
            public int DiskId;

            /// <summary>
            /// Fail if a block is incompressible
            /// </summary>
            [MarshalAs(UnmanagedType.Bool)]
            public bool FailOnIncompressibleBlocks;

            /// <summary>
            /// Cabinet set id
            /// </summary>
            public ushort SetId;

            /// <summary>
            /// Disk name (max: 255 characters)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string DiskName;

            byte _fixAlignment;

            /// <summary>
            /// Name of the cabinet file (max: 255 characters)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string CabinetName;

            byte _fixAlignmentAgain;

            /// <summary>
            /// The path to the directory that contains the 
            /// cabinet file including terminating separator
            /// (max: 255 characters)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string CabinetPath;

            /// <summary>
            /// Managed constructor to initialize members to default values
            /// </summary>
            public CurrentCabinet()
            {
                this.CabinetName = this.CabinetPath = this.DiskName = String.Empty;
            }
        }

        /// <summary>
        /// Allocate memory
        /// </summary>
        /// <param name="amountNeeded">Size of memory needed</param>
        /// <returns>Pointer to the allocated block or IntPtr.Zero if failed.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr MemAllocDelegate(int amountNeeded);

        /// <summary>
        /// Free memory
        /// </summary>
        /// <param name="memoryBlock">Pointer to the block to free</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MemFreeDelegate(IntPtr memoryBlock);

        /// <summary>
        /// Open the file specified by fileName, using the open mode 
        /// and sharing modes given. The open and sharing modes use C semantics.
        /// Return a file handle that the other file I/O delegates can use.
        /// Return -1 on error, and set the err value to a meaningful value 
        /// for your application.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr FileOpenDelegate(
            string fileName,
            int oflag,
            int pmode,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Read from the file referenced by the hf parameter into the passed array.
        /// The number of bytes to be read is given in the cb parameter.
        /// Return the number of bytes read.  Return -1 on error and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileReadDelegate(
            IntPtr hf,
            [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1,
                SizeParamIndex = 2)] byte[] buffer,
            int cb,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Write bytes from the passed array to the file referenced by the hf parameter.
        /// The number of bytes to write is given in the cb parameter.
        /// Return the number of bytes written.  Return -1 on error and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileWriteDelegate(
            IntPtr hf,
            [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1,
                SizeParamIndex = 2)] byte[] buffer,
            int cb,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Close the file referenced by the hf parameter.
        /// Return 0 on success.  Return -1 on error and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileCloseDelegate(
            IntPtr hf,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Seek to the requested position in the file referenced by hf.
        /// Return new position.  On error, return -1 and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileSeekDelegate(
            IntPtr hf,
            int dist,
            int seektype,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Delete the file passed in the fileName parameter.
        /// Return 0 on success.  On error, return -1 and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate int FileDeleteDelegate(
            string fileName,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Get the name and other information about the next cabinet.
        /// ccab is a reference to the FciCurrentCab structure to modify.
        /// cbPrevCab is an estimate of the size of the previous cabinet.
        /// At minimum, the function should change the ccab.CabName value.
        /// The CurrentCab value in the structure will have been updated by FCI.
        /// Return true on success.  Return false on failure.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool GetNextCabinetDelegate(
            [In, Out] CurrentCabinet ccab,
            int cbPrevCab,
            IntPtr userData);

        /// <summary>
        /// Called when FCI places a file in a cabinet.
        /// This is a notification only, and the client should not modify 
        /// the ccab structure.
        /// ccab is a reference to the cabinet parameters structure.
        /// fileName is the name of the file that was placed.
        /// cbFile is the length of the file in bytes.
        /// fContinuation is true if this is the later segment of a continued file.
        /// Return 0 on success.  Return -1 on error.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate int FilePlacedDelegate(
            CurrentCabinet ccab,
            string fileName,
            int cbFile,
            bool fContinuation,
            IntPtr userData);

        // Ideally, the tempName parameter would be a StringBuilder.
        // However, there seems to be a bug in the runtime that makes all StringBuilder
        // objects have a MaxCapacity of 16.
        // http://support.microsoft.com/?kbid=317577
        // So use an IntPtr and get hands dirty fiddling with bytes.
        //
        // That bug supposedly is fixed in .NET Framework 1.1 SP1, but StringBuilder still
        // doesn't work here.
        /// <summary>
        /// Get the name of a temporary file and return it in the buffer pointed to
        /// by tempName.  The length of the buffer is passed in cbTempName.
        /// The file must not exist when this function returns.
        /// Return true on success.  Return false on error.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool GetTempFileDelegate(
            IntPtr tempName,
            int cbTempName,
            IntPtr userData);

        /// <summary>
        /// Open a file and return information about it.
        /// The file to be opened is specified by the fileName parameter
        /// Date and rTime must be set to the file's last access time.  These values are
        /// DOS file date/time values.
        /// Set attribs to the file's attributes.  This uses C-format file attributes.
        /// On success, return the file handle.
        /// On error, return -1 and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr GetOpenInfoDelegate(
            string fileName,
            ref ushort rDate,
            ref ushort rTime,
            ref ushort attribs,
            ref int err,
            IntPtr userData);

        /// <summary>
        /// Status notification callback.  There are three different status values
        /// as defined by the FciStatus enumeration.
        /// 
        /// typeStatus == statusFile if compressing a block into a folder
        ///     cb1 = Size of compressed block
        ///     cb2 = Size of uncompressed block
        ///
        /// typeStatus == statusFolder if adding a folder to a cabinet
        ///     cb1 = Amount of folder copied to cabinet so far
        ///     cb2 = Total size of folder
        ///
        /// typeStatus == statusCabinet if writing out a complete cabinet
        ///     cb1 = Estimated cabinet size that was previously
        ///           passed to fnfciGetNextCabinet().
        ///     cb2 = Actual cabinet size
        /// 
        ///     NOTE: Return value is desired client size for cabinet
        ///     file.  FCI updates the maximum cabinet size
        ///     remaining using this value.  This allows a client
        ///     to generate multiple cabinets per disk, and have
        ///     FCI limit the size correctly -- the client can do
        ///     cluster size rounding on the cabinet size!
        ///     The client should either return cb2, or round cb2
        ///     up to some larger value and return that.
        /// 
        /// Return -1 on error.  FCI will abort.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StatusDelegate(
            Status typeStatus,
            int cb1,
            int cb2,
            IntPtr userData);

        /// <summary>
        /// Create a compression context.  Opens a new CAB file and prepares it to
        /// accept files.
        /// </summary>
        /// <param name="erf">Pointer to error structure in unmanaged memory</param>
        /// <param name="fnFilePlaced">Callback for file placement notifications.</param>
        /// <param name="fnMemAlloc">Memory allocation callback.</param>
        /// <param name="fnMemFree">Memory free callback.</param>
        /// <param name="fnFileOpen">File open callback.</param>
        /// <param name="fnFileRead">File read callback.</param>
        /// <param name="fnFileWrite">File write callback.</param>
        /// <param name="fnFileClose">File close callback.</param>
        /// <param name="fnFileSeek">File seek callback.</param>
        /// <param name="fnFileDelete">File delete callback.</param>
        /// <param name="fnTempFile">Callback to return temporary file name.</param>
        /// <param name="ccab">Reference to cabinet file parameters</param>
        /// <param name="userData">User's context pointer</param>
        /// <returns>On success, returns a non-null handle to an FCI context.
        /// On error, the return value will be IntPtr.Zero and the error structure
        /// will have error information.</returns>
        [DllImport("cabinet.dll",
             CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FCICreate")]
        static extern IntPtr CreateContext(
            SafeMemoryBlock erf,
            FilePlacedDelegate fnFilePlaced,
            MemAllocDelegate fnMemAlloc,
            MemFreeDelegate fnMemFree,
            FileOpenDelegate fnFileOpen,
            FileReadDelegate fnFileRead,
            FileWriteDelegate fnFileWrite,
            FileCloseDelegate fnFileClose,
            FileSeekDelegate fnFileSeek,
            FileDeleteDelegate fnFileDelete,
            GetTempFileDelegate fnTempFile,
            CurrentCabinet ccab,
            IntPtr userData);

        public static IntPtr CreateContext(SafeMemoryBlock erf,
            FilePlacedDelegate fnFilePlaced, MemAllocDelegate fnMemAlloc,
            MemFreeDelegate fnMemFree, FileOpenDelegate fnFileOpen,
            FileReadDelegate fnFileRead, FileWriteDelegate fnFileWrite,
            FileCloseDelegate fnFileClose, FileSeekDelegate fnFileSeek,
            FileDeleteDelegate fnFileDelete, GetTempFileDelegate fnTempFile,
            CurrentCabinet ccab)
        {
            return CreateContext(erf, fnFilePlaced, fnMemAlloc, fnMemFree,
                fnFileOpen, fnFileRead, fnFileWrite, fnFileClose, fnFileSeek,
                fnFileDelete, fnTempFile, ccab, IntPtr.Zero);
        }

        /// <summary>
        /// Add a disk file to a cabinet.
        /// </summary>
        /// <param name="hfci">Handle to FCI context returned by FciCreate.</param>
        /// <param name="sourceFileName">Full path and name of the file to add.</param>
        /// <param name="fileNameInCabinet">Name to use when storing in the cabinet.</param>
        /// <param name="fExecute">True if the file should be marked to execute on extraction.</param>
        /// <param name="fnGetNextCab">GetNextCab callback.</param>
        /// <param name="fnStatus">Status callback.</param>
        /// <param name="fnGetOpenInfo">OpenInfo callback.</param>
        /// <param name="typeCompress">Type of compression desired.</param>
        /// <returns>Returns true on success.  Returns false on failure.
        /// In the event of failure, the Error structure passed to the FciCreate function
        /// that created the current compression context will contain error information.</returns>
        [DllImport("cabinet.dll",
             CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FCIAddFile", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddFile(
            IntPtr hfci,
            string sourceFileName,
            string fileNameInCabinet,
            bool fExecute,
            GetNextCabinetDelegate fnGetNextCab,
            StatusDelegate fnStatus,
            GetOpenInfoDelegate fnGetOpenInfo,
            CompressionLevel typeCompress);

        /// <summary>
        /// Completes the current cabinet under construction, gathering all of the pieces
        /// and writing them to the cabinet file.
        /// </summary>
        /// <param name="hfci">Handle to FCI context returned by FciCreate.</param>
        /// <param name="fGetNextCab">If set to true, forces creation of a 
        /// new cabinet after this one is closed.
        /// If false, only creates a new cabinet if the current cabinet overflows.</param>
        /// <param name="fnGetNextCab">Callback function to get continuation
        /// cabinet information.</param>
        /// <param name="fnStatus">Status callback.</param>
        /// <returns>Returns true on success.  Returns false on failure, and 
        /// the Error structure passed to FciCreate is filled with 
        /// error information.</returns>
        /// <remarks>Flushing the cabinet causes a folder flush as well.</remarks>
        [DllImport("cabinet.dll",
             CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FCIFlushCabinet")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlushCabinet(
            IntPtr hfci,
            bool fGetNextCab,
            GetNextCabinetDelegate fnGetNextCab,
            StatusDelegate fnStatus);

        /// <summary>
        /// Forces completion of the current cabinet file folder.
        /// </summary>
        /// <param name="hfci">FCI context handle.</param>
        /// <param name="fnGetNextCab">Callback function to get continuation
        /// cabinet information.</param>
        /// <param name="fnStatus">Status callback.</param>
        /// <returns>Returns true on success.  Returns false on failure, and 
        /// the Error structure passed to FciCreate is filled with 
        /// error information.</returns>
        [DllImport("cabinet.dll",
             CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FCIFlushFolder")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlushFolder(
            IntPtr hfci,
            GetNextCabinetDelegate fnGetNextCab,
            StatusDelegate fnStatus);

        /// <summary>
        /// Destroy an FCI context and delete temporary files.
        /// </summary>
        /// <param name="hfci">Handle to FCI context.</param>
        /// <returns>Returns true if successful. If unsuccessful, returns 
        /// false and the Error structure passed to FciCreate is filled with error 
        /// information.</returns>
        /// <remarks>If this function fails, temporary files 
        /// could be left behind.</remarks>
        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FCIDestroy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyContext(IntPtr hfci);
    }
}
