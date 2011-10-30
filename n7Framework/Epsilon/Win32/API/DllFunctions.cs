using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class DllFunctions
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibraryEx(string lpFileName,
            IntPtr reservedMustBeNull,
            uint dwFlags);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}
