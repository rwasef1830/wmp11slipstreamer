using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.IO.Compression.Cabinet
{
    internal static class Helpers
    {
        internal static bool IsValidHandle(IntPtr handle)
        {
            return handle.ToInt64() > 0L;
        }
    }
}
