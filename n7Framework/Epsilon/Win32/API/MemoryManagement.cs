using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Epsilon.Win32.API
{
    public static class MemoryManagement
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeWin32Handle CreateFileMapping(SafeFileHandle hFile,
           IntPtr lpFileMappingAttributes, PageProtection flProtect,
           uint dwMaximumSizeHigh, uint dwMaximumSizeLow, IntPtr lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeMappedViewHandle MapViewOfFile(
            SafeWin32Handle hFileMappingObject,
            MappingAccess dwDesiredAccess, uint dwFileOffsetHigh,
            uint dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);
    }

    [Flags]
    public enum PageProtection : uint
    {
        NoAccess = 0x01,
        Readonly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        Guard = 0x100,
        NoCache = 0x200,
        WriteCombine = 0x400,
    }

    [Flags]
    public enum MappingAccess : uint
    {
        SectionQuery = 0x0001,
        SectionMapWrite = 0x0002,
        SectionMapRead = 0x0004,
        SectionMapExecute = 0x0008,
        SectionExtendSize = 0x0010,
        SectionAllAccess = AccessMask.StandardRightsRequired | SectionQuery
            | SectionMapWrite | SectionMapRead | SectionMapExecute
            | SectionExtendSize
    }
}
