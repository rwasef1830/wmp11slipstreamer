using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Epsilon.WindowsModTools;
using System.IO;
using System.Collections.ObjectModel;

namespace Epsilon.Slipstreamers.WMP11Slipstreamer
{
    public class BackendParams : SlipstreamerArguments
    {
        public readonly string WmpInstallerSource;
        public readonly string HotfixFolder;
        public readonly ReadOnlyCollection<string> HotfixFiles;
        public readonly PackageType RequestedType;
        public readonly byte[] CustomIcon;
        public readonly bool IgnoreCats;

        public BackendResult Result;

        public BackendParams(string winSrc, WindowsSourceInfo srcInfo, 
            string wmpInstallerSource, string hotfixLine, PackageType addonType, 
            byte[] customIcon, bool ignoreCats) : base(winSrc, srcInfo)
        {
            this.WmpInstallerSource = Path.GetFullPath(wmpInstallerSource);
            base.ParseHotfixLine(hotfixLine, out this.HotfixFolder, out this.HotfixFiles);
            this.CustomIcon = customIcon;

            this.RequestedType = addonType;
            this.IgnoreCats = ignoreCats;

            this.Result = BackendResult.NotStarted;
        }
    }

    public enum BackendResult
    {
        Success = 0,
        Cancelled = 1,
        Error = 2,
        UnhandledException = 3,
        NotStarted = 4,
    }

    public enum PackageType
    {
        Unknown = 0,
        Vanilla = 1,
        Tweaked = 2
    }
}
