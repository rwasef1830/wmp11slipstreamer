using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class DebugHelp
    {
        [DllImport("imagehlp.dll", SetLastError = true)]
        public static extern IntPtr CheckSumMappedFile(
            SafeMappedViewHandle baseAddress,
            uint FileLength, IntPtr HeaderSum, IntPtr CheckSum);
    }
}
