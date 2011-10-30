using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Epsilon.IO.Compression.Cabinet
{
    public static class FDI
    {
        /// <summary>
        /// Error codes returned by FDI functions in the CabError.FdiError property.
        /// </summary>
        /// <remarks>
        /// In general, FDI will only fail if one of the passed-in memory or file I/O
        /// functions fails.  Other errors are unlikely and are caused by corrupted
        /// cabinet files, passing in a file which is not a cabinet, or cabinet
        /// files out of order.
        /// </remarks>
        public enum ErrorCode : int
        {
            /// <summary>
            /// No Error.
            /// </summary>
            None,
            /// <summary>
            /// The cabinet file was not found.
            /// </summary>
            CabinetNotFound,
            /// <summary>
            /// The referenced file does not have the correct format.
            /// </summary>
            NotACabinet,
            /// <summary>
            /// The cabinet file has an unknown version number.
            /// </summary>
            UnknownCabinetVersion,
            /// <summary>
            /// The cabinet file is corrupt.
            /// </summary>
            CorruptCabinet,
            /// <summary>
            /// Could not allocate memory.
            /// </summary>
            AllocFail,
            /// <summary>
            /// A folder in a cabinet has an unknown compression type.
            /// </summary>
            BadCompressionType,
            /// <summary>
            /// Failure decompressing data from a cabinet file.
            /// </summary>
            MdiFail,
            /// <summary>
            /// Failure writing to the target file.
            /// </summary>
            TargetFileWrite,
            /// <summary>
            /// Cabinets in a set do not have the same reserve sizes.
            /// </summary>
            ReserveMismatch,
            /// <summary>
            /// Cabinet returned from the NextCabinet notification is incorrect.
            /// </summary>
            WrongCabinet,
            /// <summary>
            /// FDI aborted.
            /// </summary>
            UserAbort
        }

        /// <summary>
        /// FDI notification types.
        /// </summary>
        public enum NotificationType
        {
            /// <summary>
            /// General information about cabinet
            /// </summary>
            CabinetInfo,
            /// <summary>
            /// First file in cabinet is continuation
            /// </summary>
            PartialFile,
            /// <summary>
            /// File to be copied
            /// </summary>
            CopyFile,
            /// <summary>
            /// Close the file, set relevant info
            /// </summary>
            CloseFileInfo,
            /// <summary>
            /// File continued to next cabinet
            /// </summary>
            NextCabinet,
            /// <summary>
            /// Enumeration status
            /// </summary>
            Enumerate
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Error
        {
            public ErrorCode errorCode;
            public int cErrorCode;
            public bool fError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class CabinetInfo
        {
            /// <summary>
            /// Total length of the cabinet
            /// </summary>
            public int CabinetLength;

            /// <summary>
            /// Number of folders in the cabinet
            /// </summary>
            public ushort NumberOfFolders;

            /// <summary>
            /// Number of files in the cabinet
            /// </summary>
            public ushort NumberOfFiles;

            /// <summary>
            /// Cabinet set ID
            /// </summary>
            public ushort SetId;

            /// <summary>
            /// Has space reserved
            /// </summary>
            public bool IsSpaceReserved;

            /// <summary>
            /// Cabinet is part of a chain and has a cabinet before it
            /// </summary>
            public bool IsChainedPrevious;

            /// <summary>
            /// Cabinet is part of a chain and has a cabinet after it
            /// </summary>
            public bool IsChainedNext;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class Notification
        {
            public int cb;
            public string String1;
            public string String2;
            public string String3;
            public IntPtr UserData;
            public IntPtr FileHandle;
            public ushort Date;
            public ushort Time;
            public ushort Attributes;
            public ushort SetId;
            public ushort CabinetNumber;
            public ushort FolderNumber;
            public ErrorCode ErrorType;
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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr FileOpenDelegate(
            string fileName,
            int oflag,
            int pmode);

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
            int cb);

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
            int cb);

        /// <summary>
        /// Close the file referenced by the hf parameter.
        /// Return 0 on success.  Return -1 on error and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileCloseDelegate(IntPtr hf);

        /// <summary>
        /// Seek to the requested position in the file referenced by hf.
        /// Return new position.  On error, return -1 and set the err value.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FileSeekDelegate(
            IntPtr hf,
            int dist,
            int seektype);

        /// <summary>
        /// Notification callback.  fdint tells which notification is being given.
        /// fdin is the notification structure that can be modified by the client.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr NotifyDelegate(
            NotificationType fdint, 
            Notification fdin);

        [DllImport("cabinet.dll",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "FDICreate")]
        static extern IntPtr CreateContext(
            MemAllocDelegate fnMemAlloc,
            MemFreeDelegate fnMemFree,
            FileOpenDelegate fnFileOpen,
            FileReadDelegate fnFileRead,
            FileWriteDelegate fnFileWrite,
            FileCloseDelegate fnFileClose,
            FileSeekDelegate fnFileSeek,
            int cpuType,    // ignored by 32-bit FDI
            SafeMemoryBlock erf);

        public static IntPtr CreateContext(MemAllocDelegate fnMemAlloc,
            MemFreeDelegate fnMemFree, FileOpenDelegate fnFileOpen,
            FileReadDelegate fnFileRead, FileWriteDelegate fnFileWrite,
            FileCloseDelegate fnFileClose, FileSeekDelegate fnFileSeek, 
            SafeMemoryBlock erf)
        {
            return CreateContext(fnMemAlloc, fnMemFree, fnFileOpen, fnFileRead,
                fnFileWrite, fnFileClose, fnFileSeek, -1, erf);
        }

        /// <summary>
        /// Determines if a file is a cabinet, and returns information about the cabinet
        /// if so.
        /// </summary>
        /// <param name="hfdi">FDI context created by FdiCreate.</param>
        /// <param name="hf">File handle compatible with Read and Seek delegates passed
        /// to FdiCreate.  The file should be positioned at offset 0 in the file to test.</param>
        /// <param name="cabInfo">Structure to receive information about the cabinet file.</param>
        /// <returns>Returns true if the file appears to be a valid cabinet.  Information
        /// about the file is placed in the passed cabInfo structure.
        /// Returns false if the file is not a cabinet.</returns>
        [DllImport("cabinet.dll",
             CallingConvention = CallingConvention.Cdecl,
             EntryPoint = "FDIIsCabinet")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsCabinet(IntPtr hfdi, IntPtr hf, 
            [Out] CabinetInfo ccab);

        [DllImport("cabinet.dll",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "FDICopy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool Copy(IntPtr hfdi,
            string cabinetName,
            string cabinetPath,
            int flags,
            NotifyDelegate notifyMethod,
            IntPtr decryptMethod,
            IntPtr userData);

        public static bool Copy(IntPtr hfdi,
            string cabinetName, string cabinetPath,
            NotifyDelegate notifyMethod)
        {
            string newCabinetPathRef;
            if (!cabinetPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                newCabinetPathRef = cabinetPath + Path.DirectorySeparatorChar;
            else
                newCabinetPathRef = cabinetPath;

            bool result = Copy(hfdi, cabinetName, newCabinetPathRef, 0, notifyMethod,
                IntPtr.Zero, IntPtr.Zero);
            GC.KeepAlive(newCabinetPathRef);
            return result;
        }

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "FDIDestroy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyContext(IntPtr hfdi);
    }
}
