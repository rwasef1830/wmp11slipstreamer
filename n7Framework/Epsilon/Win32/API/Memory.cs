using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class Memory
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern IntPtr HeapAlloc(IntPtr hHeap, uint controlFlags,
            uint bytesNeeded);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool HeapFree(IntPtr hHeap,
            uint controlFlags, IntPtr memory);
    }
}
