using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Security;

namespace Epsilon.Win32
{
    /// <summary>
    /// Safe handle for use with Win32 functions that return handles
    /// that must be closed with CloseHandle when done
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeWin32Handle : SafeHandleZeroOrMinusOneIsInvalid
    {
        SafeWin32Handle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            try
            {
                return CloseHandle(base.handle);
            }
            finally
            {
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
    }

    /// <summary>
    /// Safe handle for use with Win32 functions like MapViewOfFile that
    /// return handles that must be unmapped when done
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeMappedViewHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        SafeMappedViewHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            try
            {
                return UnmapViewOfFile(base.handle);
            }
            finally
            {
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool UnmapViewOfFile(IntPtr handle);
    }

    /// <summary>
    /// Simple registry handle wrapper
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        SafeRegistryHandle() : base(true)
        {
        }

        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                return (RegCloseKey(base.handle) == 0);
            }
            finally
            {
            }
        }

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(IntPtr hKey);
    }

    /// <summary>
    /// Simple service handle wrapper
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        SafeServiceHandle() : base(true)
        {
        }

        public SafeServiceHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                return CloseServiceHandle(base.handle);
            }
            finally
            {
            }
        }

        [DllImport("advapi32.dll")]
        static extern bool CloseServiceHandle(IntPtr serviceHandle);
    }

    /// <summary>
    /// Safe wrapper around find file handle
    /// </summary>
    public sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Methods
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        SafeFindHandle() : base(true)
        {
        }

        public SafeFindHandle(IntPtr preExistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preExistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                return FindClose(base.handle);
            }
            finally
            {
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool FindClose(IntPtr hFindFile);
    }
}
