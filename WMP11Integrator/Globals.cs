using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace WMP11Slipstreamer
{
    public class Globals
    {
        public static string Version = Application.ProductVersion;
        public static StreamWriter Logger;
        public const string repository1Key = "e2f2dad638ce726af2f03074eef307cb";
        public const string otherReposKeys = "86fcb6bda64c0a7674179f7d1e09e5fe";
        public const ulong uniqueTag = 0xFC010203040506CF;
        public const string cancelMessage
            = "Integration has been cancelled by the user. "
            + "The source has not been modified.";
        public const string successMessage
            = "Integration completed successfully.";

        // Website URL
        public const string website = "http://www.boooggy.org/slipstreamer/";

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
