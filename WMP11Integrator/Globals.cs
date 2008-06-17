using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Resources;
using System.Reflection;

[assembly: SatelliteContractVersion("1.0.0.0")]

namespace WMP11Slipstreamer
{    
    public class Globals
    {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public const string Repo1Key = "e2f2dad638ce726af2f03074eef307cb";
        public const string Repo2Key = "86fcb6bda64c0a7674179f7d1e09e5fe";
        public const string LocalizerKey = "3270d2f650dacfe8c48f2f66ce3179d7";
        public const ulong UniqueTag = 0xFC010203040506CF;

        // Website URL
        public const string WebsiteUrl = "http://www.boooggy.org/slipstreamer/";

        // WMP Download URL
        public const string WmpRedistUrl 
            = "http://www.microsoft.com/windows/windowsmedia/player/download/download.aspx";

        // Registry values
        public const string wmp11SlipstreamerKey = "Software\\WMP11Slipstreamer";
        public const string wmp11InstallerValue = "LastWmp11InstallerPath";
        public const string winSourceValue = "LastWindowsSourcePath";
        public const string addonTypeValue = "LastAddOnTypeUsed";
        public const string useCustomIconValue = "UseCustomIcon";
        public const string whichCustomIconValue = "CustomIconIndex";
        public const string hotfixLineValue = "HotfixesUsed";
        public const string customIconData = "CustomIconData";
    }
}
