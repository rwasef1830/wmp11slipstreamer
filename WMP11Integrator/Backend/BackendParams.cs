using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WMP11Slipstreamer
{
    class BackendParams
    {
        public string WinSource;
        public string WmpInstallerSource;
        public string HotfixLine;
        public int AddonType;
        public byte[] CustomIcon;
        public bool IgnoreCats;
        public BackendResult Result;

        public BackendParams()
        {
            this.Result = BackendResult.NotStarted;
        }
    }

    enum BackendResult
    {
        Success = 0,
        Cancelled = 1,
        Error = 2,
        UnhandledException = 3,
        NotStarted = 4,
    }
}
