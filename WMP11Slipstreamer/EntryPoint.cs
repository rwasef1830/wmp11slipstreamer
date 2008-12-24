using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Epsilon;
using System.Threading;
using System.Reflection;
using System.Resources;
using System.Globalization;
using Epsilon.DebugServices;
using Epsilon.Slipstreamers.WMP11Slipstreamer.Localization;

namespace Epsilon.Slipstreamers.WMP11Slipstreamer
{
    static class EntryPoint
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
                string filename = CM.SaveFileDialogStandard("Choose location to save debug log...",
                    "Log files (*.log)|*.log|All files (*.*)|*.*");
                HelperConsole.InitializeDefaultConsole(filename);
#endif

                ArgumentParser argParser = new ArgumentParser(args);
                argParser.Parse(
                    0,
                    10,
                    0,
                    0,
                    new string[] { "nocats", "slipstream", "closeonsuccess" },
                    new string[] { "installer", "winsource", "customicon", "output", 
                        "hotfix", "customiconpath", "resfile" , "culture" },
                    false
                );

                string installer = argParser.GetValue("installer");
                string winsource = argParser.GetValue("winsource");
                string output = argParser.GetValue("output");
                string hotfixes = argParser.GetValue("hotfix");
                string customicon = argParser.GetValue("customicon");
                bool nocats = argParser.IsSpecified("nocats");
                bool slipstream = argParser.IsSpecified("slipstream");
                bool closeonsuccess = argParser.IsSpecified("closeonsuccess") && slipstream;
                string customiconpath = argParser.GetValue("customiconpath");

                string culture = argParser.GetValue("culture");
                string resFile = argParser.GetValue("resfile");

                if (!String.IsNullOrEmpty(resFile))
                {
                    if (!File.Exists(resFile))
                    {
                        throw new FileNotFoundException("The resource file was not found.",
                            resFile);
                    }

                    ResourceManager resMan = ResourceManager.CreateFileBasedResourceManager(
                        Path.GetFileNameWithoutExtension(resFile),
                        Path.GetDirectoryName(resFile), null);
                    FieldInfo resManInfo = typeof(Messages).GetField("resourceMan",
                        BindingFlags.Static | BindingFlags.NonPublic);
                    resManInfo.SetValue(typeof(Messages), resMan); 
                }
                else if (!String.IsNullOrEmpty(culture))
                {
                    try
                    {
                        CultureInfo overrideCulture = CultureInfo.GetCultureInfo(culture);
                        Assembly.GetExecutingAssembly().GetSatelliteAssembly(overrideCulture,
                            new Version(
                            ((SatelliteContractVersionAttribute)(
                            Assembly.GetExecutingAssembly().GetCustomAttributes(
                            typeof(SatelliteContractVersionAttribute), false)[0])).Version));
                        Thread.CurrentThread.CurrentUICulture = overrideCulture;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "An error occurred while overriding the culture of the application. Maybe the culture string is invalid."
                            + Environment.NewLine + Environment.NewLine + ex.Message,
                            "Culture override failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                ValidateResource();

                Application.Run(new MainForm(installer, winsource, hotfixes,
                    output, customicon, nocats, slipstream, closeonsuccess,
                    customiconpath));

                return 0;
            }
            catch (ShowUsageException)
            {
                ShowUsageInformation();
                return 1;
            }
            catch (ArgumentParserException ex)
            {
                MessageBox.Show(ex.Message
                    + Environment.NewLine + Environment.NewLine
                    + "Click \"OK\" to view usage information.", 
                    "Argument Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowUsageInformation();
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unhandled Exception in UI Thread");
                return 2;
            }
        }

        static void ValidateResource()
        {
            if (!String.Equals(Messages.LocalizerKey, Globals.LocalizerKey,
                StringComparison.Ordinal))
            {
                MessageBox.Show(
                    "Localizer key mismatch. Switching back to default culture.",
                    "Satellite assembly error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                ResetCultureToDefault();
            }
        }

        static void ResetCultureToDefault()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(
                ((NeutralResourcesLanguageAttribute)(
                Assembly.GetExecutingAssembly().GetCustomAttributes(
                typeof(NeutralResourcesLanguageAttribute), false)[0])).CultureName);
        }

        static void ShowUsageInformation()
        {
            MessageBox.Show(Messages.dlgUsageInfo_Text, Messages.dlgUsageInfo_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}