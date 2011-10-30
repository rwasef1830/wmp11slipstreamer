using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.IO.Compression
{
    public static class Delta
    {
        [Flags]
        public enum ApplyOptionFlags
        {
            FailIfExact = 0x1,
            FailIfClose = 0x2,
            TestOnly = 0x4,
            AllFlags = 0x7
        }

        public static bool ApplyPatchToFile(
            string patchFilePath,
            string basisFilePath,
            string destinationFilePath,
            ApplyOptionFlags flags)
        {
            return DeltaWin32.ApplyPatchToFile(patchFilePath, 
                basisFilePath, destinationFilePath, (int)flags);
        }
    }
}
