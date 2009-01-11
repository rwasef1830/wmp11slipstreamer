using System;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using Epsilon.n7Framework.Epsilon.Parsers;
using Epsilon.n7Framework.Epsilon.Slipstreamers;
using Epsilon.WMP11Slipstreamer.Localization;

[assembly: SatelliteContractVersion("1.1.0.0")]

namespace Epsilon.WMP11Slipstreamer
{
    internal class Globals
    {
        internal static Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        internal const string Repo1Key = "e2f2dad638ce726af2f03074eef307cb";
        internal const string Repo2Key = "86fcb6bda64c0a7674179f7d1e09e5fe";
        internal const string LocalizerKey = "dc826746e1b7919b6275ea3f14c5e1bd";
        internal const ulong UniqueTag = 0xFC010203040506CF;

        // Website URL
        internal const string WebsiteUrl = "http://www.boooggy.org/slipstreamer/";

        // WMP Download URL
        internal const string WmpRedistUrl
            = "http://www.microsoft.com/windows/windowsmedia/player/download/download.aspx";

        // Registry values
        internal const string wmp11SlipstreamerKey = "Software\\WMP11Slipstreamer";
        internal const string wmp11InstallerValue = "LastWmp11InstallerPath";
        internal const string winSourceValue = "LastWindowsSourcePath";
        internal const string addonTypeValue = "LastAddOnTypeUsed";
        internal const string useCustomIconValue = "UseCustomIcon";
        internal const string whichCustomIconValue = "CustomIconIndex";
        internal const string hotfixLineValue = "HotfixesUsed";
        internal const string customIconData = "CustomIconData";

        internal static void ShowUsageInformation()
        {
            MessageBox.Show(
                String.Format(Msg.dlgUsageInfo_Text,
                Assembly.GetExecutingAssembly().GetName().Name,
                typeof(Msg).Name, typeof(SlipstreamersMsg).Name, 
                typeof(ParsersMsg).Name),
                Msg.dlgUsageInfo_Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
