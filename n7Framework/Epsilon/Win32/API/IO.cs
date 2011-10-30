using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Epsilon.Win32.API
{
    public static class IO
    {
        public static class Safe
        {
            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern SafeFileHandle CreateFile(
                string fileName,
                [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
                [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
                IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] ExtendedFileAttributes flags,
                IntPtr template);

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern SafeFileHandle CreateFile(
                string fileName,
                [MarshalAs(UnmanagedType.U4)] uint desiredAccessWin32,
                [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
                IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] ExtendedFileAttributes flags,
                IntPtr template);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetFileTime(SafeFileHandle hFile,
                FileTimeRef creationTime,
                FileTimeRef lastAccessTime,
                FileTimeRef lastWriteTime);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetFileTime(SafeFileHandle hFile,
                FileTimeRef creationTime,
                FileTimeRef lastAccessTime,
                FileTimeRef lastWriteTime);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadFile(SafeFileHandle hFile,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)][Out] byte[] lpBuffer, 
                uint numberOfBytesToRead, out uint numberOfBytesRead, 
                IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteFile(SafeFileHandle hFile,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)][In] byte[] lpBuffer, 
                uint numberOfBytesToWrite, out uint numberOfBytesWritten, 
                IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetFilePointerEx(SafeFileHandle hFile,
                long liDistanceToMove, out long lpNewFilePointer, uint dwMoveMethod);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafeFindHandle FindFirstFile(
                string lpFileName,
                [Out] FindData lpFindFileData);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool FindNextFile(
                SafeFindHandle hFindFile, 
                [Out] FindData lpFindFileData);
        }

        public static class Unsafe
        {
            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFile(
                string fileName,
                FileAccess fileAccess,
                FileShare fileShare,
                IntPtr securityAttributes,
                FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] ExtendedFileAttributes flags,
                IntPtr template);

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFile(
                string fileName,
                [MarshalAs(UnmanagedType.U4)] uint desiredAccessWin32,
                [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
                IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] ExtendedFileAttributes flags,
                IntPtr template);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetFileTime(IntPtr hFile,
                FileTimeRef creationTime,
                FileTimeRef lastAccessTime,
                FileTimeRef lastWriteTime);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetFileTime(IntPtr hFile,
                FileTimeRef creationTime,
                FileTimeRef lastAccessTime,
                FileTimeRef lastWriteTime);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static unsafe extern bool ReadFile(IntPtr hFile,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)][Out] byte[] lpBuffer, 
                uint numberOfBytesToRead, out uint numberOfBytesRead, 
                IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static unsafe extern bool ReadFile(IntPtr hFile,
                byte* lpBuffer, uint numberOfBytesToRead,
                out uint numberOfBytesRead, IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteFile(IntPtr hFile,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] lpBuffer, 
                uint numberOfBytesToWrite, out uint numberOfBytesWritten, 
                IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static unsafe extern bool WriteFile(IntPtr hFile,
                byte* lpBuffer, uint numberOfBytesToWrite,
                out uint numberOfBytesWritten, IntPtr overlappedStructure);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetFilePointerEx(IntPtr hFile,
                long liDistanceToMove, out long lpNewPosition, uint dwMoveMethod);

            [DllImport("kernel32.dll")]
            public static extern bool CloseHandle(IntPtr handle);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetShortPathName(string longPath,
            StringBuilder shortPath, uint bufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FileTimeToDosDateTime(
            FileTimeRef pFileTime,
            ref ushort fatDate, ref ushort fatTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DosDateTimeToFileTime(
            ushort fatDate, ushort fatTime,
            FileTimeRef fileTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FileTimeToLocalFileTime(
            FileTimeRef pFileTime,
            FileTimeRef pLocalFileTime
        );

        [Flags]
        public enum ExtendedFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            WriteThrough = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        public static class DesiredAccessWin32
        {
            [Flags]
            public enum FileAccess : uint
            {
                None = 0x0,
                ReadData = 0x0001,
                ReadAttributes = 0x0080,
                ReadExtendedAttributes = 0x0008,
                
                WriteData = 0x0002,
                AppendData = 0x0004,
                WriteAttributes = 0x0100,
                WriteExtendedAttributes = 0x0010,

                Execute = 0x0020,

                AllRead = ReadData | ReadAttributes | ReadExtendedAttributes,
                AllWrite = WriteData | AppendData | WriteAttributes 
                    | WriteExtendedAttributes
            }

            [Flags]
            public enum DirectoryAccess : uint
            {
                None = 0x0,
                AddFile = 0x0002,
                AddSubDirectory = 0x0004,
                DeleteChild = 0x0040,
                ListFiles = 0x0001,
                Traverse = 0x0020
            }

            public enum PipeAccess : uint
            {
                CreatePipeInstance = 0x0004
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class FileTimeRef
    {
        uint _fileTimeLow;
        uint _fileTimeHigh;

        public long FileTime
        {
            get
            {
                return ((long)this._fileTimeHigh << 32) | this._fileTimeLow;
            }
            set
            {
                this._fileTimeLow = (uint)value;
                this._fileTimeHigh = (uint)(value >> 32);
            }
        }

        public FileTimeRef() { }

        public FileTimeRef(long fileTime)
        {
            this.FileTime = fileTime;
        }

        public DateTime ToDateTime(bool isUtc)
        {
            if (isUtc) return DateTime.FromFileTimeUtc(this.FileTime);
            else return DateTime.FromFileTime(this.FileTime);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class FindData
    {
        [MarshalAs(UnmanagedType.U4)]
        public FileAttributes dwFileAttributes;
        uint _ftCreationTimeLow;
        uint _ftCreationTimeHigh;
        uint _ftLastAccessTimeLow;
        uint _ftLastAccessTimeHigh;
        uint _ftLastWriteTimeLow;
        uint _ftLastWriteTimeHigh;
        uint _nFileSizeHigh;
        uint _nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;

        public long ftCreationTime
        {
            get { return ((long)this._ftCreationTimeHigh << 32) | this._ftCreationTimeLow; }
        }

        public long ftLastAccessTime
        {
            get { return ((long)this._ftLastAccessTimeHigh << 32) | this._ftLastAccessTimeLow; }
        }

        public long ftLastWriteTime
        {
            get { return ((long)this._ftLastWriteTimeHigh << 32) | this._ftLastWriteTimeLow; }
        }

        public long FileSize
        {
            get { return ((long)this._nFileSizeHigh << 32) | this._nFileSizeLow; }
        }
    }
}
