using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.IO.Compression
{
    public static class DeltaWin32
    {
        [DllImport("mspatcha.dll", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Auto, SetLastError=true)]
        public static extern bool ApplyPatchToFile(
            [MarshalAs(UnmanagedType.LPTStr)]string patchFilePath,
            [MarshalAs(UnmanagedType.LPTStr)]string basisFilePath,
            [MarshalAs(UnmanagedType.LPTStr)]string destinationFilePath,
            [MarshalAs(UnmanagedType.U4)] int applyOptionFlags);
    }
}
