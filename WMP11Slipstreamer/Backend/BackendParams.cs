using System;
using System.Collections.ObjectModel;
using System.IO;
using Epsilon.Slipstreamers;

namespace Epsilon.WMP11Slipstreamer
{
    public class BackendParams : SlipstreamerArgumentsBase
    {
        public readonly byte[] CustomIcon;
        public readonly ReadOnlyCollection<string> HotfixFiles;
        public readonly string HotfixFolder;
        public readonly PackageType RequestedType;
        public readonly string WmpInstallerSource;

        public BackendParams(
            string winSrc, WindowsSourceInfo srcInfo,
            string wmpInstallerSource, string hotfixLine, PackageType addonType,
            byte[] customIcon, bool ignoreCats) : base(winSrc, srcInfo, ignoreCats)
        {
            this.WmpInstallerSource = Path.GetFullPath(wmpInstallerSource);
            base.ParseHotfixLine(hotfixLine, out this.HotfixFolder, out this.HotfixFiles);
            this.CustomIcon = customIcon;
            this.RequestedType = addonType;
        }
    }

    public enum PackageType
    {
        Unknown = 0,
        Vanilla = 1,
        Tweaked = 2
    }
}