using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Epsilon.Win32.API
{
    public static class Security
    {
        [DllImport("Advapi32.dll", SetLastError = true)]
        public static extern bool CreateRestrictedToken(
            IntPtr hExistingToken,
            PrivilegeFlags flags,
            uint disableSidCount,
            SidAndAttributes[] sidsToDisable,
            uint deletePrivilegeCount,
            LuidAndAttributes[] privilegesToDelete,
            uint restrictSidCount,
            SidAndAttributes[] sidsToRestrict,
            out SafeWin32Handle hNewToken
        );

        [DllImport("Advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(
            SafeWin32Handle hExistingToken,
            bool disableAllPrivileges,
            ref TokenPrivileges newTokenPrivileges,
            uint bufferLength,
            IntPtr oldTokenPrivileges,
            IntPtr returnLength
        );

        public static bool AdjustTokenPrivileges(
            SafeWin32Handle hExistingToken,
            bool disableAllPrivileges,
            ref TokenPrivileges newTokenPrivileges,
            uint bufferLength,
            ref TokenPrivileges oldTokenPrivileges,
            ref uint returnLength)
        {
            GCHandle handleOldPriv = GCHandle.Alloc(oldTokenPrivileges, 
                GCHandleType.Pinned);
            GCHandle handleReturnLength = GCHandle.Alloc(returnLength, 
                GCHandleType.Pinned);
            bool result = AdjustTokenPrivileges(
                hExistingToken, disableAllPrivileges, ref newTokenPrivileges,
                bufferLength, handleOldPriv.AddrOfPinnedObject(), 
                handleReturnLength.AddrOfPinnedObject());
            handleOldPriv.Free();
            handleReturnLength.Free();
            return result;
        }

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupPrivilegeValue(
            StringBuilder systemName,
            StringBuilder privilegeName,
            out Luid luid
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SidAndAttributes
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LuidAndAttributes
    {
        public Luid Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [Flags]
    public enum PrivilegeFlags : uint
    {
        DisableMaxPrivilege = 0x1,
        SandboxInert = 0x2,
        /// <summary>
        /// Available in Windows Vista and upwards only
        /// </summary>
        LuaToken = 0x4,
        /// <summary>
        /// Available in Windows Vista and upwards only
        /// </summary>
        WriteRestricted = 0x8
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public LuidAndAttributes Privileges;
    }

    public enum PrivilegeAttribute : uint
    {
        EnabledByDefault = 0x00000001,
        Enabled = 0x00000002,
        /// <summary>
        /// Available on Windows XP SP2 and higher only
        /// </summary>
        Removed = 0x00000004,
        UsedForAccess = 0x80000000,
        Disabled = 0
    }

    [Flags]
    public enum GenericAccessRights : uint
    {
        Read = 0x80000000,
        Write = 0x40000000,
        Execute = 0x20000000,
        All = 0x10000000
    }
}
